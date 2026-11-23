using Microsoft.AspNetCore.Mvc;

namespace Gcmac.Registration.Pages.Account;

public sealed class StudentLoginModel(IConfiguration configuration) : BasePageModel
{
    [BindProperty] public string RegistrationNo { get; set; } = "";
    [BindProperty] public string DateOfBirth { get; set; } = "";
    [BindProperty] public string SecurityCode { get; set; } = "";

    public void OnGet() => ViewData["Title"] = "Student login";

    public IActionResult OnPost()
    {
        var secret = configuration["MemberLogin:DemoSecret"] ?? "123456";
        if (SecurityCode != secret)
        {
            ModelState.AddModelError("", "Invalid security code.");
            return Page();
        }
        if (string.IsNullOrWhiteSpace(RegistrationNo) || string.IsNullOrWhiteSpace(DateOfBirth))
        {
            ModelState.AddModelError("", "Registration number and date of birth are required.");
            return Page();
        }

        var regNo = RegistrationNo.Trim();
        HttpContext.Session.SetString(SessionKeys.Email, $"{regNo}@students.gcmac.local");
        HttpContext.Session.Remove(SessionKeys.IsAdmin);
        return Redirect($"/Register/Form?category=Student&regNo={Uri.EscapeDataString(regNo)}&dob={Uri.EscapeDataString(DateOfBirth.Trim())}");
    }
}
