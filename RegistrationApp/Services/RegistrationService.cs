using Gcmac.Registration.Data;
using Gcmac.Registration.Models;
using Gcmac.Registration.Options;
using Microsoft.Extensions.Options;

namespace Gcmac.Registration.Services;

public sealed record PaymentCompletion(
    PaymentCallbackResult Result,
    string? RegistrationId,
    string? BatchId);

public sealed class RegistrationService(
    RegistrationStore store,
    FeeCatalog fees,
    IPaymentGateway gateway,
    INotificationService notifications,
    IOptions<NotificationOptions> notificationOptions)
{
    public FeeCatalog Fees => fees;

    public int MaxDelegatesPerMember => notificationOptions.Value.MaxDelegatesPerMember;

    public int MaxAdditionalDelegatesPerMember => notificationOptions.Value.MaxAdditionalDelegatesPerMember;

    public async Task<RegistrationRecord> CreateAsync(
        RegistrationInput input,
        string accountEmail,
        bool isAdditionalDelegate = false)
    {
        var record = RegistrationRecord.From(input, RegistrationStore.GenerateId(), accountEmail,
            fees.Calculate(input.Category));
        record.IsAdditionalDelegate = isAdditionalDelegate;
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

    public async Task<int> CountAdditionalDelegatesAsync(string memberNo) =>
        await store.CountAdditionalBySponsorMemberNoAsync(memberNo);

    public async Task<string> CreatePaymentBatchAsync(
        IReadOnlyList<RegistrationRecord> records,
        string accountEmail,
        string sponsorMemberNo,
        string kind)
    {
        var items = records.Where(r => !string.IsNullOrWhiteSpace(r.Id)).ToList();
        if (items.Count == 0) throw new ArgumentException("No registrations to pay for.", nameof(records));

        var batchId = RegistrationStore.GenerateBatchId();
        foreach (var record in items)
        {
            record.PaymentBatchId = batchId;
            record.AccountEmail = accountEmail;
            if (string.IsNullOrWhiteSpace(record.SponsorMemberNo) &&
                !string.Equals(record.MembershipNo, sponsorMemberNo, StringComparison.OrdinalIgnoreCase))
                record.SponsorMemberNo = sponsorMemberNo;
        }

        await store.SaveRegistrationsAsync(items);
        return batchId;
    }

    public async Task<(string BatchId, IReadOnlyList<RegistrationRecord> Items)> CreateCombinedPaymentBatchAsync(
        string accountEmail,
        string? memberNo = null,
        string kind = PaymentBatchKinds.Registration)
    {
        var byEmail = await store.GetByEmailAsync(accountEmail);
        var byMember = string.IsNullOrWhiteSpace(memberNo)
            ? Array.Empty<RegistrationRecord>()
            : await store.GetBySponsorMemberNoAsync(memberNo);

        var items = byEmail.Concat(byMember)
            .Where(r => r.PaymentStatus == PaymentStates.Pending)
            .GroupBy(r => r.Id)
            .Select(group => group.First())
            .OrderBy(r => r.CreatedAt)
            .ToList();

        if (items.Count == 0) throw new ArgumentException("No pending registrations to pay for.");

        var batchId = RegistrationStore.GenerateBatchId();
        foreach (var record in items)
        {
            record.PaymentBatchId = batchId;
            record.AccountEmail = accountEmail;
            if (string.IsNullOrWhiteSpace(record.SponsorMemberNo) &&
                !string.Equals(record.MembershipNo, memberNo, StringComparison.OrdinalIgnoreCase))
                record.SponsorMemberNo = memberNo;
        }

        await store.SaveRegistrationsAsync(items);
        return (batchId, items);
    }

    public async Task<IReadOnlyList<RegistrationRecord>> GetPaymentBatchAsync(string batchId) =>
        await store.GetByPaymentBatchIdAsync(batchId);

    public async Task<bool> CanAccessPaymentBatchAsync(
        string batchId,
        string? accountEmail,
        string? memberNo,
        bool isAdmin)
    {
        var records = await store.GetByPaymentBatchIdAsync(batchId);
        if (records.Count == 0) return false;
        if (isAdmin) return true;

        return records.Any(record =>
            (!string.IsNullOrWhiteSpace(accountEmail) &&
             string.Equals(record.AccountEmail, accountEmail, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(memberNo) &&
             (string.Equals(record.SponsorMemberNo, memberNo, StringComparison.OrdinalIgnoreCase) ||
              string.Equals(record.MembershipNo, memberNo, StringComparison.OrdinalIgnoreCase))));
    }

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

    public async Task<PaymentResult?> SubmitOfflineBatchPaymentAsync(string batchId, string? reference)
    {
        var records = await store.GetByPaymentBatchIdAsync(batchId);
        if (records.Count == 0 || records.All(r => r.PaymentStatus == PaymentStates.Paid)) return null;

        foreach (var record in records)
        {
            record.PaymentStatus = PaymentStates.Verification;
            record.PaymentMethod = "Offline";
            record.PaymentReference = reference;
            record.PaymentMessage = "Payment proof submitted. The secretariat will verify it.";
        }

        await store.SaveRegistrationsAsync(records);
        foreach (var record in records)
            await notifications.OfflineProofSubmittedAsync(record);

        return new PaymentResult(true, reference ?? "OFFLINE", "Payment proof submitted for the whole batch.");
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

    public async Task<PaymentInitiationResult> InitiateBatchPaymentAsync(
        string batchId,
        string method,
        string returnUrl)
    {
        var records = await store.GetByPaymentBatchIdAsync(batchId);
        if (records.Count == 0)
            return new PaymentInitiationResult(false, null, "Payment batch not found.");
        if (records.Any(r => r.PaymentStatus == PaymentStates.Paid))
            return new PaymentInitiationResult(false, null, "This payment batch is already paid.");

        var currency = records[0].Currency;
        if (records.Any(r => !string.Equals(r.Currency, currency, StringComparison.OrdinalIgnoreCase)))
            return new PaymentInitiationResult(false, null,
                "This batch has registrations in different currencies. Pay them separately.");

        var primary = records[0];
        var amount = records.Sum(r => r.TotalAmount);
        var merchantRef = RegistrationStore.GenerateMerchantRef();
        var result = await gateway.InitiateAsync(new PaymentInitiationRequest(
            batchId,
            merchantRef,
            amount,
            currency,
            method,
            primary.Email,
            primary.Mobile,
            primary.FullName,
            returnUrl));

        if (!result.Success) return result;

        await store.AddPaymentTransactionAsync(new PaymentTransaction
        {
            MerchantTxnRefNo = merchantRef,
            RegistrationId = batchId,
            BatchId = batchId,
            RegistrationIdsCsv = string.Join(",", records.Select(r => r.Id)),
            Kind = primary.IsAdditionalDelegate
                ? PaymentBatchKinds.AdditionalDelegate
                : PaymentBatchKinds.Registration,
            Amount = amount,
            Currency = currency,
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
            return new PaymentCompletion(result, null, null);

        var transaction = await store.FindPaymentTransactionAsync(result.MerchantTxnRefNo);
        if (transaction is null)
            return new PaymentCompletion(new PaymentCallbackResult(false, result.GatewayTransactionId,
                result.StatusCode, result.MerchantTxnRefNo, result.Amount,
                "Unknown payment reference."), null, null);

        if (transaction.Status == PaymentTransactionStatus.Success)
            return new PaymentCompletion(result, transaction.RegistrationId, transaction.BatchId);

        var isBatch = !string.IsNullOrWhiteSpace(transaction.BatchId);
        var records = isBatch
            ? await store.GetByPaymentBatchIdAsync(transaction.BatchId!)
            : (await store.FindAsync(transaction.RegistrationId) is { } single ? [single] : []);
        if (records.Count == 0)
            return new PaymentCompletion(new PaymentCallbackResult(false, result.GatewayTransactionId,
                result.StatusCode, result.MerchantTxnRefNo, result.Amount,
                "Registration for this payment no longer exists."), null, transaction.BatchId);

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
            foreach (var record in records)
            {
                record.PaymentStatus = PaymentStates.Paid;
                record.PaymentMethod = transaction.Method;
                record.TransactionId = result.GatewayTransactionId ?? transaction.MerchantTxnRefNo;
                record.PaymentMessage = "Online payment received.";
                record.PaidAt ??= DateTimeOffset.UtcNow;
            }

            await store.SaveRegistrationsAsync(records);
            transaction.Status = PaymentTransactionStatus.Success;

            foreach (var record in records)
                await notifications.PaymentSucceededAsync(record, transaction);
        }
        else
        {
            transaction.Status = PaymentTransactionStatus.Failed;
            foreach (var record in records)
                await notifications.PaymentFailedAsync(record);
        }

        await store.SavePaymentTransactionAsync(transaction);
        return new PaymentCompletion(result, records[0].Id, transaction.BatchId);
    }

    public async Task MarkPaidAsync(RegistrationRecord record)
    {
        record.PaymentStatus = PaymentStates.Paid;
        record.PaidAt ??= DateTimeOffset.UtcNow;
        record.PaymentMessage = "Verified by the secretariat.";
        await store.SaveRegistrationAsync(record);
        await notifications.PaymentVerifiedAsync(record);
    }

    public async Task MarkBatchPaidAsync(string batchId)
    {
        var records = await store.GetByPaymentBatchIdAsync(batchId);
        foreach (var record in records)
        {
            if (record.PaymentStatus == PaymentStates.Paid) continue;
            await MarkPaidAsync(record);
        }
    }
}
