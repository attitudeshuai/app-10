namespace DeviceMaintenanceSystem.Models;

public enum CooperationStatus
{
    Active = 0,
    Suspended = 1,
    Terminated = 2
}

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public CooperationStatus Status { get; set; } = CooperationStatus.Active;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<SupplierRating> Ratings { get; set; } = new List<SupplierRating>();
}
