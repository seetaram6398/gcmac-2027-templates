using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class BatchPaymentModel(RegistrationService service) : BasePageModel
{
    [BindProperty] public string Method { get; set; } = "Card";
    [BindProperty] public string? Reference { get; set; }

    public string BatchId { get; private set; } = "";
    public IReadOnlyList<RegistrationRecord> Items { get; private set; } = [];
    public decimal TotalAmount => Items.Sum(item => item.TotalAmount);
    public string Currency => Items.FirstOrDefault()?.Currency ?? "INR";
    public string ErrorMessage { get; private set; } = "";
    public bool IsAdditional => Items.Any(item => item.IsAdditionalDelegate);
    public IReadOnlyList<string> Methods { get; } = ["Card", "UPI", "NetBanking", "Offline"];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var denied = await GuardAsync(id);
        if (denied is not null) return denied;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var denied = await GuardAsync(id);
        if (denied is not null) return denied;

        if (Method == "Offline")
        {
            if (string.IsNullOrWhiteSpace(Reference))
            {
                ErrorMessage = "Enter the payment reference or UTR number for offline payment.";
                return Page();
            }

            await service.SubmitOfflineBatchPaymentAsync(id, Reference.Trim());
            return RedirectToPage("Confirmation", new { id = Items[0].Id });
        }

        var returnUrl = BuildAbsoluteUrl("/Register/PaymentReturn");
        var result = await service.InitiateBatchPaymentAsync(id, Method, returnUrl);
        if (!result.Success || string.IsNullOrWhiteSpace(result.RedirectUrl))
        {
            ErrorMessage = result.ErrorMessage ?? "Unable to start payment. Try again later.";
            return Page();
        }

        return Redirect(result.RedirectUrl);
    }

    private async Task<IActionResult?> GuardAsync(string id)
    {
        var signedOut = SignedOut($"/Register/BatchPayment?id={Uri.EscapeDataString(id)}");
        if (signedOut is not null) return signedOut;

        BatchId = id;
        var memberNo = HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo);
        if (!await service.CanAccessPaymentBatchAsync(id, CurrentEmail, memberNo, IsAdmin))
            return RedirectToPage("MyRegistrations");

        Items = await service.GetPaymentBatchAsync(id);
        if (Items.Count == 0 || Items.All(item => item.PaymentStatus == PaymentStates.Paid))
            return RedirectToPage("Confirmation", new { id = Items.FirstOrDefault()?.Id });

        ViewData["Title"] = IsAdditional ? "Pay for added delegates" : "Pay registration & delegates";
        return null;
    }
}
