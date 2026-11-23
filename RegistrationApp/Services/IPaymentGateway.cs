namespace Gcmac.Registration.Services;

public sealed record PaymentResult(bool Success, string TransactionId, string Message);

public sealed record PaymentInitiationRequest(
    string RegistrationId,
    string MerchantTxnRefNo,
    decimal Amount,
    string Currency,
    string Method,
    string Email,
    string Mobile,
    string CustomerName,
    string ReturnUrl);

public sealed record PaymentInitiationResult(
    bool Success,
    string? RedirectUrl,
    string? ErrorMessage);

public sealed record PaymentCallback(
    string RawResponse,
    IReadOnlyDictionary<string, string?> Values);

public sealed record PaymentCallbackResult(
    bool Success,
    string? GatewayTransactionId,
    string? StatusCode,
    string? MerchantTxnRefNo,
    decimal? Amount,
    string? ErrorMessage);

public interface IPaymentGateway
{
    Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentCallbackResult> VerifyCallbackAsync(
        PaymentCallback callback,
        CancellationToken cancellationToken = default);
}

public sealed class DemoPaymentGateway : IPaymentGateway
{
    public Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        var redirect = $"/Register/PaymentReturn?demo=1&ref={Uri.EscapeDataString(request.MerchantTxnRefNo)}" +
                       $"&status=0300&amount={request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}";
        return Task.FromResult(new PaymentInitiationResult(true, redirect, null));
    }

    public Task<PaymentCallbackResult> VerifyCallbackAsync(
        PaymentCallback callback,
        CancellationToken cancellationToken = default)
    {
        if (!callback.Values.TryGetValue("demo", out var demo) || demo != "1")
            return Task.FromResult(new PaymentCallbackResult(false, null, null, null, null, "Invalid demo callback."));

        callback.Values.TryGetValue("ref", out var reference);
        callback.Values.TryGetValue("status", out var status);
        decimal? amount = null;
        if (callback.Values.TryGetValue("amount", out var amountText) &&
            decimal.TryParse(amountText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            amount = parsed;
        }

        var success = status == "0300";
        return Task.FromResult(new PaymentCallbackResult(
            success,
            success ? $"DEMO-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}" : null,
            status,
            reference,
            amount,
            success ? null : "Demo payment was not completed."));
    }
}
