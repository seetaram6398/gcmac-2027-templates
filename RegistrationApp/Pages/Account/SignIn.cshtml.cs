using Gcmac.Registration.Data;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class SignInModel(
    RegistrationStore store,
    PasswordHasher hasher,
    IMemberAccountService memberAccounts,
    IConfiguration configuration) : BasePageModel
{
    [BindProperty] public string UserType { get; set; } = "delegate";
    [BindProperty(SupportsGet = true)] public string? Type { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string MembershipNo { get; set; } = "";
    [BindProperty] public string Otp { get; set; } = "";
    [BindProperty] public string RegistrationNo { get; set; } = "";
    [BindProperty] public string DateOfBirth { get; set; } = "";
    [BindProperty] public string Category { get; set; } = "";
    [BindProperty] public string SecurityCode { get; set; } = "";
    [BindProperty] public bool SendOtp { get; set; }

    public string ActiveType { get; private set; } = "delegate";
    public bool ShowOtpWindow { get; private set; }
    public string? Notice { get; private set; }
    public string DemoSecretValue => configuration["MemberLogin:DemoSecret"] ?? "123456";

    public void OnGet()
    {
        ViewData["Title"] = "Sign in";
        ActiveType = Normalize(Type);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ActiveType = Normalize(UserType);
        if (ActiveType == "member") return await MemberAsync();
        if (ActiveType == "corporate") return Corporate();
        if (ActiveType == "student") return Student();
        if (ActiveType == "foreign") return Foreign();
        if (ActiveType == "admin") return Admin();
        return await DelegateAsync();
    }

    private async Task<IActionResult> DelegateAsync()
    {
        Email = Email.Trim();
        var account = await store.FindAccountAsync(Email);
        var valid = account is not null && hasher.Verify(Password, account.PasswordHash, account.PasswordSalt);
        if (!valid)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return Page();
        }

        HttpContext.Session.SetString(SessionKeys.Email, Email);
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        return Redirect(LocalReturnUrl("/Register/Category"));
    }

    private IActionResult Admin()
    {
        var adminEmail = configuration["Admin:Email"] ?? "";
        var adminPassword = configuration["Admin:Password"] ?? "";
        if (string.IsNullOrWhiteSpace(adminEmail) ||
            !string.Equals(Email.Trim(), adminEmail, StringComparison.OrdinalIgnoreCase) ||
            Password != adminPassword)
        {
            ModelState.AddModelError("", "Invalid admin credentials.");
            return Page();
        }

        HttpContext.Session.SetString(SessionKeys.Email, Email.Trim());
        HttpContext.Session.SetString(SessionKeys.IsAdmin, "true");
        return Redirect(LocalReturnUrl("/Admin/Index"));
    }

    private async Task<IActionResult> MemberAsync()
    {
        ShowOtpWindow = true;
        MembershipNo = MembershipNo.Trim();
        if (MembershipNo.Length == 0)
        {
            ModelState.AddModelError("", "Membership number is required.");
            return Page();
        }

        if (SendOtp)
        {
            Notice = $"OTP sent to your registered email and mobile number. Demo OTP: {DemoSecretValue}";
            return Page();
        }

        if ((Otp ?? "").Trim() != DemoSecretValue)
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
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberDetails, System.Text.Json.JsonSerializer.Serialize(member));
        return Redirect(LocalReturnUrl("/Member/Dashboard"));
    }

    private IActionResult Corporate()
    {
        var secret = DemoSecretValue;
        if (Password != secret || SecurityCode != secret)
        {
            ModelState.AddModelError("", "Invalid password or security code.");
            return Page();
        }
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Category))
        {
            ModelState.AddModelError("", "Email and category are required.");
            return Page();
        }

        HttpContext.Session.SetString(SessionKeys.Email, Email.Trim());
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        return Redirect($"/Register/Form?category=CorporateOthers&occupancy={Uri.EscapeDataString(Category)}");
    }

    private IActionResult Student()
    {
        if (SecurityCode != DemoSecretValue)
        {
            ModelState.AddModelError("", "Invalid security code.");
            return Page();
        }
        if (string.IsNullOrWhiteSpace(RegistrationNo) || string.IsNullOrWhiteSpace(DateOfBirth))
        {
            ModelState.AddModelError("", "Registration number and date of birth are required.");
            return Page();
        }

        var regNo = RegistrationNo.Trim();
        HttpContext.Session.SetString(SessionKeys.Email, $"{regNo}@students.gcmac.local");
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        return Redirect($"/Register/Form?category=Student&regNo={Uri.EscapeDataString(regNo)}&dob={Uri.EscapeDataString(DateOfBirth.Trim())}");
    }

    private IActionResult Foreign()
    {
        if (Password != DemoSecretValue || SecurityCode != DemoSecretValue)
        {
            ModelState.AddModelError("", "Invalid password or security code.");
            return Page();
        }
        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError("", "Email is required.");
            return Page();
        }

        HttpContext.Session.SetString(SessionKeys.Email, Email.Trim());
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        return Redirect("/Register/Form?category=Foreign");
    }

    private string LocalReturnUrl(string fallback) => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : fallback;

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "member" => "member",
        "corporate" => "corporate",
        "student" => "student",
        "foreign" => "foreign",
        "admin" => "admin",
        _ => "delegate"
    };
}
