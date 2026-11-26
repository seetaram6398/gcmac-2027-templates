namespace Gcmac.Registration.Options;

public sealed class MemberApiOptions
{
    public const string SectionName = "MemberApi";

    public string BaseUrl { get; set; } = "https://eicmai.in/member_gcmac_api/";
    public string ApiKey { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 10;
    public bool RequireVerification { get; set; } = true;
}

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    public string Provider { get; set; } = "PNB";
    public string Currency { get; set; } = "INR";
    public string PublicBaseUrl { get; set; } = "";
    public PnbPaymentOptions Pnb { get; set; } = new();
}

public sealed class PnbPaymentOptions
{
    public string RequestUrl { get; set; } = "";
    public string VerifyUrl { get; set; } = "";
    public string MerchantCode { get; set; } = "";
    public string Key { get; set; } = "";
    public string Iv { get; set; } = "";
    public string BankCode { get; set; } = "470";
}

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public EmailOptions Email { get; set; } = new();
    public SmsOptions Sms { get; set; } = new();
    public string AdminEmail { get; set; } = "";
    public string AdminMobile { get; set; } = "";
    public int MaxDelegatesPerMember { get; set; } = 5;
    public int MaxAdditionalDelegatesPerMember { get; set; } = 5;
    public string PublicBaseUrl { get; set; } = "";
    public NotificationForwardingOptions Forwarding { get; set; } = new();
}

public sealed class NotificationForwardingOptions
{
    public bool Enabled { get; set; }
    public string Email { get; set; } = "";
    public string Templates { get; set; } = "";
}

public sealed class EmailOptions
{
    public string ApiUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string FromEmail { get; set; } = "gcmac2027@icmai.in";
    public string FromName { get; set; } = "GCMAC 2027";
}

public sealed class SmsOptions
{
    public string ApiUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string SenderId { get; set; } = "";
    public Dictionary<string, string> TemplateIds { get; set; } = new();
}
