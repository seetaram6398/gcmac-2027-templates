using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class AddDelegateModel(RegistrationService service) : BasePageModel
{
    [BindProperty] public RegistrationInput Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? EditingId { get; set; }

    public string MemberNo => HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo) ?? "";
    public string MemberName => HttpContext.Session.GetString(SessionKeys.VerifiedMemberName) ?? "";
    public int Limit => service.MaxDelegatesPerMember;
    public FeeBreakdown Fee => service.Fees.Calculate(DelegateCategory.CorporateOthers);
    public string CategoryName => service.Fees.NameOf(DelegateCategory.CorporateOthers);
    public string? Notice { get; private set; }
    public IReadOnlyList<RegistrationRecord> Delegates { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var signedOut = SignedOut("/Register/AddDelegate");
        if (signedOut is not null) return signedOut;
        if (string.IsNullOrWhiteSpace(MemberNo))
            return RedirectToPage("/Account/VerifyMember", new { returnUrl = "/Register/AddDelegate" });

        ViewData["Title"] = "Add delegate";
        Input = NewInput();
        await ReloadAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetEditAsync(string id)
    {
        var signedOut = SignedOut("/Register/AddDelegate");
        if (signedOut is not null) return signedOut;
        if (string.IsNullOrWhiteSpace(MemberNo))
            return RedirectToPage("/Account/VerifyMember", new { returnUrl = "/Register/AddDelegate" });

        var record = await service.FindAsync(id);
        if (record is null || !string.Equals(record.SponsorMemberNo, MemberNo, StringComparison.OrdinalIgnoreCase))
            return RedirectToPage("AddDelegate");

        Input = FromRecord(record);
        EditingId = record.Id;
        ViewData["Title"] = "Edit delegate";
        await ReloadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddDelegateAsync()
    {
        var guard = await GuardAsync();
        if (guard is not null) return guard;
        await ReloadAsync();
        if (Delegates.Count >= Limit)
        {
            ModelState.AddModelError("", "The delegate limit for this membership has been reached.");
            return Page();
        }
        if (!ValidateInput()) return Page();

        await service.CreateAsync(Input, CurrentEmail ?? MemberNo);
        Notice = "Delegate added. You can continue adding delegates or pay now.";
        Input = NewInput();
        await ReloadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostPayNowAsync()
    {
        var guard = await GuardAsync();
        if (guard is not null) return guard;
        await ReloadAsync();
        if (Delegates.Count >= Limit)
        {
            ModelState.AddModelError("", "The delegate limit for this membership has been reached.");
            return Page();
        }
        if (!ValidateInput()) return Page();

        var record = await service.CreateAsync(Input, CurrentEmail ?? MemberNo);
        return RedirectToPage("Payment", new { id = record.Id });
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        var guard = await GuardAsync();
        if (guard is not null) return guard;
        if (string.IsNullOrWhiteSpace(EditingId))
        {
            ModelState.AddModelError("", "Select a delegate to update.");
            await ReloadAsync();
            return Page();
        }
        if (!ValidateInput()) return Page();

        var updated = await service.UpdateAsync(EditingId, Input, MemberNo);
        if (updated is null)
        {
            ModelState.AddModelError("", "Delegate not found or you do not have permission to edit it.");
            await ReloadAsync();
            return Page();
        }

        Notice = "Delegate details updated.";
        EditingId = null;
        Input = NewInput();
        await ReloadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        var signedOut = SignedOut("/Register/AddDelegate");
        if (signedOut is not null) return signedOut;
        if (string.IsNullOrWhiteSpace(MemberNo))
            return RedirectToPage("/Account/VerifyMember", new { returnUrl = "/Register/AddDelegate" });

        Input = NewInput();
        EditingId = null;
        await ReloadAsync();
        return Page();
    }

    public string GenderLabel(string? gender) => gender switch
    {
        "M" => "Male",
        "F" => "Female",
        _ => gender ?? "—"
    };

    public string Text(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private async Task<IActionResult?> GuardAsync()
    {
        var signedOut = SignedOut("/Register/AddDelegate");
        if (signedOut is not null) return signedOut;
        if (string.IsNullOrWhiteSpace(MemberNo))
            return RedirectToPage("/Account/VerifyMember", new { returnUrl = "/Register/AddDelegate" });
        return null;
    }

    private bool ValidateInput()
    {
        Input.Category = DelegateCategory.CorporateOthers;
        Input.SponsorMemberNo = MemberNo;
        Input.MemberVerified = true;
        Input.RequireMemberVerification = false;
        Input.ConfirmEmail = Input.Email;
        Input.MembershipNo = Input.MembershipNo?.Trim();
        Input.Gstin = Input.GstDetailsRequired ? Input.Gstin?.Trim() : null;

        foreach (var error in Input.Validate()) ModelState.AddModelError("", error);
        return ModelState.IsValid;
    }

    private RegistrationInput NewInput() => new()
    {
        Category = DelegateCategory.CorporateOthers,
        SponsorMemberNo = MemberNo,
        MemberVerified = true,
        RequireMemberVerification = false,
        Country = "INDIA"
    };

    private static RegistrationInput FromRecord(RegistrationRecord record) => new()
    {
        Category = record.Category,
        FullName = record.FullName,
        Email = record.Email,
        ConfirmEmail = record.Email,
        Mobile = record.Mobile,
        Gender = record.Gender,
        Designation = record.Designation ?? "",
        Address = record.Address ?? "",
        State = record.State ?? "",
        City = record.City ?? "",
        PinCode = record.PinCode ?? "",
        Country = record.Country ?? "",
        OrganisationName = record.OrganisationName ?? "",
        OrganisationAddress = record.OrganisationAddress ?? "",
        SignatoryName = record.SignatoryName ?? "",
        MembershipNo = record.MembershipNo ?? "",
        OccupancyType = record.OccupancyType ?? "",
        GstDetailsRequired = record.GstDetailsRequired,
        Gstin = record.Gstin ?? "",
        SponsorMemberNo = record.SponsorMemberNo,
        MemberVerified = true,
        RequireMemberVerification = false,
        AgreeTerms = true
    };

    private async Task ReloadAsync() =>
        Delegates = await service.GetBySponsorMemberNoAsync(MemberNo);
}
