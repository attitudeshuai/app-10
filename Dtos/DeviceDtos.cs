using DeviceMaintenanceSystem.Models;

namespace DeviceMaintenanceSystem.Dtos;

public class DeviceDto
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
    public DeviceStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDeviceDto
{
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
}

public class UpdateDeviceDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string Location { get; set; } = string.Empty;
    public DeviceStatus? Status { get; set; }
    public string? Description { get; set; }
}

public class DeviceQueryDto : PagedQuery
{
    public DeviceStatus? Status { get; set; }
    public string? Category { get; set; }
}

public class DeviceStatisticsDto
{
    public int TotalCount { get; set; }
    public int RunningCount { get; set; }
    public int StandbyCount { get; set; }
    public int MaintenanceCount { get; set; }
    public int FaultCount { get; set; }
    public int ScrappedCount { get; set; }
    public List<CategoryStatDto>? ByCategory { get; set; }
}

public class CategoryStatDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}
