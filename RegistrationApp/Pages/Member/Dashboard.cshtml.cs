using Gcmac.Registration.Models;
using Gcmac.Registration.Options;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Gcmac.Registration.Pages.Member;

public sealed class DashboardModel(
    IMemberAccountService memberAccounts,
    RegistrationService registrations,
    IOptions<NotificationOptions> options) : BasePageModel
{
    public MemberAccount? Member { get; private set; }
    public IReadOnlyList<RegistrationRecord> Delegates { get; private set; } = [];
    public IReadOnlyList<RegistrationRecord> MyRegistrations { get; private set; } = [];
    public int DelegateLimit { get; } = options.Value.MaxDelegatesPerMember;

    public async Task<IActionResult> OnGetAsync()
    {
        var signedOut = SignedOut("/Member/Dashboard");
        if (signedOut is not null) return signedOut;

        var memberNo = HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo);
        if (string.IsNullOrWhiteSpace(memberNo))
            return RedirectToPage("/Account/Login", new { returnUrl = "/Member/Dashboard" });

        Member = await memberAccounts.GetStoredAsync(memberNo);
        if (Member is null)
            return RedirectToPage("/Account/Login", new { returnUrl = "/Member/Dashboard" });

        Delegates = await registrations.GetBySponsorMemberNoAsync(memberNo);
        MyRegistrations = await registrations.GetForEmailAsync(CurrentEmail ?? "");
        ViewData["Title"] = "Member dashboard";
        return Page();
    }

    public static string CategoryName(DelegateCategory category) => category switch
    {
        DelegateCategory.CorporateOthers => "Corporate Delegates & Others",
        DelegateCategory.CmaPractice => "CMA in Practice / Self Sponsored",
        DelegateCategory.Student => "Student",
        DelegateCategory.Foreign => "Foreign Delegate",
        DelegateCategory.Spouse => "Accompanying Spouse",
        _ => category.ToString()
    };
}
