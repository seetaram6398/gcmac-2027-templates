using Gcmac.Registration.Data;
using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class SignInModel(
    RegistrationStore store,
    PasswordHasher hasher,
    IMemberAccountService memberAccounts,
    INotificationService notifications,
    IConfiguration configuration) : BasePageModel
{
    [BindProperty] public string UserType { get; set; } = "delegate";
    [BindProperty(SupportsGet = true)] public string? Type { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string MembershipNo { get; set; } = "";
    [BindProperty] public string Otp { get; set; } = "";
    [BindProperty] public string OtpChannel { get; set; } = "email";
    [BindProperty] public string RegistrationNo { get; set; } = "";
    [BindProperty] public string DateOfBirth { get; set; } = "";
    [BindProperty] public string Category { get; set; } = "";
    [BindProperty] public string SecurityCode { get; set; } = "";
    [BindProperty] public bool SendOtp { get; set; }

    public string ActiveType { get; private set; } = "delegate";
    public bool ShowOtpWindow { get; private set; }
    public string? Notice { get; private set; }
    public string DemoSecretValue => configuration["MemberLogin:DemoSecret"] ?? "123456";

    public int OtpValidityMinutes => int.TryParse(configuration["MemberLogin:OtpValidityMinutes"], out var minutes) && minutes > 0
        ? minutes
        : 10;

    public void OnGet()
    {
        ViewData["Title"] = "Sign in";
        if (HttpContext.Request.Path.StartsWithSegments("/registration", StringComparison.OrdinalIgnoreCase))
        {
            Type ??= "delegate";
            ReturnUrl ??= "/Register/MyRegistrations";
        }
        ActiveType = Normalize(Type);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ActiveType = Normalize(UserType);
        ModelState.Clear();
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
        OtpChannel = NormalizeOtpChannel(OtpChannel);
        if (MembershipNo.Length == 0)
        {
            ModelState.AddModelError("", "Membership number is required.");
            return Page();
        }

        var stored = await memberAccounts.GetStoredAsync(MembershipNo);
        if (stored is null)
        {
            ModelState.AddModelError("", "Please sign up and verify your member details before logging in.");
            return Page();
        }

        var channel = OtpChannel == "sms" ? NotificationChannel.Sms : NotificationChannel.Email;
        var recipient = channel == NotificationChannel.Sms ? stored.Mobile : stored.Email;
        if (string.IsNullOrWhiteSpace(recipient) || !IsChannelVerified(stored, channel))
        {
            ModelState.AddModelError("", OtpChannel == "sms"
                ? "Please verify your mobile number before logging in via SMS."
                : "Please verify your email id before logging in via email.");
            return Page();
        }

        if (SendOtp)
        {
            var otp = DemoSecretValue;
            HttpContext.Session.SetString(SessionKeys.LoginOtp, otp);
            HttpContext.Session.SetString(SessionKeys.LoginOtpMembershipNo, MembershipNo);
            HttpContext.Session.SetString(SessionKeys.LoginOtpChannel, OtpChannel);
            await notifications.LoginOtpAsync(recipient, channel, otp, OtpValidityMinutes);
            Notice = $"OTP sent via {(OtpChannel == "sms" ? "SMS" : "email")} to {Mask(recipient)}. Demo OTP: {otp}";
            return Page();
        }

        var expectedOtp = HttpContext.Session.GetString(SessionKeys.LoginOtp);
        var otpMembershipNo = HttpContext.Session.GetString(SessionKeys.LoginOtpMembershipNo);
        if (string.IsNullOrWhiteSpace(expectedOtp) || !string.Equals(otpMembershipNo, MembershipNo, StringComparison.Ordinal))
        {
            expectedOtp = DemoSecretValue;
        }

        if ((Otp ?? "").Trim() != expectedOtp)
        {
            await notifications.OtpFailedAsync(recipient, channel);
            ModelState.AddModelError("", "Invalid OTP.");
            return Page();
        }

        var load = await memberAccounts.LoadAsync(MembershipNo);
        if (load.Member is null)
        {
            ModelState.AddModelError("", load.Message);
            return Page();
        }

        var member = load.Member;
        HttpContext.Session.Remove(SessionKeys.LoginOtp);
        HttpContext.Session.Remove(SessionKeys.LoginOtpMembershipNo);
        HttpContext.Session.Remove(SessionKeys.LoginOtpChannel);
        HttpContext.Session.SetString(SessionKeys.Email,
            string.IsNullOrWhiteSpace(member.Email) ? member.MemberNo : member.Email);
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberNo, member.MemberNo);
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberName, member.Name);
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberDetails, System.Text.Json.JsonSerializer.Serialize(member));
        await notifications.LoginSucceededAsync(stored.Email, stored.Mobile, channel);
        return Redirect(LocalReturnUrl("/Member/Dashboard"));
    }

    private static bool IsChannelVerified(MemberAccount member, NotificationChannel channel) =>
        channel == NotificationChannel.Sms ? member.MobileVerified : member.EmailVerified;

    private static string NormalizeOtpChannel(string? value) =>
        string.Equals(value, "sms", StringComparison.OrdinalIgnoreCase) ? "sms" : "email";

    private static string Mask(string recipient) => recipient.Length <= 4
        ? recipient
        : $"{recipient[..2]}••••{recipient[^2..]}";

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
