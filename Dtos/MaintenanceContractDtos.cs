using DeviceMaintenanceSystem.Models;

namespace DeviceMaintenanceSystem.Dtos;

public class MaintenanceContractDto
{
    public int Id { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCode { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Amount { get; set; }
    public ContractStatus Status { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MaintenanceContractDetailDto : MaintenanceContractDto
{
    public string ServiceDescription { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class CreateMaintenanceContractDto
{
    public string ContractCode { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Amount { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateMaintenanceContractDto
{
    public string ContractName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Amount { get; set; }
    public string? ServiceDescription { get; set; }
    public int? SupplierId { get; set; }
    public string? Remarks { get; set; }
}

public class MaintenanceContractQueryDto : PagedQuery
{
    public ContractStatus? Status { get; set; }
    public int? DeviceId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public DateTime? EndDateFrom { get; set; }
    public DateTime? EndDateTo { get; set; }
}

public class MaintenanceContractStatisticsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ExpiredCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ContractDeviceStatDto>? ExpiredByDevice { get; set; }
    public List<ContractExpiryStatDto>? ExpiryByMonth { get; set; }
}

public class ContractDeviceStatDto
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceCode { get; set; } = string.Empty;
    public int ContractCount { get; set; }
}

public class ContractExpiryStatDto
{
    public string Month { get; set; } = string.Empty;
    public int ExpiringCount { get; set; }
    public int ExpiredCount { get; set; }
}
