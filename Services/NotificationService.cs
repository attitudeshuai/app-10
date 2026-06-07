using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeviceMaintenanceSystem.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationQueue _queue;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext context,
        IMapper mapper,
        INotificationQueue queue,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _mapper = mapper;
        _queue = queue;
        _logger = logger;
    }

    public async Task<PagedResult<NotificationDto>> GetPagedAsync(int userId, NotificationQueryDto query)
    {
        var queryable = _context.Notifications
            .Include(n => n.User)
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (query.IsRead.HasValue)
            queryable = queryable.Where(n => n.IsRead == query.IsRead.Value);

        if (query.Type.HasValue)
            queryable = queryable.Where(n => n.Type == query.Type.Value);

        if (query.Priority.HasValue)
            queryable = queryable.Where(n => n.Priority == query.Priority.Value);

        if (query.StartDate.HasValue)
            queryable = queryable.Where(n => n.CreatedAt >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            queryable = queryable.Where(n => n.CreatedAt <= query.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(n =>
                n.Title.ToLower().Contains(keyword) ||
                n.Content.ToLower().Contains(keyword));
        }

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "createdat";
        queryable = sortBy switch
        {
            "title" => query.SortDesc ? queryable.OrderByDescending(n => n.Title) : queryable.OrderBy(n => n.Title),
            "type" => query.SortDesc ? queryable.OrderByDescending(n => n.Type) : queryable.OrderBy(n => n.Type),
            "priority" => query.SortDesc ? queryable.OrderByDescending(n => n.Priority) : queryable.OrderBy(n => n.Priority),
            "isread" => query.SortDesc ? queryable.OrderByDescending(n => n.IsRead) : queryable.OrderBy(n => n.IsRead),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(n => n.CreatedAt) : queryable.OrderBy(n => n.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(n => n.Id) : queryable.OrderBy(n => n.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<NotificationDto>>(items);
        return new PagedResult<NotificationDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<NotificationDto?> GetByIdAsync(int id, int userId)
    {
        var notification = await _context.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        return notification == null ? null : _mapper.Map<NotificationDto>(notification);
    }

    public async Task<NotificationStatisticsDto> GetStatisticsAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        var total = await _context.Notifications.CountAsync(n => n.UserId == userId);
        var unread = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        var today = await _context.Notifications.CountAsync(n => n.UserId == userId && n.CreatedAt >= todayStart);

        return new NotificationStatisticsDto
        {
            TotalCount = total,
            UnreadCount = unread,
            TodayCount = today
        };
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null || !user.IsActive)
            throw new KeyNotFoundException("用户不存在或已禁用");

        var notification = _mapper.Map<Notification>(dto);
        notification.CreatedAt = DateTime.UtcNow;

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        try
        {
            if (!_queue.Enqueue(notification))
            {
                _logger.LogWarning("通知入队失败（队列已满），通知ID: {NotificationId}, 用户ID: {UserId}", notification.Id, notification.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通知入队异常，通知ID: {NotificationId}, 用户ID: {UserId}", notification.Id, notification.UserId);
        }

        return _mapper.Map<NotificationDto>(notification);
    }

    public async Task BatchCreateAsync(BatchCreateNotificationDto dto)
    {
        if (dto.UserIds == null || dto.UserIds.Count == 0)
            return;

        var validUserIds = await _context.Users
            .Where(u => dto.UserIds.Contains(u.Id) && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        if (validUserIds.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var notifications = validUserIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = dto.Title,
            Content = dto.Content,
            Type = dto.Type,
            Priority = dto.Priority,
            RelatedEntityType = dto.RelatedEntityType,
            RelatedEntityId = dto.RelatedEntityId,
            CreatedAt = now,
            IsRead = false
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        try
        {
            var successCount = _queue.EnqueueRange(notifications);
            if (successCount < notifications.Count)
            {
                _logger.LogWarning("批量通知入队部分失败（队列可能已满），成功: {SuccessCount}, 总计: {TotalCount}", successCount, notifications.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量通知入队异常，数量: {Count}", notifications.Count);
        }
    }

    public async Task EnqueueAsync(CreateNotificationDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("通知目标用户不存在或已禁用，用户ID: {UserId}", dto.UserId);
                return;
            }

            var notification = new Notification
            {
                UserId = dto.UserId,
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                Priority = dto.Priority,
                RelatedEntityType = dto.RelatedEntityType,
                RelatedEntityId = dto.RelatedEntityId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            if (_queue.Enqueue(notification))
            {
                return;
            }

            _logger.LogWarning("通知入队失败，准备重试，用户ID: {UserId}, 标题: {Title}", dto.UserId, dto.Title);
            await Task.Delay(50);

            if (_queue.Enqueue(notification))
            {
                _logger.LogInformation("通知入队重试成功，用户ID: {UserId}, 标题: {Title}", dto.UserId, dto.Title);
                return;
            }

            _logger.LogWarning("通知入队重试失败，降级为直接写入数据库，用户ID: {UserId}, 标题: {Title}", dto.UserId, dto.Title);
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            _logger.LogInformation("通知降级写库成功，通知ID: {NotificationId}", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通知入队异常，用户ID: {UserId}, 标题: {Title}", dto.UserId, dto.Title);
        }
    }

    public async Task BatchEnqueueAsync(BatchCreateNotificationDto dto)
    {
        try
        {
            if (dto.UserIds == null || dto.UserIds.Count == 0)
                return;

            var validUserIds = await _context.Users
                .Where(u => dto.UserIds.Contains(u.Id) && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            if (validUserIds.Count == 0)
            {
                _logger.LogWarning("批量通知没有有效目标用户");
                return;
            }

            var now = DateTime.UtcNow;
            var notifications = validUserIds.Select(userId => new Notification
            {
                UserId = userId,
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                Priority = dto.Priority,
                RelatedEntityType = dto.RelatedEntityType,
                RelatedEntityId = dto.RelatedEntityId,
                CreatedAt = now,
                IsRead = false
            }).ToList();

            var failedNotifications = new List<Notification>();

            foreach (var notification in notifications)
            {
                if (!_queue.Enqueue(notification))
                {
                    failedNotifications.Add(notification);
                }
            }

            if (failedNotifications.Count == 0)
            {
                return;
            }

            _logger.LogWarning("批量通知入队部分失败，首次失败数量: {FailedCount}, 总计: {TotalCount}，准备重试", failedNotifications.Count, notifications.Count);
            await Task.Delay(50);

            var retryFailed = new List<Notification>();
            foreach (var notification in failedNotifications)
            {
                if (!_queue.Enqueue(notification))
                {
                    retryFailed.Add(notification);
                }
            }

            if (retryFailed.Count == 0)
            {
                _logger.LogInformation("批量通知重试后全部入队成功");
                return;
            }

            _logger.LogWarning("批量通知重试后仍有 {FailedCount} 条失败，降级为直接写入数据库", retryFailed.Count);
            _context.Notifications.AddRange(retryFailed);
            await _context.SaveChangesAsync();
            _logger.LogInformation("批量通知降级写库完成，数量: {Count}", retryFailed.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量通知入队异常，数量: {Count}, 标题: {Title}", dto.UserIds?.Count ?? 0, dto.Title);
        }
    }

    public async Task<NotificationDto?> MarkAsReadAsync(int id, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null) return null;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return _mapper.Map<NotificationDto>(notification);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var count = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        _logger.LogInformation("批量标记已读，用户ID: {UserId}, 数量: {Count}", userId, count);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null) return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteReadAsync(int userId)
    {
        var count = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ExecuteDeleteAsync();

        _logger.LogInformation("清理已读通知，用户ID: {UserId}, 数量: {Count}", userId, count);
        return count;
    }
}
