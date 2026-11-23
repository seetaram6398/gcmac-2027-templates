using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class LogoutModel : BasePageModel
{
    public IActionResult OnGet()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Index");
    }
}
