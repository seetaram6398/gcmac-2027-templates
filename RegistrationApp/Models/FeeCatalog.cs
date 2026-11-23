using System.Globalization;

namespace Gcmac.Registration.Models;

public enum DelegateCategory
{
    CorporateOthers,
    CmaPractice,
    Spouse,
    Student,
    Foreign
}

public sealed record CategoryOption(DelegateCategory Category, string Name, string Description);

public sealed record FeeBreakdown(
    string Currency,
    decimal BaseFee,
    decimal GstRate,
    decimal GstAmount,
    decimal TotalAmount,
    bool IsEarlyBird)
{
    public string Format(decimal amount) =>
        $"{Symbol}{amount.ToString("N2", CultureInfo.InvariantCulture)}";

    public string Symbol => Currency == "USD" ? "$" : "\u20B9";
}

public sealed class FeeCatalog
{
    public static readonly DateTimeOffset EarlyBirdCutoff =
        new(2026, 12, 15, 23, 59, 59, TimeSpan.FromMinutes(330));

    public IReadOnlyList<CategoryOption> Categories { get; } =
    [
        new(DelegateCategory.CorporateOthers, "Corporate Delegates & Others",
            "Organisation and signatory details for corporate and other paid delegates."),
        new(DelegateCategory.CmaPractice, "CMA in Practice / Self Sponsored CMAs",
            "For practising CMAs and self-sponsored members of the Institute."),
        new(DelegateCategory.Spouse, "Accompanying Spouse",
            "For a spouse accompanying a registered delegate."),
        new(DelegateCategory.Student, "Students",
            "For registered students of The Institute of Cost Accountants of India."),
        new(DelegateCategory.Foreign, "Foreign Delegates",
            "For overseas participants attending the convention.")
    ];

    public string NameOf(DelegateCategory category) =>
        Categories.First(c => c.Category == category).Name;

    public bool IsEarlyBird(DateTimeOffset? now = null) => (now ?? DateTimeOffset.UtcNow) <= EarlyBirdCutoff;

    public FeeBreakdown Calculate(DelegateCategory category, DateTimeOffset? now = null)
    {
        var early = IsEarlyBird(now);
        var currency = category == DelegateCategory.Foreign ? "USD" : "INR";
        var baseFee = category switch
        {
            DelegateCategory.CorporateOthers => early ? 8000m : 10000m,
            DelegateCategory.CmaPractice => early ? 4000m : 5000m,
            DelegateCategory.Spouse => 3000m,
            DelegateCategory.Student => 2000m,
            DelegateCategory.Foreign => 200m,
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };

        var gstRate = currency == "USD" ? 0m : 0.18m;
        var gstAmount = Math.Round(baseFee * gstRate, 2, MidpointRounding.AwayFromZero);
        return new FeeBreakdown(currency, baseFee, gstRate, gstAmount, baseFee + gstAmount, early);
    }
}
