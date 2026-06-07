namespace DeviceMaintenanceSystem.Models;

public class SparePartConsumption
{
    public int Id { get; set; }
    public int SparePartId { get; set; }
    public SparePart? SparePart { get; set; }
    public int FaultReportId { get; set; }
    public FaultReport? FaultReport { get; set; }
    public int Quantity { get; set; }
    public DateTime ConsumedAt { get; set; } = DateTime.UtcNow;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
