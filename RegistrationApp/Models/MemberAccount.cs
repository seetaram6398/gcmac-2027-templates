namespace Gcmac.Registration.Models;

public sealed class MemberAccount
{
    public string MemberNo { get; set; } = "";
    public string Name { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Designation { get; set; }
    public string? Organisation { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PinCode { get; set; }
    public bool EmailVerified { get; set; }
    public bool MobileVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
