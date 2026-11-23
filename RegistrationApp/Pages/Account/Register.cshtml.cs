using Gcmac.Registration.Data;
using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class RegisterModel(RegistrationStore store, PasswordHasher hasher) : BasePageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string ConfirmPassword { get; set; } = "";
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        ReturnUrl = returnUrl;
        Email = Email.Trim();

        if (!System.Text.RegularExpressions.Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            ModelState.AddModelError(nameof(Email), "Enter a valid email address.");
        if (Password.Length < 8)
            ModelState.AddModelError(nameof(Password), "Password must be at least 8 characters.");
        if (Password != ConfirmPassword)
            ModelState.AddModelError(nameof(ConfirmPassword), "Passwords do not match.");

        if (!ModelState.IsValid) return Page();

        if (await store.AccountExistsAsync(Email))
        {
            ModelState.AddModelError(nameof(Email), "An account with this email already exists. Please login.");
            return Page();
        }

        var (hash, salt) = hasher.Hash(Password);
        await store.AddAccountAsync(new Models.Account
        {
            Email = Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTimeOffset.UtcNow
        });

        HttpContext.Session.SetString(SessionKeys.Email, Email);
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Register/Category");
    }
}
