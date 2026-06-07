namespace DeviceMaintenanceSystem.Models;

public enum InspectionPlanStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum InspectionCycle
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2
}

public class InspectionPlan
{
    public int Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public InspectionCycle Cycle { get; set; }
    public DateTime PlannedDate { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public User? AssignedTechnician { get; set; }
    public string InspectionContent { get; set; } = string.Empty;
    public InspectionPlanStatus Status { get; set; } = InspectionPlanStatus.Pending;
    public DateTime? ActualInspectionDate { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InspectionRecord> InspectionRecords { get; set; } = new List<InspectionRecord>();
}
