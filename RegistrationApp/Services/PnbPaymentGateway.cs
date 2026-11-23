using System.Globalization;
using Gcmac.Registration.Options;
using Microsoft.Extensions.Options;

namespace Gcmac.Registration.Services;

public sealed class PnbPaymentGateway(
    IOptions<PaymentOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<PnbPaymentGateway> logger) : IPaymentGateway
{
    private static readonly string[] SuccessStatuses = ["0300", "0200"];

    public async Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value.Pnb;
        if (!IsConfigured(settings))
            return new PaymentInitiationResult(false, null,
                "PNB payment gateway is not configured. Add the institute-supplied settings to configuration.");

        var amount = request.Amount.ToString("F2", CultureInfo.InvariantCulture);
        var fields = new Dictionary<string, string>
        {
            ["requesttype"] = "T",
            ["merchantcode"] = settings.MerchantCode,
            ["MerchantTxnRefNo"] = request.MerchantTxnRefNo,
            ["amount"] = amount,
            ["currencycode"] = request.Currency,
            ["uniqueCustomerID"] = request.RegistrationId,
            ["returnURL"] = request.ReturnUrl,
            ["TPSLTXNID"] = "",
            ["Shoppingcartdetails"] = $"FIRST_{amount}_0",
            ["TxnDate"] = DateTime.UtcNow.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            ["Email"] = request.Email,
            ["mobileNo"] = request.Mobile,
            ["Bankcode"] = settings.BankCode,
            ["customerName"] = request.CustomerName,
            ["CardID"] = "",
            ["AccountNo"] = "",
            ["IsKey"] = settings.Key,
            ["IsIv"] = settings.Iv,
            ["ITC"] = $"~name:{request.CustomerName}~regid:{request.RegistrationId}"
        };

        var responseText = await PostAsync(settings.RequestUrl, fields, cancellationToken);
        if (responseText is null)
            return new PaymentInitiationResult(false, null, "Payment gateway did not respond. Try again later.");

        if (responseText.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
            return new PaymentInitiationResult(false, null, $"Payment gateway rejected the request ({responseText}).");

        if (!Uri.TryCreate(responseText, UriKind.Absolute, out var redirect) ||
            redirect.Scheme != Uri.UriSchemeHttps && redirect.Scheme != Uri.UriSchemeHttp)
            return new PaymentInitiationResult(false, null, "Payment gateway returned an unexpected redirect.");

        return new PaymentInitiationResult(true, redirect.ToString(), null);
    }

    public async Task<PaymentCallbackResult> VerifyCallbackAsync(
        PaymentCallback callback,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value.Pnb;
        if (!IsConfigured(settings))
            return new PaymentCallbackResult(false, null, null, null, null,
                "PNB payment gateway is not configured.");

        callback.Values.TryGetValue("msg", out var message);
        var raw = string.IsNullOrWhiteSpace(message) ? callback.RawResponse : message;
        if (string.IsNullOrWhiteSpace(raw))
            return new PaymentCallbackResult(false, null, null, null, null, "Missing payment gateway response.");

        var responseText = await PostAsync(settings.VerifyUrl,
            new Dictionary<string, string> { ["msg"] = raw }, cancellationToken);
        if (responseText is null)
            return new PaymentCallbackResult(false, null, null, null, null,
                "Payment gateway did not respond. The transaction will be checked again.");

        if (responseText.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
            return new PaymentCallbackResult(false, null, null, null, null,
                $"Payment verification failed ({responseText}).");

        var values = ParsePipeSeparated(responseText);
        values.TryGetValue("TXN_STATUS", out var status);
        values.TryGetValue("TPSL_TXN_ID", out var transactionId);
        values.TryGetValue("CLNT_TXN_REF", out var reference);
        values.TryGetValue("TXN_ERR_MSG", out var error);

        decimal? amount = null;
        if (values.TryGetValue("TXN_AMT", out var amountText) &&
            decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            amount = parsed;
        }

        var success = status is not null && SuccessStatuses.Contains(status);
        return new PaymentCallbackResult(success, transactionId, status, reference, amount,
            success ? null : error ?? "Payment was not completed.");
    }

    private async Task<string?> PostAsync(
        string url,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            using var content = new FormUrlEncodedContent(fields);
            using var response = await client.PostAsync(url, content, cancellationToken);
            var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Payment gateway returned HTTP {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            return body;
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Payment gateway network error.");
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Payment gateway request timed out.");
            return null;
        }
    }

    private static bool IsConfigured(PnbPaymentOptions settings) =>
        !string.IsNullOrWhiteSpace(settings.RequestUrl) &&
        !string.IsNullOrWhiteSpace(settings.VerifyUrl) &&
        !string.IsNullOrWhiteSpace(settings.MerchantCode) &&
        !string.IsNullOrWhiteSpace(settings.Key) &&
        !string.IsNullOrWhiteSpace(settings.Iv);

    private static Dictionary<string, string> ParsePipeSeparated(string response)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in response.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0) continue;
            result[pair[..separator].Trim()] = pair[(separator + 1)..].Trim();
        }

        return result;
    }
}
