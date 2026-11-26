using Gcmac.Registration.Models;

namespace Gcmac.Registration.Services;

public static class DelegatePasses
{
    public static string QrText(RegistrationRecord record) => string.Join(Environment.NewLine,
        "GCMAC 2027",
        "Event entry pass",
        $"Name: {record.FullName}",
        $"Registration no.: {record.Id}",
        $"Membership ID: {record.MembershipId}",
        $"Category: {record.Category}",
        $"Registered on: {record.CreatedAt.ToLocalTime():dd-MM-yyyy}");

    public static string PassLink(RegistrationRecord record) =>
        $"/Register/Pass?id={Uri.EscapeDataString(record.Id)}";

    public static string ImageLink(RegistrationRecord record) =>
        $"/Register/Qr?id={Uri.EscapeDataString(record.Id)}";

    public static string Absolute(string? baseUrl, string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var existing)) return existing.ToString();
        if (string.IsNullOrWhiteSpace(baseUrl)) return path;
        var root = baseUrl.TrimEnd('/') + "/";
        return Uri.TryCreate(new Uri(root), path.TrimStart('/'), out var absolute)
            ? absolute.ToString()
            : path;
    }
}
