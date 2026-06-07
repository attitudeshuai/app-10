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
    private const int PendingScanIntervalSeconds = 30;
    private const int MaxPendingRetryCount = 3;

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

        var queueTask = ProcessQueueAsync(stoppingToken);
        var pendingTask = ProcessPendingNotificationsAsync(stoppingToken);

        await Task.WhenAll(queueTask, pendingTask);

        _logger.LogInformation("Notification Background Service is stopping.");
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
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
                _logger.LogError(ex, "Error occurred processing notification batch from queue.");
                await Task.Delay(DelayMilliseconds, stoppingToken);
            }
        }
    }

    private async Task ProcessPendingNotificationsAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(PendingScanIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ScanAndProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred scanning pending notifications.");
            }
        }
    }

    private async Task ScanAndProcessPendingAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingNotifications = await context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending && n.RetryCount < MaxPendingRetryCount)
            .OrderBy(n => n.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        if (pendingNotifications.Count == 0)
        {
            return;
        }

        _logger.LogInformation("扫描到 {Count} 条待处理通知，开始补偿处理", pendingNotifications.Count);

        var userIds = pendingNotifications.Select(n => n.UserId).Distinct().ToList();
        var validUserIds = await context.Users
            .Where(u => userIds.Contains(u.Id) && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(stoppingToken);

        var validUserSet = new HashSet<int>(validUserIds);
        var now = DateTime.UtcNow;
        var processedCount = 0;
        var failedCount = 0;

        foreach (var notification in pendingNotifications)
        {
            notification.RetryCount++;

            if (validUserSet.Contains(notification.UserId))
            {
                notification.Status = NotificationStatus.Processed;
                notification.ProcessedAt = now;
                processedCount++;
            }
            else
            {
                if (notification.RetryCount >= MaxPendingRetryCount)
                {
                    notification.Status = NotificationStatus.Failed;
                    failedCount++;
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);

        if (processedCount > 0)
        {
            _logger.LogInformation("待处理通知补偿处理完成，成功: {ProcessedCount}, 失败: {FailedCount}", processedCount, failedCount);
        }
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
        var now = DateTime.UtcNow;
        foreach (var notification in batch)
        {
            if (validUserSet.Contains(notification.UserId))
            {
                notification.Status = NotificationStatus.Processed;
                notification.ProcessedAt = now;
                validBatch.Add(notification);
            }
        }

        if (validBatch.Count == 0) return;

        context.Notifications.AddRange(validBatch);
        await context.SaveChangesAsync(stoppingToken);

        _logger.LogDebug("Processed {Count} notifications in batch, written to database.", validBatch.Count);
    }
}
