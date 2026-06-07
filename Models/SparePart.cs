namespace DeviceMaintenanceSystem.Models;

public class SparePart
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int MinStockWarning { get; set; }
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SparePartConsumption> Consumptions { get; set; } = new List<SparePartConsumption>();
}
