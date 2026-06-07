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
    SystemNotice = 8,
    ContractExpiringSoon = 9,
    ContractExpired = 10,
    InspectionTaskOverdue = 11,
    BorrowRequestSubmitted = 12,
    BorrowRequestApproved = 13,
    BorrowRequestRejected = 14
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
    System = 5,
    MaintenanceContract = 6,
    DeviceBorrowRecord = 7
}

public enum NotificationStatus
{
    Pending = 0,
    Processed = 1,
    Failed = 2
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
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public RelatedEntityType? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
