using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class ReviewModel(RegistrationService service) : BasePageModel
{
    public RegistrationInput Input { get; private set; } = new();
    public FeeBreakdown Fee => service.Fees.Calculate(Input.Category);
    public string CategoryName => service.Fees.NameOf(Input.Category);

    public IActionResult OnGet()
    {
        var signedOut = SignedOut("/Register/Review");
        if (signedOut is not null) return signedOut;
        ViewData["Title"] = "Review registration";

        var draft = GetDraft();
        if (draft is null) return RedirectToPage("Category");
        Input = draft;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var signedOut = SignedOut("/Register/Review");
        if (signedOut is not null) return signedOut;

        var draft = GetDraft();
        if (draft is null) return RedirectToPage("Category");

        if (!string.IsNullOrWhiteSpace(draft.SponsorMemberNo))
        {
            var memberNo = HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo);
            if (!string.Equals(memberNo, draft.SponsorMemberNo, StringComparison.OrdinalIgnoreCase) ||
                await service.CountDelegatesAsync(draft.SponsorMemberNo) >= service.MaxDelegatesPerMember)
            {
                return RedirectToPage("AddDelegate");
            }
        }

        var record = await service.CreateAsync(draft, CurrentEmail!);
        ClearDraft();
        return RedirectToPage("Payment", new { id = record.Id });
    }
}
