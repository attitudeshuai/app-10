using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeviceMaintenanceSystem.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private const int BatchSize = 100;
    private const int DelayMilliseconds = 500;

    public NotificationBackgroundService(
        INotificationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await _queue.DequeueBatchAsync(BatchSize, stoppingToken);
                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(batch, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing notification batch.");
                await Task.Delay(DelayMilliseconds, stoppingToken);
            }
        }

        _logger.LogInformation("Notification Background Service is stopping.");
    }

    private async Task ProcessBatchAsync(List<Notification> batch, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var validBatch = new List<Notification>();
        var userIds = batch.Select(n => n.UserId).Distinct().ToList();

        var validUserIds = await context.Users
            .Where(u => userIds.Contains(u.Id) && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(stoppingToken);

        var validUserSet = new HashSet<int>(validUserIds);
        foreach (var notification in batch)
        {
            if (validUserSet.Contains(notification.UserId))
            {
                validBatch.Add(notification);
            }
        }

        if (validBatch.Count == 0) return;

        context.Notifications.AddRange(validBatch);
        await context.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Processed {Count} notifications in batch.", validBatch.Count);
    }
}
