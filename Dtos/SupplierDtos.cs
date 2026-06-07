using DeviceMaintenanceSystem.Models;

namespace DeviceMaintenanceSystem.Dtos;

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public CooperationStatus Status { get; set; }
    public string? Description { get; set; }
    public int DeviceCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SupplierDetailDto : SupplierDto
{
    public List<SupplierDeviceDto> Devices { get; set; } = new List<SupplierDeviceDto>();
}

public class SupplierDeviceDto
{
    public int Id { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DeviceStatus Status { get; set; }
}

public class CreateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public CooperationStatus Status { get; set; } = CooperationStatus.Active;
    public string? Description { get; set; }
}

public class UpdateSupplierDto
{
    public string? Name { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public CooperationStatus? Status { get; set; }
    public string? Description { get; set; }
}

public class SupplierQueryDto : PagedQuery
{
    public CooperationStatus? Status { get; set; }
}

public class SupplierStatisticsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int SuspendedCount { get; set; }
    public int TerminatedCount { get; set; }
    public List<SupplierDeviceStatDto>? DeviceStats { get; set; }
}

public class SupplierDeviceStatDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int TotalDevices { get; set; }
    public int RunningCount { get; set; }
    public int StandbyCount { get; set; }
    public int MaintenanceCount { get; set; }
    public int FaultCount { get; set; }
    public int ScrappedCount { get; set; }
    public int FaultReportCount { get; set; }
    public decimal FaultRate { get; set; }
}
