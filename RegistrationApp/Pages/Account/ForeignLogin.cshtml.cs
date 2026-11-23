using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class ForeignLoginModel(IConfiguration configuration) : BasePageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string SecurityCode { get; set; } = "";

    public void OnGet() => ViewData["Title"] = "Foreign delegate login";

    public IActionResult OnPost()
    {
        var secret = configuration["MemberLogin:DemoSecret"] ?? "123456";
        if (Password != secret || SecurityCode != secret)
        {
            ModelState.AddModelError("", "Invalid password or security code.");
            return Page();
        }
        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError("", "Email is required.");
            return Page();
        }

        HttpContext.Session.SetString(SessionKeys.Email, Email.Trim());
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        return Redirect("/Register/Form?category=Foreign");
    }
}
