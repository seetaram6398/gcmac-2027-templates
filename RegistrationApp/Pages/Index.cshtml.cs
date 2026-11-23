using Gcmac.Registration.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Gcmac.Registration.Pages;

public sealed class IndexModel(FeeCatalog fees) : PageModel
{
    public IReadOnlyList<CategoryOption> Categories => fees.Categories;
    public IReadOnlyList<(CategoryOption Option, FeeBreakdown Fee)> Pricing =>
        fees.Categories.Select(c => (c, fees.Calculate(c.Category))).ToList();

    public void OnGet() => ViewData["Title"] = "Registration";
}
