namespace DeviceMaintenanceSystem.Dtos;

public class SparePartDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int MinStockWarning { get; set; }
    public bool IsLowStock => StockQuantity <= MinStockWarning;
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCode { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSparePartDto
{
    public string Name { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int MinStockWarning { get; set; }
    public int DeviceId { get; set; }
    public string? Description { get; set; }
}

public class UpdateSparePartDto
{
    public string? Name { get; set; }
    public string? Specification { get; set; }
    public int? StockQuantity { get; set; }
    public int? MinStockWarning { get; set; }
    public string? Description { get; set; }
}

public class SparePartQueryDto : PagedQuery
{
    public int? DeviceId { get; set; }
    public bool? LowStockOnly { get; set; }
}

public class SparePartStatisticsDto
{
    public int TotalCount { get; set; }
    public int LowStockCount { get; set; }
    public decimal LowStockRatio { get; set; }
    public int TotalStockQuantity { get; set; }
    public List<SparePartDto>? LowStockItems { get; set; }
}

public class SparePartConsumptionDto
{
    public int Id { get; set; }
    public int SparePartId { get; set; }
    public string? SparePartName { get; set; }
    public string? SparePartSpecification { get; set; }
    public int FaultReportId { get; set; }
    public string? FaultReportCode { get; set; }
    public int Quantity { get; set; }
    public DateTime ConsumedAt { get; set; }
    public string? Remark { get; set; }
}

public class SparePartConsumptionItemDto
{
    public int SparePartId { get; set; }
    public int Quantity { get; set; }
    public string? Remark { get; set; }
}

public class SparePartConsumptionQueryDto : PagedQuery
{
    public int? SparePartId { get; set; }
    public int? FaultReportId { get; set; }
    public int? DeviceId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
