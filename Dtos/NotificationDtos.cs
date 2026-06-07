using DeviceMaintenanceSystem.Models;

namespace DeviceMaintenanceSystem.Dtos;

public class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public RelatedEntityType? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationQueryDto : PagedQuery
{
    public bool? IsRead { get; set; }
    public NotificationType? Type { get; set; }
    public NotificationPriority? Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class NotificationStatisticsDto
{
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int TodayCount { get; set; }
}

public class CreateNotificationDto
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;
    public RelatedEntityType? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}

public class BatchCreateNotificationDto
{
    public List<int> UserIds { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;
    public RelatedEntityType? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}

public class MarkAsReadDto
{
    public int Id { get; set; }
}
