namespace DeviceMaintenanceSystem.Models;

public enum MaintenancePlanStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum MaintenanceCycle
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4,
    Custom = 5
}

public class MaintenancePlan
{
    public int Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public MaintenanceCycle Cycle { get; set; }
    public DateTime PlannedDate { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public User? ResponsibleTechnician { get; set; }
    public string Content { get; set; } = string.Empty;
    public MaintenancePlanStatus Status { get; set; } = MaintenancePlanStatus.Pending;
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string? Result { get; set; }
    public string? Remark { get; set; }
    public int? MaintenanceScheduleId { get; set; }
    public MaintenanceSchedule? MaintenanceSchedule { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
