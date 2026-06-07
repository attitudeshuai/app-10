namespace DeviceMaintenanceSystem.Models;

public enum RatingWorkType
{
    Maintenance = 0,
    Repair = 1
}

public class SupplierRating
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public int RaterId { get; set; }
    public User? Rater { get; set; }
    public RatingWorkType WorkType { get; set; }
    public int? MaintenancePlanId { get; set; }
    public MaintenancePlan? MaintenancePlan { get; set; }
    public int? FaultReportId { get; set; }
    public FaultReport? FaultReport { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
