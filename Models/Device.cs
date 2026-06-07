namespace DeviceMaintenanceSystem.Models;

public enum DeviceStatus
{
    Running = 0,
    Standby = 1,
    Maintenance = 2,
    Fault = 3,
    Scrapped = 4
}

public class Device
{
    public int Id { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string Location { get; set; } = string.Empty;
    public DeviceStatus Status { get; set; } = DeviceStatus.Running;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MaintenancePlan> MaintenancePlans { get; set; } = new List<MaintenancePlan>();
    public ICollection<FaultReport> FaultReports { get; set; } = new List<FaultReport>();
    public ICollection<SparePart> SpareParts { get; set; } = new List<SparePart>();
    public ICollection<InspectionPlan> InspectionPlans { get; set; } = new List<InspectionPlan>();
    public ICollection<InspectionTask> InspectionTasks { get; set; } = new List<InspectionTask>();
    public ICollection<InspectionRecord> InspectionRecords { get; set; } = new List<InspectionRecord>();
}
