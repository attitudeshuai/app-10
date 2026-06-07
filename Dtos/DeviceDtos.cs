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
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DeviceDetailDto : DeviceDto
{
    public List<InspectionRecordDto> RecentInspectionRecords { get; set; } = new List<InspectionRecordDto>();
    public int InspectionRecordCount { get; set; }
    public List<DeviceBorrowRecordDto> BorrowRecords { get; set; } = new List<DeviceBorrowRecordDto>();
    public int BorrowRecordCount { get; set; }
    public DeviceBorrowRecordDto? CurrentBorrowRecord { get; set; }
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
    public int? SupplierId { get; set; }
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
    public int? SupplierId { get; set; }
}

public class DeviceQueryDto : PagedQuery
{
    public DeviceStatus? Status { get; set; }
    public string? Category { get; set; }
    public int? SupplierId { get; set; }
}

public class DeviceStatisticsDto
{
    public int TotalCount { get; set; }
    public int RunningCount { get; set; }
    public int StandbyCount { get; set; }
    public int MaintenanceCount { get; set; }
    public int FaultCount { get; set; }
    public int ScrappedCount { get; set; }
    public int BorrowedCount { get; set; }
    public List<CategoryStatDto>? ByCategory { get; set; }
}

public class CategoryStatDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DeviceImportResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<DeviceImportErrorDto> Errors { get; set; } = new();
}

public class DeviceImportErrorDto
{
    public int RowNumber { get; set; }
    public string? DeviceCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DeviceBorrowRecordDto
{
    public int Id { get; set; }
    public string RecordCode { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCode { get; set; }
    public BorrowType BorrowType { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string? BorrowerContact { get; set; }
    public string? BorrowerDepartment { get; set; }
    public string? BorrowerCompany { get; set; }
    public DateTime BorrowTime { get; set; }
    public DateTime ExpectedReturnTime { get; set; }
    public DateTime? ActualReturnTime { get; set; }
    public string? BorrowPurpose { get; set; }
    public string? ReturnRemark { get; set; }
    public int? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public int? ReturnOperatorId { get; set; }
    public string? ReturnOperatorName { get; set; }
    public DeviceStatus StatusBeforeBorrow { get; set; }
    public bool IsReturned { get; set; }
    public BorrowApprovalStatus ApprovalStatus { get; set; }
    public int? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public string? ApprovalRemark { get; set; }
    public int? ApplicantId { get; set; }
    public string? ApplicantName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDeviceBorrowDto
{
    public int DeviceId { get; set; }
    public BorrowType BorrowType { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string? BorrowerContact { get; set; }
    public string? BorrowerDepartment { get; set; }
    public string? BorrowerCompany { get; set; }
    public DateTime BorrowTime { get; set; }
    public DateTime ExpectedReturnTime { get; set; }
    public string? BorrowPurpose { get; set; }
}

public class ReturnDeviceBorrowDto
{
    public string? ReturnRemark { get; set; }
}

public class ApproveBorrowDto
{
    public string? ApprovalRemark { get; set; }
}

public class RejectBorrowDto
{
    public string ApprovalRemark { get; set; } = string.Empty;
}

public class DeviceBorrowQueryDto : PagedQuery
{
    public int? DeviceId { get; set; }
    public BorrowType? BorrowType { get; set; }
    public bool? IsReturned { get; set; }
    public BorrowApprovalStatus? ApprovalStatus { get; set; }
    public int? ApplicantId { get; set; }
    public string? BorrowerName { get; set; }
    public DateTime? BorrowTimeFrom { get; set; }
    public DateTime? BorrowTimeTo { get; set; }
}

public class DeviceBorrowStatisticsDto
{
    public int TotalCount { get; set; }
    public int BorrowingCount { get; set; }
    public int ReturnedCount { get; set; }
    public int ExternalBorrowCount { get; set; }
    public int InternalBorrowCount { get; set; }
    public int OverdueCount { get; set; }
    public int PendingApprovalCount { get; set; }
}
