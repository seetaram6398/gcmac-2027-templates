using Gcmac.Registration.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class CategoryModel(FeeCatalog fees) : BasePageModel
{
    public IReadOnlyList<(CategoryOption Option, FeeBreakdown Fee)> Pricing { get; private set; } = [];
    public bool Sponsor { get; private set; }
    public FeeBreakdown CorporateEarly { get; private set; } = null!;
    public FeeBreakdown CorporateLate { get; private set; } = null!;
    public FeeBreakdown MemberEarly { get; private set; } = null!;
    public FeeBreakdown MemberLate { get; private set; } = null!;
    public FeeBreakdown SpouseFee { get; private set; } = null!;
    public FeeBreakdown StudentFee { get; private set; } = null!;
    public FeeBreakdown ForeignFee { get; private set; } = null!;

    public IActionResult OnGet(string? category, bool sponsor = false)
    {
        ViewData["Title"] = "Choose category";
        Sponsor = sponsor;
        if (sponsor && string.IsNullOrWhiteSpace(HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo)))
            return RedirectToPage("/Account/VerifyMember", new { returnUrl = "/Register/AddDelegate" });
        if (!string.IsNullOrWhiteSpace(category) &&
            Enum.TryParse<DelegateCategory>(category, true, out var selected) &&
            Enum.IsDefined(selected))
        {
            return RedirectToPage("Form", new { category = selected.ToString(), sponsor });
        }

        var future = DateTimeOffset.UtcNow.AddYears(1);
        Pricing = fees.Categories.Select(c => (c, fees.Calculate(c.Category))).ToList();
        CorporateEarly = fees.Calculate(DelegateCategory.CorporateOthers);
        CorporateLate = fees.Calculate(DelegateCategory.CorporateOthers, future);
        MemberEarly = fees.Calculate(DelegateCategory.CmaPractice);
        MemberLate = fees.Calculate(DelegateCategory.CmaPractice, future);
        SpouseFee = fees.Calculate(DelegateCategory.Spouse);
        StudentFee = fees.Calculate(DelegateCategory.Student);
        ForeignFee = fees.Calculate(DelegateCategory.Foreign);
        return Page();
    }
}
