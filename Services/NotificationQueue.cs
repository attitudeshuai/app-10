using System.Threading.Channels;
using DeviceMaintenanceSystem.Models;

namespace DeviceMaintenanceSystem.Services;

public class NotificationQueue : INotificationQueue
{
    private readonly Channel<Notification> _queue;
    private const int DefaultCapacity = 10000;

    public NotificationQueue(int capacity = DefaultCapacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        };
        _queue = Channel.CreateBounded<Notification>(options);
    }

    public bool Enqueue(Notification notification)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        return _queue.Writer.TryWrite(notification);
    }

    public int EnqueueRange(IEnumerable<Notification> notifications)
    {
        if (notifications == null) throw new ArgumentNullException(nameof(notifications));
        var successCount = 0;
        foreach (var notification in notifications)
        {
            if (_queue.Writer.TryWrite(notification))
            {
                successCount++;
            }
        }
        return successCount;
    }

    public async Task<List<Notification>> DequeueBatchAsync(int batchSize, CancellationToken stoppingToken)
    {
        var batch = new List<Notification>(batchSize);
        var reader = _queue.Reader;

        if (batch.Count == 0)
        {
            var firstItem = await reader.ReadAsync(stoppingToken);
            batch.Add(firstItem);
        }

        while (batch.Count < batchSize && reader.TryRead(out var item))
        {
            batch.Add(item);
        }

        return batch;
    }

    public int GetQueueCount()
    {
        return _queue.Reader.Count;
    }
}
