namespace DeviceMaintenanceSystem.Models;

public enum InspectionResult
{
    Normal = 0,
    Abnormal = 1,
    NeedsAttention = 2
}

public class InspectionRecord
{
    public int Id { get; set; }
    public string RecordCode { get; set; } = string.Empty;
    public int? InspectionTaskId { get; set; }
    public InspectionTask? InspectionTask { get; set; }
    public int? InspectionPlanId { get; set; }
    public InspectionPlan? InspectionPlan { get; set; }
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public int InspectorId { get; set; }
    public User? Inspector { get; set; }
    public DeviceStatus DeviceStatus { get; set; }
    public InspectionResult Result { get; set; }
    public string? AbnormalDescription { get; set; }
    public string? Remark { get; set; }
    public DateTime InspectionTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InspectionPhoto> Photos { get; set; } = new List<InspectionPhoto>();
}
