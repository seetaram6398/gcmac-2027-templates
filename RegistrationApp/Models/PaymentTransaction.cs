namespace Gcmac.Registration.Models;

public static class PaymentTransactionStatus
{
    public const string Pending = "Pending";
    public const string Success = "Success";
    public const string Failed = "Failed";
}

public static class PaymentBatchKinds
{
    public const string Single = "Single";
    public const string Registration = "Registration";
    public const string AdditionalDelegate = "AdditionalDelegate";
}

public sealed class PaymentTransaction
{
    public string MerchantTxnRefNo { get; set; } = "";
    public string RegistrationId { get; set; } = "";
    public string? BatchId { get; set; }
    public string RegistrationIdsCsv { get; set; } = "";
    public string Kind { get; set; } = PaymentBatchKinds.Single;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Method { get; set; } = "";
    public string Status { get; set; } = PaymentTransactionStatus.Pending;
    public string? GatewayTransactionId { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public IReadOnlyList<string> BatchRegistrationIds() =>
        RegistrationIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
