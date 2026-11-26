using Gcmac.Registration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Gcmac.Registration.Pages.Register;

public sealed class QrModel(RegistrationService service) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var record = await service.FindAsync(id);
        if (record is null) return NotFound();

        return File(QrCodes.PngBytes(DelegatePasses.QrText(record)), "image/png");
    }
}
