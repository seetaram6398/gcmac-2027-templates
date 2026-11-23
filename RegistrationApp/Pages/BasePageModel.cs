using System.Text.Json;
using Gcmac.Registration.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Gcmac.Registration.Pages;

public abstract class BasePageModel : PageModel
{
    protected string? CurrentEmail => HttpContext.Session.GetString(SessionKeys.Email);
    protected bool IsAdmin => HttpContext.Session.GetString(SessionKeys.IsAdmin) == "true";
    protected bool IsSignedIn => !string.IsNullOrWhiteSpace(CurrentEmail);

    protected IActionResult? SignedOut(string returnUrl)
    {
        if (IsSignedIn) return null;
        var page = "/Account/EmailLogin";
        if (returnUrl.Contains("CmaPractice", StringComparison.OrdinalIgnoreCase) ||
            returnUrl.Contains("AddDelegate", StringComparison.OrdinalIgnoreCase) ||
            returnUrl.Contains("VerifyMember", StringComparison.OrdinalIgnoreCase) ||
            returnUrl.Contains("/Member/", StringComparison.OrdinalIgnoreCase))
            page = "/Account/Login";
        else if (returnUrl.Contains("CorporateOthers", StringComparison.OrdinalIgnoreCase))
            page = "/Account/CorporateLogin";
        else if (returnUrl.Contains("category=Student", StringComparison.OrdinalIgnoreCase))
            page = "/Account/StudentLogin";
        else if (returnUrl.Contains("category=Foreign", StringComparison.OrdinalIgnoreCase))
            page = "/Account/ForeignLogin";
        return RedirectToPage(page, new { returnUrl });
    }

    protected bool CanView(RegistrationRecord record) =>
        IsAdmin ||
        string.Equals(record.AccountEmail, CurrentEmail, StringComparison.OrdinalIgnoreCase) ||
        HttpContext.Session.GetString(SessionKeys.TrackAccess) == record.Id;

    protected RegistrationInput? GetDraft()
    {
        var json = HttpContext.Session.GetString(SessionKeys.Draft);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<RegistrationInput>(json);
    }

    protected void SetDraft(RegistrationInput draft) =>
        HttpContext.Session.SetString(SessionKeys.Draft, JsonSerializer.Serialize(draft));

    protected void ClearDraft() => HttpContext.Session.Remove(SessionKeys.Draft);
}
