namespace Gcmac.Registration.Models;

public sealed class Account
{
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
