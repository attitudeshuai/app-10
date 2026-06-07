namespace DeviceMaintenanceSystem.Models;

public enum NotificationType
{
    FaultAssigned = 0,
    FaultCompleted = 1,
    FaultCancelled = 2,
    MaintenanceReminder = 3,
    MaintenanceStarted = 4,
    MaintenanceCompleted = 5,
    DeviceStatusChanged = 6,
    InspectionTaskAssigned = 7,
    SystemNotice = 8
}

public enum NotificationPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

public enum RelatedEntityType
{
    FaultReport = 0,
    MaintenancePlan = 1,
    Device = 2,
    InspectionTask = 3,
    InspectionPlan = 4,
    System = 5
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public RelatedEntityType? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
