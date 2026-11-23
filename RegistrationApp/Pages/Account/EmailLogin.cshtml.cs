using Gcmac.Registration.Data;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class EmailLoginModel(RegistrationStore store, PasswordHasher hasher, IConfiguration configuration) : BasePageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public void OnGet() => ViewData["Title"] = "Delegate login";

    public async Task<IActionResult> OnPostAsync()
    {
        Email = Email.Trim();

        var adminEmail = configuration["Admin:Email"] ?? "";
        var adminPassword = configuration["Admin:Password"] ?? "";
        var isAdmin = string.Equals(Email, adminEmail, StringComparison.OrdinalIgnoreCase)
                      && Password == adminPassword;

        var account = await store.FindAccountAsync(Email);
        var validUser = account is not null && hasher.Verify(Password, account.PasswordHash, account.PasswordSalt);

        if (!isAdmin && !validUser)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return Page();
        }

        HttpContext.Session.SetString(SessionKeys.Email, Email);
        if (isAdmin)
            HttpContext.Session.SetString(SessionKeys.IsAdmin, "true");
        else
            HttpContext.Session.Remove(SessionKeys.IsAdmin);

        return Redirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/Register/Category");
    }
}
