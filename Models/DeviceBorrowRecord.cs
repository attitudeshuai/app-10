namespace DeviceMaintenanceSystem.Models;

public class DeviceBorrowRecord
{
    public int Id { get; set; }
    public string RecordCode { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
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
    public User? Operator { get; set; }
    public int? ReturnOperatorId { get; set; }
    public User? ReturnOperator { get; set; }
    public DeviceStatus StatusBeforeBorrow { get; set; }
    public bool IsReturned { get; set; } = false;
    public BorrowApprovalStatus ApprovalStatus { get; set; } = BorrowApprovalStatus.Pending;
    public int? ApproverId { get; set; }
    public User? Approver { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public string? ApprovalRemark { get; set; }
    public int? ApplicantId { get; set; }
    public User? Applicant { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
