using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class PaymentReturnModel(RegistrationService service) : BasePageModel
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = "";
    public string? RegistrationId { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Payment status";

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Request.Query)
            values[pair.Key] = pair.Value.ToString();
        var raw = Request.QueryString.Value ?? "";

        var completion = await service.CompletePaymentAsync(new PaymentCallback(raw, values));
        Success = completion.Result.Success;
        Message = completion.Result.ErrorMessage ?? "Payment completed successfully.";
        RegistrationId = completion.RegistrationId;
        return Page();
    }
}
