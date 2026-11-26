using Gcmac.Registration.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Admin;

public sealed class AdminModel(Services.RegistrationService service, FeeCatalog fees) : BasePageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    public IReadOnlyList<RegistrationRecord> Registrations { get; private set; } = [];
    public string CategoryName(DelegateCategory category) => fees.NameOf(category);

    private IActionResult? Denied() =>
        IsAdmin ? null : RedirectToPage("/Account/SignIn", new { type = "admin", returnUrl = "/Admin/Index" });

    public async Task<IActionResult> OnGetAsync()
    {
        var denied = Denied();
        if (denied is not null) return denied;
        ViewData["Title"] = "Registrations";
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMarkPaidAsync(string id)
    {
        var denied = Denied();
        if (denied is not null) return denied;

        var record = await service.FindAsync(id);
        if (record is not null) await service.MarkPaidAsync(record);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVerifyBatchAsync(string batchId)
    {
        var denied = Denied();
        if (denied is not null) return denied;

        await service.MarkBatchPaidAsync(batchId);
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var all = await service.GetAllAsync();
        var term = Search?.Trim();
        Registrations = string.IsNullOrWhiteSpace(term)
            ? all
            : all.Where(r =>
                r.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Email.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
