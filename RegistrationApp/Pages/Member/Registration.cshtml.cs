using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Member;

public sealed class RegistrationModel(
    IMemberAccountService memberAccounts,
    RegistrationService registrations) : BasePageModel
{
    [BindProperty] public RegistrationInput Input { get; set; } = new();
    [BindProperty] public List<RegistrationInput> Delegates { get; set; } = [];

    public string MemberNo { get; private set; } = "";
    public MemberAccount? Member { get; private set; }
    public IReadOnlyList<RegistrationRecord> SavedDelegates { get; private set; } = [];
    public IReadOnlyList<RegistrationRecord> SavedMemberRegistrations { get; private set; } = [];
    public IReadOnlyList<string> SavedMembershipIds { get; private set; } = [];
    public int Limit => registrations.MaxDelegatesPerMember;
    public bool CanAddDelegate => SavedDelegates.Count < Limit;
    public string? Notice { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var denied = await GuardAsync();
        if (denied is not null) return denied;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var denied = await GuardAsync();
        if (denied is not null) return denied;

        ApplyServerValues(Input, DelegateCategory.CmaPractice);
        foreach (var error in Input.Validate()) ModelState.AddModelError("", error);

        var submitted = Delegates.Where(HasData).ToList();
        var available = Math.Max(0, Limit - SavedDelegates.Count);
        if (submitted.Count > available)
            ModelState.AddModelError("", $"You can add only {available} more delegate(s) under membership {MemberNo}.");

        for (var index = 0; index < submitted.Count; index++)
        {
            var delegateInput = submitted[index];
            ApplyServerValues(delegateInput, DelegateCategory.CorporateOthers);
            foreach (var error in delegateInput.Validate())
                ModelState.AddModelError("", $"Delegate {index + 1}: {error}");
        }

        if (!ModelState.IsValid)
        {
            await ReloadAsync();
            return Page();
        }

        var accountEmail = CurrentEmail ?? MemberNo;
        var created = new List<RegistrationRecord>
        {
            await registrations.CreateAsync(Input, accountEmail)
        };
        foreach (var delegateInput in submitted)
            created.Add(await registrations.CreateAsync(delegateInput, accountEmail));

        SavedMembershipIds = created.Select(r =>
            $"{r.FullName} — {r.MembershipId}").ToList();
        Notice = $"Saved {created.Count} registration(s) under membership {MemberNo}. Membership IDs are generated from your membership number and each delegate's name.";

        Input = NewMemberInput();
        Delegates = [];
        await ReloadAsync();
        return Page();
    }

    private async Task<IActionResult?> GuardAsync()
    {
        var signedOut = SignedOut("/Member/Registration");
        if (signedOut is not null) return signedOut;

        MemberNo = HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo) ?? "";
        if (string.IsNullOrWhiteSpace(MemberNo))
            return RedirectToPage("/Account/SignIn", new { type = "member", returnUrl = "/Member/Registration" });

        Member = await memberAccounts.GetStoredAsync(MemberNo);
        if (Member is null)
            return RedirectToPage("/Account/SignIn", new { type = "member", returnUrl = "/Member/Registration" });

        if (string.IsNullOrEmpty(Input.FullName) && Delegates.Count == 0)
        {
            Input = NewMemberInput();
            Delegates = Enumerable.Range(0, Limit).Select(_ => new RegistrationInput()).ToList();
        }
        await ReloadAsync();
        return null;
    }

    private async Task ReloadAsync()
    {
        while (Delegates.Count < Limit) Delegates.Add(new RegistrationInput());

        var sponsored = await registrations.GetBySponsorMemberNoAsync(MemberNo);
        SavedDelegates = sponsored.Where(r =>
            r.Category != DelegateCategory.CmaPractice ||
            !string.Equals(r.MembershipNo, MemberNo, StringComparison.OrdinalIgnoreCase)).ToList();

        var mine = await registrations.GetForEmailAsync(CurrentEmail ?? "");
        SavedMemberRegistrations = mine.Where(r =>
            r.Category == DelegateCategory.CmaPractice &&
            string.Equals(r.MembershipNo, MemberNo, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void ApplyServerValues(RegistrationInput input, DelegateCategory category)
    {
        input.Category = category;
        input.FullName ??= "";
        input.Email ??= "";
        input.Mobile ??= "";
        input.Address ??= "";
        input.City ??= "";
        input.State ??= "";
        input.PinCode ??= "";
        input.MembershipNo = MemberNo;
        input.SponsorMemberNo = category == DelegateCategory.CmaPractice ? null : MemberNo;
        input.MemberVerified = true;
        input.RequireMemberVerification = false;
        input.ConfirmEmail = input.Email.Trim();
        input.Country = string.IsNullOrWhiteSpace(input.Country) ? "India" : input.Country.Trim();
        if (!input.GstDetailsRequired) input.Gstin = null;
    }

    private static bool HasData(RegistrationInput input) =>
        new[] { input.FullName, input.Email, input.Mobile, input.Designation, input.Address,
                input.City, input.State, input.PinCode, input.OccupancyType,
                input.OrganisationName, input.SignatoryName, input.OrganisationAddress }
            .Any(value => !string.IsNullOrWhiteSpace(value));

    private RegistrationInput NewMemberInput() => new()
    {
        Category = DelegateCategory.CmaPractice,
        MembershipNo = MemberNo,
        MemberVerified = true,
        RequireMemberVerification = false,
        FullName = Member?.Name ?? "",
        Email = Member?.Email ?? "",
        ConfirmEmail = Member?.Email ?? "",
        Mobile = Member?.Mobile ?? "",
        Designation = Member?.Designation ?? "",
        OrganisationName = Member?.Organisation ?? "",
        Address = Member?.AddressLine1 ?? "",
        City = Member?.City ?? "",
        State = Member?.State ?? "",
        PinCode = Member?.PinCode ?? "",
        Country = string.IsNullOrWhiteSpace(Member?.Country) ? "India" : Member!.Country
    };
}
