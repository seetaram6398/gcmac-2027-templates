using Gcmac.Registration.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Register;

public sealed class TrackModel(Services.RegistrationService service) : BasePageModel
{
    [BindProperty] public string Id { get; set; } = "";
    [BindProperty] public string Email { get; set; } = "";
    public RegistrationRecord? Result { get; private set; }
    public bool Searched { get; private set; }
    public string ErrorMessage { get; private set; } = "";

    public void OnGet() => ViewData["Title"] = "Track registration";

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Track registration";
        Searched = true;
        Id = Id.Trim();
        Email = Email.Trim();

        var record = await service.FindAsync(Id);
        if (record is null || !string.Equals(record.Email, Email, StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "No registration found with that number and email address.";
            return Page();
        }

        Result = record;
        HttpContext.Session.SetString(SessionKeys.TrackAccess, record.Id);
        return Page();
    }
}
