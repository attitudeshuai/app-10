namespace DeviceMaintenanceSystem.Models;

public enum ContractStatus
{
    Active = 0,
    ExpiringSoon = 1,
    Expired = 2,
    Cancelled = 3
}

public class MaintenanceContract
{
    public int Id { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Amount { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool ReminderSent { get; set; } = false;
    public DateTime? ReminderSentAt { get; set; }
}
