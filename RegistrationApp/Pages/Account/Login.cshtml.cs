using System.Text.Json;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class LoginModel(IMemberAccountService memberAccounts, IConfiguration configuration) : BasePageModel
{
    [BindProperty(SupportsGet = true)] public string? MembershipNo { get; set; }
    [BindProperty] public string? Otp { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty(SupportsGet = true)] public bool Created { get; set; }

    public bool ShowOtpWindow { get; private set; }
    public string? Notice { get; private set; }
    public string DemoSecretValue => DemoSecret();

    public void OnGet()
    {
        ViewData["Title"] = "CMA member login";
        if (Created) Notice = "Account has been created. Please login with your membership number and OTP.";
    }

    public IActionResult OnPostSendOtp()
    {
        MembershipNo = MembershipNo?.Trim() ?? "";
        if (MembershipNo.Length == 0)
        {
            ModelState.AddModelError("", "Membership number is required.");
            return Page();
        }

        ShowOtpWindow = true;
        Notice = $"Demo OTP: {DemoSecret()}";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        MembershipNo = MembershipNo?.Trim() ?? "";
        ShowOtpWindow = true;
        if (MembershipNo.Length == 0)
        {
            ModelState.AddModelError("", "Membership number is required.");
            return Page();
        }

        if (Otp != DemoSecret())
        {
            ModelState.AddModelError("", "Invalid OTP.");
            return Page();
        }

        var stored = await memberAccounts.GetStoredAsync(MembershipNo);
        if (stored is null || !stored.EmailVerified || !stored.MobileVerified)
        {
            ModelState.AddModelError("", "Please sign up and verify your email and mobile number before logging in.");
            return Page();
        }

        var load = await memberAccounts.LoadAsync(MembershipNo);
        if (load.Member is null)
        {
            ModelState.AddModelError("", load.Message);
            return Page();
        }

        var member = load.Member;
        HttpContext.Session.SetString(SessionKeys.Email,
            string.IsNullOrWhiteSpace(member.Email) ? member.MemberNo : member.Email);
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberNo, member.MemberNo);
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberName, member.Name);
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberDetails, JsonSerializer.Serialize(member));

        return Redirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/Member/Dashboard");
    }

    private string DemoSecret() => configuration["MemberLogin:DemoSecret"] ?? "123456";
}
