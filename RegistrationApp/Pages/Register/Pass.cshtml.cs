using Gcmac.Registration.Models;
using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class PassModel(RegistrationService service, FeeCatalog fees) : BasePageModel
{
    public Models.RegistrationRecord Registration { get; private set; } = new();
    public string CategoryName => fees.NameOf(Registration.Category);
    public string QrDataUri { get; private set; } = "";
    public string QrText { get; private set; } = "";
    public bool Paid => Registration.PaymentStatus == PaymentStates.Paid;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        ViewData["Title"] = "Event pass";

        var record = await service.FindAsync(id);
        if (record is null) return RedirectToPage("MyRegistrations");

        Registration = record;
        QrText = DelegatePasses.QrText(record);
        QrDataUri = QrCodes.DataUri(QrText);
        return Page();
    }
}
