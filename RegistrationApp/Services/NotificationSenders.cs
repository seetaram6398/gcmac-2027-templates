using System.Text;
using System.Text.Json;
using Gcmac.Registration.Models;
using Gcmac.Registration.Options;
using Microsoft.Extensions.Options;

namespace Gcmac.Registration.Services;

public interface INotificationSender
{
    NotificationChannel Channel { get; }
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

public sealed class HttpEmailSender(
    IHttpClientFactory httpClientFactory,
    IOptions<NotificationOptions> options) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value.Email;
        if (string.IsNullOrWhiteSpace(settings.ApiUrl))
            throw new InvalidOperationException("Email gateway is not configured.");

        var payload = new
        {
            apiKey = settings.ApiKey,
            from = new { email = settings.FromEmail, name = settings.FromName },
            to = new[] { new { email = message.Recipient } },
            subject = $"GCMAC 2027 - {message.TemplateKey}",
            text = message.Body,
            html = message.HtmlBody
        };

        await PostJsonAsync(settings.ApiUrl, payload, cancellationToken);
    }

    private async Task PostJsonAsync(string url, object payload, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        using var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class HttpSmsSender(
    IHttpClientFactory httpClientFactory,
    IOptions<NotificationOptions> options) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value.Sms;
        if (string.IsNullOrWhiteSpace(settings.ApiUrl))
            throw new InvalidOperationException("SMS gateway is not configured.");

        var payload = new
        {
            apiKey = settings.ApiKey,
            sender = settings.SenderId,
            templateId = message.TemplateId,
            to = new[] { message.Recipient },
            text = message.Body
        };

        var client = httpClientFactory.CreateClient();
        using var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(settings.ApiUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
