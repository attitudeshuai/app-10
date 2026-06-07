namespace DeviceMaintenanceSystem.Models;

public enum InspectionTaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public class InspectionTask
{
    public int Id { get; set; }
    public string TaskCode { get; set; } = string.Empty;
    public int InspectionPlanId { get; set; }
    public InspectionPlan? InspectionPlan { get; set; }
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public User? AssignedTechnician { get; set; }
    public string InspectionContent { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public InspectionTaskStatus Status { get; set; } = InspectionTaskStatus.Pending;
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InspectionRecord> InspectionRecords { get; set; } = new List<InspectionRecord>();
}
