namespace DeviceMaintenanceSystem.Models;

public enum InspectionPlanStatus
{
    Active = 0,
    Paused = 1,
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
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public User? AssignedTechnician { get; set; }
    public string InspectionContent { get; set; } = string.Empty;
    public InspectionPlanStatus Status { get; set; } = InspectionPlanStatus.Active;
    public int GeneratedTaskCount { get; set; } = 0;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InspectionTask> InspectionTasks { get; set; } = new List<InspectionTask>();
}
