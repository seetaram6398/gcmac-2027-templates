using System.Text.Json;
using Gcmac.Registration.Models;
using Gcmac.Registration.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Gcmac.Registration.Pages;

public abstract class BasePageModel : PageModel
{
    protected string? CurrentEmail => HttpContext.Session.GetString(SessionKeys.Email);
    protected bool IsAdmin => HttpContext.Session.GetString(SessionKeys.IsAdmin) == "true";
    protected bool IsSignedIn => !string.IsNullOrWhiteSpace(CurrentEmail);

    protected IActionResult? SignedOut(string returnUrl)
    {
        if (IsSignedIn) return null;
        var type = "delegate";
        if (returnUrl.Contains("CmaPractice", StringComparison.OrdinalIgnoreCase) ||
            returnUrl.Contains("AddDelegate", StringComparison.OrdinalIgnoreCase) ||
            returnUrl.Contains("VerifyMember", StringComparison.OrdinalIgnoreCase) ||
            returnUrl.Contains("/Member/", StringComparison.OrdinalIgnoreCase))
            type = "member";
        else if (returnUrl.Contains("CorporateOthers", StringComparison.OrdinalIgnoreCase))
            type = "corporate";
        else if (returnUrl.Contains("category=Student", StringComparison.OrdinalIgnoreCase))
            type = "student";
        else if (returnUrl.Contains("category=Foreign", StringComparison.OrdinalIgnoreCase))
            type = "foreign";
        else if (returnUrl.Contains("/Admin/", StringComparison.OrdinalIgnoreCase))
            type = "admin";
        return RedirectToPage("/Account/SignIn", new { type, returnUrl });
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

    protected string BuildAbsoluteUrl(string path)
    {
        var settings = HttpContext.RequestServices.GetRequiredService<IOptions<PaymentOptions>>().Value;
        var publicBase = settings.PublicBaseUrl?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(publicBase) &&
            Uri.TryCreate(publicBase, UriKind.Absolute, out var baseUri) &&
            (baseUri.Scheme == Uri.UriSchemeHttps || baseUri.Scheme == Uri.UriSchemeHttp))
            return $"{publicBase}{path}";

        return $"{Request.Scheme}://{Request.Host}{path}";
    }
}
