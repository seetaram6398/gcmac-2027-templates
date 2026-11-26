namespace Gcmac.Registration.Models;

public enum NotificationChannel
{
    Email,
    Sms
}

public static class NotificationStatus
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

public sealed class NotificationMessage
{
    public long Id { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = "";
    public string TemplateKey { get; set; } = "";
    public string Body { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public string? TemplateId { get; set; }
    public string Status { get; set; } = NotificationStatus.Pending;
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? LastError { get; set; }
}
