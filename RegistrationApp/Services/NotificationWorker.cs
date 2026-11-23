using Gcmac.Registration.Data;
using Gcmac.Registration.Models;

namespace Gcmac.Registration.Services;

public sealed class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification outbox processing failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<RegistrationStore>();
        var senders = scope.ServiceProvider.GetServices<INotificationSender>().ToList();
        var pending = await store.GetPendingNotificationsAsync(DateTimeOffset.UtcNow, BatchSize, cancellationToken);

        foreach (var message in pending)
        {
            var sender = senders.FirstOrDefault(s => s.Channel == message.Channel);
            if (sender is null)
            {
                MarkFailed(message, "No sender configured for this channel.");
            }
            else
            {
                try
                {
                    await sender.SendAsync(message, cancellationToken);
                    message.Status = NotificationStatus.Sent;
                    message.SentAt = DateTimeOffset.UtcNow;
                    message.LastError = null;
                }
                catch (Exception ex)
                {
                    message.Attempts++;
                    message.LastError = Truncate(ex.Message);
                    if (message.Attempts >= MaxAttempts)
                    {
                        message.Status = NotificationStatus.Failed;
                    }
                    else
                    {
                        message.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(2 * message.Attempts);
                    }
                }
            }

            await store.SaveNotificationAsync(message, cancellationToken);
        }
    }

    private static void MarkFailed(NotificationMessage message, string error)
    {
        message.Attempts++;
        message.Status = NotificationStatus.Failed;
        message.LastError = Truncate(error);
    }

    private static string Truncate(string value) =>
        value.Length <= 400 ? value : value[..400];
}
