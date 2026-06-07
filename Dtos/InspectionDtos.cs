using DeviceMaintenanceSystem.Models;

namespace DeviceMaintenanceSystem.Dtos;

public class InspectionPlanDto
{
    public int Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCode { get; set; }
    public InspectionCycle Cycle { get; set; }
    public DateTime PlannedDate { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public string InspectionContent { get; set; } = string.Empty;
    public InspectionPlanStatus Status { get; set; }
    public DateTime? ActualInspectionDate { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInspectionPlanDto
{
    public string PlanCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public InspectionCycle Cycle { get; set; }
    public DateTime PlannedDate { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public string InspectionContent { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class UpdateInspectionPlanDto
{
    public string? Title { get; set; }
    public int? DeviceId { get; set; }
    public InspectionCycle? Cycle { get; set; }
    public DateTime? PlannedDate { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public string? InspectionContent { get; set; }
    public string? Remark { get; set; }
}

public class InspectionPlanQueryDto : PagedQuery
{
    public InspectionPlanStatus? Status { get; set; }
    public InspectionCycle? Cycle { get; set; }
    public int? DeviceId { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class InspectionPhotoDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class InspectionRecordDto
{
    public int Id { get; set; }
    public string RecordCode { get; set; } = string.Empty;
    public int? InspectionPlanId { get; set; }
    public string? InspectionPlanCode { get; set; }
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCode { get; set; }
    public int InspectorId { get; set; }
    public string? InspectorName { get; set; }
    public DeviceStatus DeviceStatus { get; set; }
    public InspectionResult Result { get; set; }
    public string? AbnormalDescription { get; set; }
    public string? Remark { get; set; }
    public DateTime InspectionTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InspectionPhotoDto> Photos { get; set; } = new List<InspectionPhotoDto>();
}

public class CreateInspectionRecordDto
{
    public int? InspectionPlanId { get; set; }
    public int DeviceId { get; set; }
    public DeviceStatus DeviceStatus { get; set; }
    public InspectionResult Result { get; set; }
    public string? AbnormalDescription { get; set; }
    public string? Remark { get; set; }
    public DateTime InspectionTime { get; set; }
}

public class InspectionRecordQueryDto : PagedQuery
{
    public int? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public int? InspectorId { get; set; }
    public InspectionResult? Result { get; set; }
    public DeviceStatus? DeviceStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? InspectionPlanId { get; set; }
}

public class InspectionStatisticsDto
{
    public int TotalPlanCount { get; set; }
    public int PendingPlanCount { get; set; }
    public int InProgressPlanCount { get; set; }
    public int CompletedPlanCount { get; set; }
    public int TotalRecordCount { get; set; }
    public int NormalRecordCount { get; set; }
    public int AbnormalRecordCount { get; set; }
    public int ThisMonthRecordCount { get; set; }
    public decimal? AbnormalRate { get; set; }
}
