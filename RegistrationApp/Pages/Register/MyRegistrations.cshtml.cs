using Gcmac.Registration.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed record PaymentBatchSummary(
    string Id,
    int Count,
    decimal Amount,
    string Currency,
    string PaymentStatus,
    string FirstRegistrationId);

public sealed class MyRegistrationsModel(Services.RegistrationService service, Models.FeeCatalog fees) : BasePageModel
{
    public IReadOnlyList<Models.RegistrationRecord> Registrations { get; private set; } = [];
    public IReadOnlyList<Models.RegistrationRecord> PendingRegistrations { get; private set; } = [];
    public IReadOnlyList<PaymentBatchSummary> Batches { get; private set; } = [];
    public string AccountEmail => CurrentEmail ?? "";
    public decimal PendingTotal => PendingRegistrations.Sum(r => r.TotalAmount);
    public string PendingCurrency => PendingRegistrations.FirstOrDefault()?.Currency ?? "INR";
    public string CategoryName(Models.DelegateCategory category) => fees.NameOf(category);

    public async Task<IActionResult> OnGetAsync()
    {
        var signedOut = SignedOut("/Register/MyRegistrations");
        if (signedOut is not null) return signedOut;
        ViewData["Title"] = "My registrations";
        var byEmail = await service.GetForEmailAsync(CurrentEmail!);
        var memberNo = HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo) ?? "";
        var byMember = string.IsNullOrWhiteSpace(memberNo)
            ? Array.Empty<Models.RegistrationRecord>()
            : await service.GetBySponsorMemberNoAsync(memberNo);
        Registrations = byEmail.Concat(byMember)
            .GroupBy(r => r.Id)
            .Select(group => group.First())
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        PendingRegistrations = Registrations
            .Where(r => r.PaymentStatus == PaymentStates.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToList();
        Batches = Registrations
            .Where(r => !string.IsNullOrWhiteSpace(r.PaymentBatchId))
            .GroupBy(r => r.PaymentBatchId)
            .Select(group => new PaymentBatchSummary(
                group.Key,
                group.Count(),
                group.Sum(r => r.TotalAmount),
                group.First().Currency,
                group.All(r => r.PaymentStatus == PaymentStates.Paid)
                    ? PaymentStates.Paid
                    : group.Any(r => r.PaymentStatus == PaymentStates.Verification)
                        ? PaymentStates.Verification
                        : PaymentStates.Pending,
                group.First().Id))
            .ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostPayAllAsync()
    {
        var signedOut = SignedOut("/Register/MyRegistrations");
        if (signedOut is not null) return signedOut;

        var memberNo = HttpContext.Session.GetString(SessionKeys.VerifiedMemberNo) ?? "";
        var result = await service.CreateCombinedPaymentBatchAsync(
            CurrentEmail!, memberNo, PaymentBatchKinds.Registration);
        return RedirectToPage("BatchPayment", new { id = result.BatchId });
    }
}
