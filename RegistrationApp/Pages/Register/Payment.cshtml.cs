using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class PaymentModel(RegistrationService service) : BasePageModel
{
    [BindProperty] public string Method { get; set; } = "Card";
    [BindProperty] public string? Reference { get; set; }
    public RegistrationRecord Registration { get; private set; } = new();
    public string ErrorMessage { get; private set; } = "";
    public IReadOnlyList<string> Methods { get; } = ["Card", "UPI", "NetBanking", "Offline"];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var signedOut = SignedOut($"/Register/Payment?id={id}");
        if (signedOut is not null) return signedOut;
        ViewData["Title"] = "Payment";

        var record = await service.FindAsync(id);
        if (record is null || (!IsAdmin && !string.Equals(record.AccountEmail, CurrentEmail, StringComparison.OrdinalIgnoreCase)))
            return RedirectToPage("MyRegistrations");

        if (record.PaymentStatus == PaymentStates.Paid)
            return RedirectToPage("Confirmation", new { id = record.Id });

        Registration = record;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var signedOut = SignedOut($"/Register/Payment?id={id}");
        if (signedOut is not null) return signedOut;

        var record = await service.FindAsync(id);
        if (record is null || (!IsAdmin && !string.Equals(record.AccountEmail, CurrentEmail, StringComparison.OrdinalIgnoreCase)))
            return RedirectToPage("MyRegistrations");

        if (record.PaymentStatus == PaymentStates.Paid)
            return RedirectToPage("Confirmation", new { id = record.Id });

        if (Method == "Offline")
        {
            if (string.IsNullOrWhiteSpace(Reference))
            {
                Registration = record;
                ErrorMessage = "Enter the payment reference or UTR number for offline payment.";
                return Page();
            }

            await service.SubmitOfflinePaymentAsync(record, Reference.Trim());
            return RedirectToPage("Confirmation", new { id = record.Id });
        }

        var returnUrl = $"{Request.Scheme}://{Request.Host}/Register/PaymentReturn";
        var result = await service.InitiatePaymentAsync(record, Method, returnUrl);
        if (!result.Success || string.IsNullOrWhiteSpace(result.RedirectUrl))
        {
            Registration = record;
            ErrorMessage = result.ErrorMessage ?? "Unable to start payment. Try again later.";
            return Page();
        }

        return Redirect(result.RedirectUrl);
    }
}
