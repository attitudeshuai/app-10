namespace DeviceMaintenanceSystem.Models;

public enum FaultStatus
{
    Pending = 0,
    Assigned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum FaultPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

public class FaultReport
{
    public int Id { get; set; }
    public string ReportCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public int ReporterId { get; set; }
    public User? Reporter { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public User? AssignedTechnician { get; set; }
    public FaultPriority Priority { get; set; } = FaultPriority.Medium;
    public FaultStatus Status { get; set; } = FaultStatus.Pending;
    public string Description { get; set; } = string.Empty;
    public string? FaultLocation { get; set; }
    public DateTime ReportTime { get; set; } = DateTime.UtcNow;
    public DateTime? AssignTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string? Solution { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SparePartConsumption> SparePartConsumptions { get; set; } = new List<SparePartConsumption>();
}
