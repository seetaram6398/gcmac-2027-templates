using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class CorporateLoginModel(IConfiguration configuration) : BasePageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string Category { get; set; } = "";
    [BindProperty] public string SecurityCode { get; set; } = "";

    public void OnGet() => ViewData["Title"] = "Corporate login";

    public IActionResult OnPost()
    {
        var secret = configuration["MemberLogin:DemoSecret"] ?? "123456";
        if (Password != secret || SecurityCode != secret)
        {
            ModelState.AddModelError("", "Invalid password or security code.");
            return Page();
        }
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Category))
        {
            ModelState.AddModelError("", "Email and category are required.");
            return Page();
        }

        HttpContext.Session.SetString(SessionKeys.Email, Email.Trim());
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        return Redirect($"/Register/Form?category=CorporateOthers&occupancy={Uri.EscapeDataString(Category)}");
    }
}
