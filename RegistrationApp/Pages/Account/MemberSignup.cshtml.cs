using System.Text.Json;
using System.Text.RegularExpressions;
using Gcmac.Registration.Data;
using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class MemberSignupModel(
    IMemberAccountService memberAccounts,
    RegistrationStore store,
    RegistrationService registrationService,
    INotificationService notifications,
    PasswordHasher hasher,
    IConfiguration configuration) : BasePageModel
{
    [BindProperty] public string? MembershipNo { get; set; }
    [BindProperty] public string? MemberName { get; set; }
    [BindProperty] public string? MemberMobile { get; set; }
    [BindProperty] public string? MemberEmail { get; set; }
    [BindProperty] public bool DetailsLoaded { get; set; }
    [BindProperty] public string? EmailOtp { get; set; }
    [BindProperty] public string? MobileOtp { get; set; }
    [BindProperty] public bool EmailOtpSent { get; set; }
    [BindProperty] public bool MobileOtpSent { get; set; }
    [BindProperty] public bool EmailVerified { get; set; }
    [BindProperty] public bool MobileVerified { get; set; }
    [BindProperty] public bool EmailOtpWindowOpen { get; set; }
    [BindProperty] public bool MobileOtpWindowOpen { get; set; }
    [BindProperty] public string? MemberDesignation { get; set; }
    [BindProperty] public string? MemberAddress { get; set; }
    [BindProperty] public string? MemberCity { get; set; }
    [BindProperty] public string? MemberState { get; set; }
    [BindProperty] public string? MemberPinCode { get; set; }
    [BindProperty] public string? MemberCountry { get; set; }
    [BindProperty] public string? DelegateName { get; set; }
    [BindProperty] public string? DelegateDesignation { get; set; }
    [BindProperty] public string? DelegateMobile { get; set; }
    [BindProperty] public string? DelegateEmail { get; set; }
    [BindProperty] public string? DelegateAddress { get; set; }
    [BindProperty] public string? DelegateState { get; set; }
    [BindProperty] public string? DelegateCity { get; set; }
    [BindProperty] public string? DelegatePinCode { get; set; }
    [BindProperty] public bool DelegateAgreeTerms { get; set; }

    public string? Notice { get; private set; }
    public string? SuccessMessage { get; private set; }
    public string DemoSecretValue => DemoSecret();

    public int OtpValidityMinutes => int.TryParse(configuration["MemberLogin:OtpValidityMinutes"], out var minutes) && minutes > 0
        ? minutes
        : 10;
    public FeeBreakdown DelegateFee => registrationService.Fees.Calculate(DelegateCategory.CmaPractice);
    public string DelegateCategoryName => registrationService.Fees.NameOf(DelegateCategory.CmaPractice);

    public void OnGet() => ViewData["Title"] = "CMA member signup";

    public async Task<IActionResult> OnPostGetDetailsAsync()
    {
        MembershipNo = MembershipNo?.Trim() ?? "";
        if (MembershipNo.Length == 0)
        {
            ModelState.AddModelError("", "Membership number is required.");
            return Page();
        }

        var load = await memberAccounts.LoadAsync(MembershipNo);
        if (load.Member is null)
        {
            ModelState.AddModelError("", load.Message);
            return Page();
        }

        MemberName = load.Member.Name;
        MemberMobile = load.Member.Mobile;
        MemberEmail = load.Member.Email;
        MemberDesignation = load.Member.Designation;
        MemberAddress = load.Member.AddressLine1;
        MemberCity = load.Member.City;
        MemberState = load.Member.State;
        MemberPinCode = load.Member.PinCode;
        MemberCountry = load.Member.Country;
        MembershipNo = load.Member.MemberNo;
        DetailsLoaded = true;
        PrefillDelegate(load.Member);
        EmailOtp = null;
        MobileOtp = null;
        EmailOtpSent = false;
        MobileOtpSent = false;
        EmailVerified = false;
        MobileVerified = false;
        EmailOtpWindowOpen = false;
        MobileOtpWindowOpen = false;
        Notice = string.IsNullOrWhiteSpace(load.Message)
            ? "Member details fetched from the ICMAI directory. Verify your email and mobile to continue."
            : $"{load.Message} Verify your email and mobile to continue.";
        return Page();
    }

    public async Task<IActionResult> OnPostSendEmailOtpAsync()
    {
        if (!DetailsLoaded || string.IsNullOrWhiteSpace(MemberEmail))
        {
            ModelState.AddModelError("", "Fetch your member details first.");
            return Page();
        }

        EmailOtp = null;
        EmailOtpSent = true;
        EmailOtpWindowOpen = true;
        MobileOtpWindowOpen = false;
        await notifications.LoginOtpAsync(MemberEmail!, NotificationChannel.Email, DemoSecret(), OtpValidityMinutes);
        Notice = $"OTP sent to your email id. Demo OTP: {DemoSecretValue}";
        return Page();
    }

    public async Task<IActionResult> OnPostSendMobileOtpAsync()
    {
        if (!DetailsLoaded || string.IsNullOrWhiteSpace(MemberMobile))
        {
            ModelState.AddModelError("", "Fetch your member details first.");
            return Page();
        }

        MobileOtp = null;
        MobileOtpSent = true;
        MobileOtpWindowOpen = true;
        EmailOtpWindowOpen = false;
        await notifications.LoginOtpAsync(MemberMobile!, NotificationChannel.Sms, DemoSecret(), OtpValidityMinutes);
        Notice = $"OTP sent to your mobile number. Demo OTP: {DemoSecretValue}";
        return Page();
    }

    public async Task<IActionResult> OnPostValidateEmailOtpAsync()
    {
        if (!DetailsLoaded || !EmailOtpSent)
        {
            ModelState.AddModelError("", "Send the email OTP first.");
            return Page();
        }

        if (EmailOtp != DemoSecret())
        {
            await notifications.OtpFailedAsync(MemberEmail ?? "", NotificationChannel.Email);
            ModelState.AddModelError("", "Invalid email OTP.");
            EmailOtpWindowOpen = true;
            return Page();
        }

        EmailVerified = true;
        EmailOtp = null;
        EmailOtpWindowOpen = false;
        Notice = "Email OTP verified.";
        return Page();
    }

    public async Task<IActionResult> OnPostValidateMobileOtpAsync()
    {
        if (!DetailsLoaded || !MobileOtpSent)
        {
            ModelState.AddModelError("", "Send the mobile OTP first.");
            return Page();
        }

        if (MobileOtp != DemoSecret())
        {
            await notifications.OtpFailedAsync(MemberMobile ?? "", NotificationChannel.Sms);
            ModelState.AddModelError("", "Invalid mobile OTP.");
            MobileOtpWindowOpen = true;
            return Page();
        }

        MobileVerified = true;
        MobileOtp = null;
        MobileOtpWindowOpen = false;
        Notice = "Mobile OTP verified.";
        return Page();
    }

    public IActionResult OnPostCancelEmailOtp()
    {
        EmailOtp = null;
        EmailOtpWindowOpen = false;
        return Page();
    }

    public IActionResult OnPostCancelMobileOtp()
    {
        MobileOtp = null;
        MobileOtpWindowOpen = false;
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAccountAsync()
    {
        var member = await LoadVerifiedMemberAsync();
        if (member is null) return Page();

        await EnsureAccountAsync(member);
        await memberAccounts.MarkVerifiedAsync(member, true, true);
        SuccessMessage = "Account has been created. Please login with your membership number and OTP.";
        return RedirectToPage("/Account/Login", new { membershipNo = member.MemberNo, created = true });
    }

    public async Task<IActionResult> OnPostCreateDelegateAsync()
    {
        var member = await LoadVerifiedMemberAsync();
        if (member is null) return Page();

        await EnsureAccountAsync(member);
        await memberAccounts.MarkVerifiedAsync(member, true, true);

        var input = new RegistrationInput
        {
            Category = DelegateCategory.CmaPractice,
            FullName = DelegateName ?? "",
            Designation = DelegateDesignation ?? "",
            Mobile = DelegateMobile ?? "",
            Email = DelegateEmail ?? "",
            ConfirmEmail = DelegateEmail ?? "",
            Address = DelegateAddress ?? "",
            State = DelegateState ?? "",
            City = DelegateCity ?? "",
            PinCode = DelegatePinCode ?? "",
            Country = member.Country ?? "",
            MembershipNo = member.MemberNo,
            MemberVerified = true,
            RequireMemberVerification = true,
            AgreeTerms = DelegateAgreeTerms
        };

        foreach (var error in input.Validate()) ModelState.AddModelError("", error);
        if (!ModelState.IsValid) return Page();

        var record = await registrationService.CreateAsync(input, member.Email!);
        SetMemberSession(member);
        return RedirectToPage("/Register/Payment", new { id = record.Id });
    }

    private async Task<MemberAccount?> LoadVerifiedMemberAsync()
    {
        MembershipNo = MembershipNo?.Trim() ?? "";
        if (!DetailsLoaded || string.IsNullOrWhiteSpace(MembershipNo))
        {
            ModelState.AddModelError("", "Fetch your member details first.");
            return null;
        }

        if (!EmailVerified || !MobileVerified)
        {
            ModelState.AddModelError("", "Verify both email and mobile OTPs before continuing.");
            return null;
        }

        var load = await memberAccounts.LoadAsync(MembershipNo);
        if (load.Member is null)
        {
            ModelState.AddModelError("", load.Message);
            return null;
        }

        var member = load.Member;
        if (string.IsNullOrWhiteSpace(member.Email) ||
            !Regex.IsMatch(member.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ModelState.AddModelError("", "A valid email id is not available for this membership. Please contact the secretariat.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(member.Mobile))
        {
            ModelState.AddModelError("", "A mobile number is not available for this membership. Please contact the secretariat.");
            return null;
        }

        return member;
    }

    private async Task EnsureAccountAsync(MemberAccount member)
    {
        if (await store.AccountExistsAsync(member.Email!)) return;

        var generatedPassword = $"Mbr@{Guid.NewGuid():N}";
        var (hash, salt) = hasher.Hash(generatedPassword);
        await store.AddAccountAsync(new Models.Account
        {
            Email = member.Email!,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await notifications.AccountCreatedAsync(member.Email!, member.Mobile);
    }

    private void PrefillDelegate(MemberAccount member)
    {
        DelegateName = member.Name ?? "";
        DelegateDesignation = member.Designation ?? "";
        DelegateMobile = member.Mobile ?? "";
        DelegateEmail = member.Email ?? "";
        DelegateAddress = member.AddressLine1 ?? "";
        DelegateState = member.State ?? "";
        DelegateCity = member.City ?? "";
        DelegatePinCode = member.PinCode ?? "";
    }

    private void SetMemberSession(MemberAccount member)
    {
        HttpContext.Session.SetString(SessionKeys.Email, member.Email ?? member.MemberNo);
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberNo, member.MemberNo);
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberName, member.Name ?? "");
        HttpContext.Session.SetString(SessionKeys.VerifiedMemberDetails, JsonSerializer.Serialize(member));
    }

    private string DemoSecret() => configuration["MemberLogin:DemoSecret"] ?? "123456";
}
