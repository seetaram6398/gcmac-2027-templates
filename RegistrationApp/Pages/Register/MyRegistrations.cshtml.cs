using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class MyRegistrationsModel(Services.RegistrationService service, Models.FeeCatalog fees) : BasePageModel
{
    public IReadOnlyList<Models.RegistrationRecord> Registrations { get; private set; } = [];
    public string AccountEmail => CurrentEmail ?? "";
    public string CategoryName(Models.DelegateCategory category) => fees.NameOf(category);

    public async Task<IActionResult> OnGetAsync()
    {
        var signedOut = SignedOut("/Register/MyRegistrations");
        if (signedOut is not null) return signedOut;
        ViewData["Title"] = "My registrations";
        Registrations = await service.GetForEmailAsync(CurrentEmail!);
        return Page();
    }
}
