using System.Text.Json;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class VerifyMemberModel(IMemberDirectory directory) : BasePageModel
{
    [BindProperty] public string MemberNumber { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? Message { get; private set; }

    public IActionResult OnGet()
    {
        var signedOut = SignedOut($"/Account/VerifyMember?returnUrl={Uri.EscapeDataString(ReturnUrl ?? "")}");
        if (signedOut is not null) return signedOut;
        ViewData["Title"] = "Verify ICMAI membership";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var signedOut = SignedOut($"/Account/VerifyMember?returnUrl={Uri.EscapeDataString(ReturnUrl ?? "")}");
        if (signedOut is not null) return signedOut;

        var result = await directory.LookupAsync(MemberNumber);
        if (result.Status != MemberLookupStatus.Found || result.Member is null)
        {
            Message = result.Message;
            return Page();
        }

        HttpContext.Session.SetString(SessionKeys.VerifiedMemberNo, result.Member.MemberNo);
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberName, result.Member.Name ?? "");
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberDetails, JsonSerializer.Serialize(result.Member));

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl);

        return RedirectToPage("/Register/AddDelegate");
    }
}
