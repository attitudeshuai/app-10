namespace DeviceMaintenanceSystem.Models;

public enum MaintenanceScheduleStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2,
    Cancelled = 3
}

public class MaintenanceSchedule
{
    public int Id { get; set; }
    public string ScheduleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public MaintenanceCycle Cycle { get; set; }
    public int? CustomIntervalDays { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public User? ResponsibleTechnician { get; set; }
    public string Content { get; set; } = string.Empty;
    public MaintenanceScheduleStatus Status { get; set; } = MaintenanceScheduleStatus.Active;
    public int GeneratedPlanCount { get; set; } = 0;
    public DateTime? LastGeneratedDate { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MaintenancePlan> MaintenancePlans { get; set; } = new List<MaintenancePlan>();
}
