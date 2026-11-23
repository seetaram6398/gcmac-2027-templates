using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class ReceiptModel(Services.RegistrationService service, Models.FeeCatalog fees) : BasePageModel
{
    public Models.RegistrationRecord Registration { get; private set; } = new();
    public string CategoryName => fees.NameOf(Registration.Category);

    public async Task<IActionResult> OnGetAsync(string id)
    {
        ViewData["Title"] = "Receipt";

        var record = await service.FindAsync(id);
        if (record is null || !CanView(record))
            return IsSignedIn
                ? RedirectToPage("MyRegistrations")
                : RedirectToPage("/Account/Login", new { returnUrl = $"/Register/Receipt?id={id}" });

        Registration = record;
        return Page();
    }
}
