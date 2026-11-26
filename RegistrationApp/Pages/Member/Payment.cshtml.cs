using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Member;

public sealed record MemberQrRegistration(
    RegistrationRecord Record,
    string QrDataUri,
    string QrText);

public sealed class PaymentModel(RegistrationService registrations) : BasePageModel
{
    [BindProperty(SupportsGet = true)] public string? Ids { get; set; }
    public IReadOnlyList<MemberQrRegistration> Items { get; private set; } = [];
    public string? Notice { get; private set; }
    public decimal TotalAmount => Items.Sum(item => item.Record.TotalAmount);

    public async Task<IActionResult> OnGetAsync()
    {
        var denied = SignedOut("/Member/Payment");
        if (denied is not null) return denied;

        var memberNo = HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo) ?? "";
        if (string.IsNullOrWhiteSpace(memberNo))
            return RedirectToPage("/Account/SignIn", new { type = "member", returnUrl = "/Member/Payment" });

        var ids = (Ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (ids.Length == 0)
        {
            Notice = "No registrations selected. Save a registration first.";
            return Page();
        }

        var items = new List<MemberQrRegistration>();
        foreach (var id in ids)
        {
            var record = await registrations.FindAsync(id);
            if (record is null || !OwnedBy(record, memberNo)) continue;

            var text = DelegatePasses.QrText(record);
            items.Add(new MemberQrRegistration(record, QrCodes.DataUri(text), text));
        }

        Items = items;
        if (items.Count == 0) Notice = "No matching registrations found for this member.";
        return Page();
    }

    private static bool OwnedBy(RegistrationRecord record, string memberNo) =>
        string.Equals(record.SponsorMemberNo, memberNo, StringComparison.OrdinalIgnoreCase) ||
        (record.Category == DelegateCategory.CmaPractice &&
         string.Equals(record.MembershipNo, memberNo, StringComparison.OrdinalIgnoreCase));

}
