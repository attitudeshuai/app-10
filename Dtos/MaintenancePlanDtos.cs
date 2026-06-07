using DeviceMaintenanceSystem.Models;

namespace DeviceMaintenanceSystem.Dtos;

public class MaintenancePlanDto
{
    public int Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCode { get; set; }
    public MaintenanceCycle Cycle { get; set; }
    public DateTime PlannedDate { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public string? ResponsibleTechnicianName { get; set; }
    public string Content { get; set; } = string.Empty;
    public MaintenancePlanStatus Status { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string? Result { get; set; }
    public string? Remark { get; set; }
    public int? MaintenanceScheduleId { get; set; }
    public string? MaintenanceScheduleCode { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateMaintenancePlanDto
{
    public string PlanCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public MaintenanceCycle Cycle { get; set; }
    public DateTime PlannedDate { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class UpdateMaintenancePlanDto
{
    public string Title { get; set; } = string.Empty;
    public int? DeviceId { get; set; }
    public MaintenanceCycle? Cycle { get; set; }
    public DateTime? PlannedDate { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public string? Content { get; set; }
    public string? Remark { get; set; }
}

public class ExecuteMaintenancePlanDto
{
    public string Result { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class MaintenancePlanQueryDto : PagedQuery
{
    public MaintenancePlanStatus? Status { get; set; }
    public MaintenanceCycle? Cycle { get; set; }
    public int? DeviceId { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public int? MaintenanceScheduleId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class MaintenanceStatisticsDto
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int ThisMonthCount { get; set; }
    public int ThisMonthCompletedCount { get; set; }
    public decimal? CompletionRate { get; set; }
}

public class MaintenanceScheduleDto
{
    public int Id { get; set; }
    public string ScheduleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCode { get; set; }
    public MaintenanceCycle Cycle { get; set; }
    public int? CustomIntervalDays { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public string? ResponsibleTechnicianName { get; set; }
    public string Content { get; set; } = string.Empty;
    public MaintenanceScheduleStatus Status { get; set; }
    public int GeneratedPlanCount { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateMaintenanceScheduleDto
{
    public string ScheduleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public MaintenanceCycle Cycle { get; set; }
    public int? CustomIntervalDays { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public int GeneratePlanCount { get; set; } = 0;
}

public class UpdateMaintenanceScheduleDto
{
    public string? Title { get; set; }
    public int? DeviceId { get; set; }
    public MaintenanceCycle? Cycle { get; set; }
    public int? CustomIntervalDays { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public string? Content { get; set; }
    public string? Remark { get; set; }
}

public class MaintenanceScheduleQueryDto : PagedQuery
{
    public MaintenanceScheduleStatus? Status { get; set; }
    public MaintenanceCycle? Cycle { get; set; }
    public int? DeviceId { get; set; }
    public int? ResponsibleTechnicianId { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
}

public class GenerateMaintenancePlansDto
{
    public int Count { get; set; }
}
