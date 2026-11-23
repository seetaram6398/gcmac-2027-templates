using Gcmac.Registration.Data;
using Gcmac.Registration.Models;
using Gcmac.Registration.Options;
using Microsoft.Extensions.Options;

namespace Gcmac.Registration.Services;

public interface INotificationService
{
    Task RegistrationSubmittedAsync(RegistrationRecord record, CancellationToken cancellationToken = default);
    Task PaymentSucceededAsync(RegistrationRecord record, PaymentTransaction transaction, CancellationToken cancellationToken = default);
    Task OfflineProofSubmittedAsync(RegistrationRecord record, CancellationToken cancellationToken = default);
    Task PaymentFailedAsync(RegistrationRecord record, CancellationToken cancellationToken = default);
    Task PaymentVerifiedAsync(RegistrationRecord record, CancellationToken cancellationToken = default);
    Task AdminAlertAsync(string templateKey, IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default);
}

public sealed class NotificationService(
    RegistrationStore store,
    IOptions<NotificationOptions> options,
    ILogger<NotificationService> logger) : INotificationService
{
    private static readonly Dictionary<string, string> Templates = new()
    {
        ["account_created"] = "GCMAC 2027: Account created for {Email}. Register your delegate pass at {PaymentLink}.",
        ["registration_submitted"] = "GCMAC 2027: Registration {RegId} received for {Name} ({Category}). Pay {Amount}: {PaymentLink}. Fee is non-refundable.",
        ["payment_success"] = "GCMAC 2027: Payment received for {RegId}. Txn {TxnId}, {Amount}. Receipt: {ReceiptLink}.",
        ["offline_submitted"] = "GCMAC 2027: Payment ref {UTR} received for {RegId}. Status: Under Verification. You will be informed within 2 working days.",
        ["offline_verified"] = "GCMAC 2027: Payment verified for {RegId}. Your registration is CONFIRMED. Receipt: {ReceiptLink}.",
        ["payment_failed"] = "GCMAC 2027: Payment for {RegId} could not be completed. Retry: {PaymentLink}.",
        ["details_updated"] = "GCMAC 2027: Details for registration {RegId} were updated. Verify at {TrackLink}.",
        ["admin_new_registration"] = "GCMAC 2027 Admin: New {RegId} - {Name}, {Category}, {Amount}.",
        ["admin_offline_proof"] = "GCMAC 2027 Admin: Offline ref {UTR} for {RegId}, {Amount}. Verify in Admin panel."
    };

    public async Task RegistrationSubmittedAsync(RegistrationRecord record, CancellationToken cancellationToken = default)
    {
        var values = Common(record);
        values["PaymentLink"] = $"/Register/Payment?id={Uri.EscapeDataString(record.Id)}";
        await EnqueueAsync("registration_submitted", record, values, cancellationToken);
        await AdminAsync("admin_new_registration", values, cancellationToken);
    }

    public async Task PaymentSucceededAsync(
        RegistrationRecord record,
        PaymentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var values = Common(record);
        values["TxnId"] = transaction.GatewayTransactionId ?? transaction.MerchantTxnRefNo;
        values["ReceiptLink"] = $"/Register/Receipt?id={Uri.EscapeDataString(record.Id)}";
        await EnqueueAsync("payment_success", record, values, cancellationToken);
    }

    public async Task OfflineProofSubmittedAsync(RegistrationRecord record, CancellationToken cancellationToken = default)
    {
        var values = Common(record);
        values["UTR"] = record.PaymentReference ?? "";
        await EnqueueAsync("offline_submitted", record, values, cancellationToken);
        await AdminAsync("admin_offline_proof", values, cancellationToken);
    }

    public async Task PaymentFailedAsync(RegistrationRecord record, CancellationToken cancellationToken = default)
    {
        var values = Common(record);
        values["PaymentLink"] = $"/Register/Payment?id={Uri.EscapeDataString(record.Id)}";
        await EnqueueAsync("payment_failed", record, values, cancellationToken);
    }

    public async Task PaymentVerifiedAsync(RegistrationRecord record, CancellationToken cancellationToken = default)
    {
        var values = Common(record);
        values["ReceiptLink"] = $"/Register/Receipt?id={Uri.EscapeDataString(record.Id)}";
        await EnqueueAsync("offline_verified", record, values, cancellationToken);
    }

    public async Task AdminAlertAsync(
        string templateKey,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        if (!Templates.ContainsKey(templateKey))
        {
            logger.LogWarning("Unknown notification template {TemplateKey}.", templateKey);
            return;
        }

        var settings = options.Value;
        if (!string.IsNullOrWhiteSpace(settings.AdminEmail))
            await EnqueueAsync(templateKey, NotificationChannel.Email, settings.AdminEmail, values, cancellationToken);
        if (!string.IsNullOrWhiteSpace(settings.AdminMobile))
            await EnqueueAsync(templateKey, NotificationChannel.Sms, settings.AdminMobile, values, cancellationToken);
    }

    private Task AdminAsync(string templateKey, IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken) =>
        AdminAlertAsync(templateKey, values, cancellationToken);

    private async Task EnqueueAsync(
        string templateKey,
        RegistrationRecord record,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(record.Email))
            await EnqueueAsync(templateKey, NotificationChannel.Email, record.Email, values, cancellationToken);
        if (!string.IsNullOrWhiteSpace(record.Mobile))
            await EnqueueAsync(templateKey, NotificationChannel.Sms, record.Mobile, values, cancellationToken);
    }

    private async Task EnqueueAsync(
        string templateKey,
        NotificationChannel channel,
        string recipient,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        if (!Templates.TryGetValue(templateKey, out var template))
        {
            logger.LogWarning("Unknown notification template {TemplateKey}.", templateKey);
            return;
        }

        var body = template;
        foreach (var (key, value) in values)
            body = body.Replace($"{{{key}}}", value, StringComparison.Ordinal);

        var settings = options.Value;
        string? templateId = channel == NotificationChannel.Sms
            ? settings.Sms.TemplateIds.GetValueOrDefault(templateKey)
            : null;

        await store.EnqueueNotificationAsync(new NotificationMessage
        {
            Channel = channel,
            Recipient = recipient,
            TemplateKey = templateKey,
            TemplateId = templateId,
            Body = body
        }, cancellationToken);
    }

    private Dictionary<string, string> Common(RegistrationRecord record) => new()
    {
        ["Name"] = record.FullName,
        ["RegId"] = record.Id,
        ["Category"] = record.Category.ToString(),
        ["Amount"] = $"{record.Currency} {record.TotalAmount:F2}",
        ["Email"] = record.Email,
        ["TrackLink"] = $"/Register/Track?id={Uri.EscapeDataString(record.Id)}"
    };
}
