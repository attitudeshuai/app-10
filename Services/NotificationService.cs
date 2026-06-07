using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationQueue _queue;

    public NotificationService(AppDbContext context, IMapper mapper, INotificationQueue queue)
    {
        _context = context;
        _mapper = mapper;
        _queue = queue;
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
    }

    public Task EnqueueAsync(CreateNotificationDto dto)
    {
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

        _queue.Enqueue(notification);
        return Task.CompletedTask;
    }

    public Task BatchEnqueueAsync(BatchCreateNotificationDto dto)
    {
        if (dto.UserIds == null || dto.UserIds.Count == 0)
            return Task.CompletedTask;

        var now = DateTime.UtcNow;
        var notifications = dto.UserIds.Select(userId => new Notification
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

        _queue.EnqueueRange(notifications);
        return Task.CompletedTask;
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
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync();
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
        var readNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ToListAsync();

        if (readNotifications.Count == 0) return 0;

        _context.Notifications.RemoveRange(readNotifications);
        await _context.SaveChangesAsync();
        return readNotifications.Count;
    }
}
