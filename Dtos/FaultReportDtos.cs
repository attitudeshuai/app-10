using DeviceMaintenanceSystem.Models;

namespace DeviceMaintenanceSystem.Dtos;

public class FaultReportDto
{
    public int Id { get; set; }
    public string ReportCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCode { get; set; }
    public int ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public FaultPriority Priority { get; set; }
    public FaultStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? FaultLocation { get; set; }
    public DateTime ReportTime { get; set; }
    public DateTime? AssignTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string? Solution { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFaultReportDto
{
    public string ReportCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public FaultPriority Priority { get; set; } = FaultPriority.Medium;
    public string Description { get; set; } = string.Empty;
    public string? FaultLocation { get; set; }
}

public class UpdateFaultReportDto
{
    public string Title { get; set; } = string.Empty;
    public int? DeviceId { get; set; }
    public FaultPriority? Priority { get; set; }
    public string? Description { get; set; }
    public string? FaultLocation { get; set; }
}

public class AssignFaultReportDto
{
    public int TechnicianId { get; set; }
    public string? Remark { get; set; }
}

public class CompleteFaultReportDto
{
    public string Solution { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class FaultReportQueryDto : PagedQuery
{
    public FaultStatus? Status { get; set; }
    public FaultPriority? Priority { get; set; }
    public int? DeviceId { get; set; }
    public int? ReporterId { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class FaultStatisticsDto
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int AssignedCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int ThisMonthCount { get; set; }
    public int ThisMonthCompletedCount { get; set; }
    public double? AverageResolutionHours { get; set; }
}
