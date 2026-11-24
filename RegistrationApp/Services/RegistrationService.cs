using Gcmac.Registration.Data;
using Gcmac.Registration.Models;
using Gcmac.Registration.Options;
using Microsoft.Extensions.Options;

namespace Gcmac.Registration.Services;

public sealed record PaymentCompletion(PaymentCallbackResult Result, string? RegistrationId);

public sealed class RegistrationService(
    RegistrationStore store,
    FeeCatalog fees,
    IPaymentGateway gateway,
    INotificationService notifications,
    IOptions<NotificationOptions> notificationOptions)
{
    public FeeCatalog Fees => fees;

    public int MaxDelegatesPerMember => notificationOptions.Value.MaxDelegatesPerMember;

    public async Task<RegistrationRecord> CreateAsync(RegistrationInput input, string accountEmail)
    {
        var record = RegistrationRecord.From(input, RegistrationStore.GenerateId(), accountEmail,
            fees.Calculate(input.Category));
        var created = await store.AddRegistrationAsync(record);
        await notifications.RegistrationSubmittedAsync(created);
        return created;
    }

    public async Task<RegistrationRecord?> UpdateAsync(string id, RegistrationInput input, string sponsorMemberNo)
    {
        var record = await store.FindAsync(id);
        if (record is null ||
            !string.Equals(record.SponsorMemberNo, sponsorMemberNo, StringComparison.OrdinalIgnoreCase))
            return null;

        var fee = fees.Calculate(input.Category);
        record.Category = input.Category;
        record.FullName = input.FullName.Trim();
        record.MembershipId = MembershipIds.Build(
            input.SponsorMemberNo ?? (input.Category == DelegateCategory.CmaPractice ? input.MembershipNo : null),
            record.FullName);
        record.Email = input.Email.Trim();
        record.Mobile = input.Mobile.Trim();
        record.Gender = input.Gender;
        record.DateOfBirth = input.DateOfBirth;
        record.Address = input.Address;
        record.City = input.City;
        record.State = input.State;
        record.PinCode = input.PinCode;
        record.Country = input.Country;
        record.OrganisationName = input.OrganisationName;
        record.OrganisationAddress = input.OrganisationAddress;
        record.SignatoryName = input.SignatoryName;
        record.Designation = input.Designation;
        record.MembershipNo = input.MembershipNo;
        record.StudentRegistrationNo = input.StudentRegistrationNo;
        record.OccupancyType = input.OccupancyType;
        record.GstDetailsRequired = input.GstDetailsRequired;
        record.Gstin = input.Gstin;
        record.SponsorMemberNo = input.SponsorMemberNo;
        record.Currency = fee.Currency;
        record.BaseFee = fee.BaseFee;
        record.GstRate = fee.GstRate;
        record.GstAmount = fee.GstAmount;
        record.TotalAmount = fee.TotalAmount;
        record.IsEarlyBird = fee.IsEarlyBird;

        await store.SaveRegistrationAsync(record);
        return record;
    }

    public async Task<RegistrationRecord?> FindAsync(string id) => await store.FindAsync(id);

    public async Task<IReadOnlyList<RegistrationRecord>> GetForEmailAsync(string email) =>
        await store.GetByEmailAsync(email);

    public async Task<IReadOnlyList<RegistrationRecord>> GetAllAsync() => await store.GetAllAsync();

    public async Task<IReadOnlyList<RegistrationRecord>> GetBySponsorMemberNoAsync(string memberNo) =>
        await store.GetBySponsorMemberNoAsync(memberNo);

    public async Task<int> CountDelegatesAsync(string memberNo) =>
        await store.CountBySponsorMemberNoAsync(memberNo);

    public async Task<PaymentResult?> SubmitOfflinePaymentAsync(RegistrationRecord record, string? reference)
    {
        if (record.PaymentStatus == PaymentStates.Paid) return null;

        record.PaymentStatus = PaymentStates.Verification;
        record.PaymentMethod = "Offline";
        record.PaymentReference = reference;
        record.PaymentMessage = "Payment proof submitted. The secretariat will verify it.";
        record.PaidAt = DateTimeOffset.UtcNow;
        await store.SaveRegistrationAsync(record);
        await notifications.OfflineProofSubmittedAsync(record);
        return new PaymentResult(true, reference ?? "OFFLINE", record.PaymentMessage);
    }

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(
        RegistrationRecord record,
        string method,
        string returnUrl)
    {
        if (record.PaymentStatus == PaymentStates.Paid)
            return new PaymentInitiationResult(false, null, "This registration is already paid.");

        var merchantRef = RegistrationStore.GenerateMerchantRef();
        var result = await gateway.InitiateAsync(new PaymentInitiationRequest(
            record.Id,
            merchantRef,
            record.TotalAmount,
            record.Currency,
            method,
            record.Email,
            record.Mobile,
            record.FullName,
            returnUrl));

        if (!result.Success) return result;

        await store.AddPaymentTransactionAsync(new PaymentTransaction
        {
            MerchantTxnRefNo = merchantRef,
            RegistrationId = record.Id,
            Amount = record.TotalAmount,
            Currency = record.Currency,
            Method = method,
            Status = PaymentTransactionStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return result;
    }

    public async Task<PaymentCompletion> CompletePaymentAsync(PaymentCallback callback)
    {
        var result = await gateway.VerifyCallbackAsync(callback);
        if (string.IsNullOrWhiteSpace(result.MerchantTxnRefNo))
            return new PaymentCompletion(result, null);

        var transaction = await store.FindPaymentTransactionAsync(result.MerchantTxnRefNo);
        if (transaction is null)
            return new PaymentCompletion(new PaymentCallbackResult(false, result.GatewayTransactionId,
                result.StatusCode, result.MerchantTxnRefNo, result.Amount,
                "Unknown payment reference."), null);

        if (transaction.Status == PaymentTransactionStatus.Success)
            return new PaymentCompletion(result, transaction.RegistrationId);

        var record = await store.FindAsync(transaction.RegistrationId);
        if (record is null)
            return new PaymentCompletion(new PaymentCallbackResult(false, result.GatewayTransactionId,
                result.StatusCode, result.MerchantTxnRefNo, result.Amount,
                "Registration for this payment no longer exists."), null);

        if (result.Amount.HasValue && Math.Abs(result.Amount.Value - transaction.Amount) > 0.01m)
        {
            result = new PaymentCallbackResult(false, result.GatewayTransactionId, result.StatusCode,
                result.MerchantTxnRefNo, result.Amount, "Paid amount does not match the registration fee.");
        }

        transaction.GatewayTransactionId = result.GatewayTransactionId;
        transaction.FailureReason = result.ErrorMessage;
        transaction.CompletedAt = DateTimeOffset.UtcNow;

        if (result.Success)
        {
            record.PaymentStatus = PaymentStates.Paid;
            record.PaymentMethod = transaction.Method;
            record.TransactionId = result.GatewayTransactionId ?? transaction.MerchantTxnRefNo;
            record.PaymentMessage = "Online payment received.";
            record.PaidAt ??= DateTimeOffset.UtcNow;
            transaction.Status = PaymentTransactionStatus.Success;

            await store.SaveRegistrationAsync(record);
            await notifications.PaymentSucceededAsync(record, transaction);
        }
        else
        {
            transaction.Status = PaymentTransactionStatus.Failed;
            await notifications.PaymentFailedAsync(record);
        }

        await store.SavePaymentTransactionAsync(transaction);
        return new PaymentCompletion(result, record.Id);
    }

    public async Task MarkPaidAsync(RegistrationRecord record)
    {
        record.PaymentStatus = PaymentStates.Paid;
        record.PaidAt ??= DateTimeOffset.UtcNow;
        record.PaymentMessage = "Verified by the secretariat.";
        await store.SaveRegistrationAsync(record);
        await notifications.PaymentVerifiedAsync(record);
    }
}
