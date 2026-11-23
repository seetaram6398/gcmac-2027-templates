using System.Text.Json;
using Gcmac.Registration.Models;
using Gcmac.Registration.Options;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Gcmac.Registration.Pages.Register;

public sealed class FormModel(RegistrationService service, IOptions<MemberApiOptions> memberOptions) : BasePageModel
{
    private readonly bool _requireVerification = memberOptions.Value.RequireVerification;

    [BindProperty] public RegistrationInput Input { get; set; } = new();
    [BindProperty] public bool SponsorMode { get; set; }
    public FeeBreakdown Fee => service.Fees.Calculate(Input.Category);
    public string CategoryName => service.Fees.NameOf(Input.Category);
    public string? Notice { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? category, bool sponsor = false, string? occupancy = null, string? regNo = null, string? dob = null)
    {
        var signedOut = SignedOut($"/Register/Form?category={category}&sponsor={sponsor}&occupancy={Uri.EscapeDataString(occupancy ?? "")}&regNo={Uri.EscapeDataString(regNo ?? "")}&dob={Uri.EscapeDataString(dob ?? "")}");
        if (signedOut is not null) return signedOut;
        ViewData["Title"] = "Delegate details";
        SponsorMode = sponsor;
        Input.RequireMemberVerification = _requireVerification;

        var draft = GetDraft();
        if (draft is not null && (string.IsNullOrWhiteSpace(category) ||
                                  string.Equals(draft.Category.ToString(), category, StringComparison.OrdinalIgnoreCase)))
        {
            Input = draft;
        }
        else if (Enum.TryParse<DelegateCategory>(category, true, out var selected) && Enum.IsDefined(selected))
        {
            Input.Category = selected;
        }
        else
        {
            return RedirectToPage("Category", new { sponsor });
        }

        if (!string.IsNullOrWhiteSpace(occupancy)) Input.OccupancyType = occupancy;
        if (!string.IsNullOrWhiteSpace(regNo)) Input.StudentRegistrationNo = regNo;
        if (!string.IsNullOrWhiteSpace(dob)) Input.DateOfBirth = dob;

        Input.RequireMemberVerification = _requireVerification;
        var memberNo = HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo);
        if (SponsorMode)
        {
            if (string.IsNullOrWhiteSpace(memberNo))
                return RedirectToPage("/Account/VerifyMember", new { returnUrl = "/Register/AddDelegate" });

            if (await service.CountDelegatesAsync(memberNo) >= service.MaxDelegatesPerMember)
            {
                Notice = "The delegate limit for this membership has been reached.";
                return Page();
            }

            Input.SponsorMemberNo = memberNo;
            Input.MemberVerified = true;
        }
        else if (Input.Category == DelegateCategory.CmaPractice && !string.IsNullOrWhiteSpace(memberNo))
        {
            Input.MembershipNo = memberNo;
            Input.MemberVerified = true;
            ApplyMemberDetails();
        }

        if (!SponsorMode && string.IsNullOrWhiteSpace(Input.Email)) Input.Email = CurrentEmail ?? "";
        Input.ConfirmEmail = Input.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var signedOut = SignedOut($"/Register/Form?category={Input.Category}&sponsor={SponsorMode}");
        if (signedOut is not null) return signedOut;

        var memberNo = HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo);
        if (SponsorMode)
        {
            if (string.IsNullOrWhiteSpace(memberNo))
                return RedirectToPage("/Account/VerifyMember", new { returnUrl = "/Register/AddDelegate" });

            if (await service.CountDelegatesAsync(memberNo) >= service.MaxDelegatesPerMember)
            {
                Notice = "The delegate limit for this membership has been reached.";
                return Page();
            }

            Input.SponsorMemberNo = memberNo;
            Input.MemberVerified = true;
        }
        else if (Input.Category == DelegateCategory.CmaPractice && _requireVerification)
        {
            if (string.IsNullOrWhiteSpace(memberNo))
            {
                ModelState.AddModelError("", "Verify your ICMAI membership before registering as a CMA delegate.");
                return Page();
            }

            Input.MembershipNo = memberNo;
            Input.MemberVerified = true;
        }

        foreach (var error in Input.Validate()) ModelState.AddModelError("", error);
        if (!ModelState.IsValid) return Page();

        Input.Email = Input.Email.Trim();
        Input.ConfirmEmail = Input.Email;
        SetDraft(Input);
        return RedirectToPage("Review");
    }

    private void ApplyMemberDetails()
    {
        var json = HttpContext.Session.GetString(SessionKeys.VerifiedMemberDetails);
        if (string.IsNullOrWhiteSpace(json)) return;

        var member = JsonSerializer.Deserialize<MemberDetails>(json);
        if (member is null) return;

        Input.FullName = string.IsNullOrWhiteSpace(Input.FullName) ? member.Name ?? "" : Input.FullName;
        Input.Mobile = string.IsNullOrWhiteSpace(Input.Mobile) ? member.Mobile ?? "" : Input.Mobile;
        Input.Email = string.IsNullOrWhiteSpace(Input.Email) ? member.Email ?? "" : Input.Email;
        Input.Designation = string.IsNullOrWhiteSpace(Input.Designation) ? member.Designation ?? "" : Input.Designation;
        Input.OrganisationName = string.IsNullOrWhiteSpace(Input.OrganisationName) ? member.Organisation ?? "" : Input.OrganisationName;
        Input.Address = string.IsNullOrWhiteSpace(Input.Address) ? member.AddressLine1 ?? "" : Input.Address;
        Input.City = string.IsNullOrWhiteSpace(Input.City) ? member.City ?? "" : Input.City;
        Input.State = string.IsNullOrWhiteSpace(Input.State) ? member.State ?? "" : Input.State;
        Input.Country = string.IsNullOrWhiteSpace(Input.Country) ? member.Country ?? "" : Input.Country;
        Input.PinCode = string.IsNullOrWhiteSpace(Input.PinCode) ? member.PinCode ?? "" : Input.PinCode;
    }
}
