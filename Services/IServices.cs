using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.AspNetCore.Http;

namespace DeviceMaintenanceSystem.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserDto> GetCurrentUserAsync(int userId);
}

public interface IUserService
{
    Task<PagedResult<UserDto>> GetPagedAsync(UserQueryDto query);
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<bool> ResetPasswordAsync(int userId, string newPassword);
}

public interface IDeviceService
{
    Task<PagedResult<DeviceDto>> GetPagedAsync(DeviceQueryDto query);
    Task<DeviceDetailDto?> GetByIdAsync(int id);
    Task<DeviceDto> CreateAsync(CreateDeviceDto dto);
    Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceDto dto);
    Task<bool> DeleteAsync(int id);
    Task<DeviceStatisticsDto> GetStatisticsAsync();
    Task<DeviceDto?> UpdateStatusAsync(int id, DeviceStatus newStatus);
    Task<DeviceImportResultDto> ImportFromCsvAsync(Stream csvStream);
}

public interface IMaintenancePlanService
{
    Task<PagedResult<MaintenancePlanDto>> GetPagedAsync(MaintenancePlanQueryDto query);
    Task<MaintenancePlanDto?> GetByIdAsync(int id);
    Task<MaintenancePlanDto> CreateAsync(CreateMaintenancePlanDto dto);
    Task<MaintenancePlanDto?> UpdateAsync(int id, UpdateMaintenancePlanDto dto);
    Task<bool> DeleteAsync(int id);
    Task<MaintenancePlanDto?> StartAsync(int id);
    Task<MaintenancePlanDto?> CompleteAsync(int id, ExecuteMaintenancePlanDto dto);
    Task<MaintenancePlanDto?> CancelAsync(int id);
    Task<MaintenanceStatisticsDto> GetStatisticsAsync();
    Task<int> SendUpcomingRemindersAsync(int daysAhead = 3);
}

public interface IMaintenanceScheduleService
{
    Task<PagedResult<MaintenanceScheduleDto>> GetPagedAsync(MaintenanceScheduleQueryDto query);
    Task<MaintenanceScheduleDto?> GetByIdAsync(int id);
    Task<MaintenanceScheduleDto> CreateAsync(CreateMaintenanceScheduleDto dto);
    Task<MaintenanceScheduleDto?> UpdateAsync(int id, UpdateMaintenanceScheduleDto dto);
    Task<bool> DeleteAsync(int id);
    Task<MaintenanceScheduleDto?> PauseAsync(int id);
    Task<MaintenanceScheduleDto?> ResumeAsync(int id);
    Task<MaintenanceScheduleDto?> CancelAsync(int id);
    Task<int> GeneratePlansAsync(int scheduleId, int count);
    Task<int> GenerateUpcomingPlansAsync(int monthsAhead = 3);
}

public interface IFaultReportService
{
    Task<PagedResult<FaultReportDto>> GetPagedAsync(FaultReportQueryDto query);
    Task<FaultReportDto?> GetByIdAsync(int id);
    Task<FaultReportDto> CreateAsync(CreateFaultReportDto dto, int reporterId);
    Task<FaultReportDto?> UpdateAsync(int id, UpdateFaultReportDto dto);
    Task<bool> DeleteAsync(int id);
    Task<FaultReportDto?> AssignAsync(int id, AssignFaultReportDto dto);
    Task<FaultReportDto?> StartAsync(int id);
    Task<FaultReportDto?> CompleteAsync(int id, CompleteFaultReportDto dto);
    Task<FaultReportDto?> CancelAsync(int id);
    Task<FaultStatisticsDto> GetStatisticsAsync();
}

public interface ISparePartService
{
    Task<PagedResult<SparePartDto>> GetPagedAsync(SparePartQueryDto query);
    Task<SparePartDto?> GetByIdAsync(int id);
    Task<SparePartDto> CreateAsync(CreateSparePartDto dto);
    Task<SparePartDto?> UpdateAsync(int id, UpdateSparePartDto dto);
    Task<bool> DeleteAsync(int id);
    Task<SparePartStatisticsDto> GetStatisticsAsync();
    Task<PagedResult<SparePartConsumptionDto>> GetConsumptionsAsync(SparePartConsumptionQueryDto query);
}

public interface IInspectionPlanService
{
    Task<PagedResult<InspectionPlanDto>> GetPagedAsync(InspectionPlanQueryDto query);
    Task<InspectionPlanDto?> GetByIdAsync(int id);
    Task<InspectionPlanDto> CreateAsync(CreateInspectionPlanDto dto);
    Task<InspectionPlanDto?> UpdateAsync(int id, UpdateInspectionPlanDto dto);
    Task<bool> DeleteAsync(int id);
    Task<InspectionPlanDto?> PauseAsync(int id);
    Task<InspectionPlanDto?> ResumeAsync(int id);
    Task<InspectionPlanDto?> CancelAsync(int id);
    Task<int> GenerateTasksAsync(int planId, int count);
}

public interface IInspectionTaskService
{
    Task<PagedResult<InspectionTaskDto>> GetPagedAsync(InspectionTaskQueryDto query);
    Task<InspectionTaskDto?> GetByIdAsync(int id);
    Task<InspectionTaskDto?> StartAsync(int id);
    Task<InspectionTaskDto?> CompleteAsync(int id);
    Task<InspectionTaskDto?> CancelAsync(int id);
    Task<List<InspectionTaskDto>> GetPlanTasksAsync(int planId);
}

public interface IInspectionRecordService
{
    Task<PagedResult<InspectionRecordDto>> GetPagedAsync(InspectionRecordQueryDto query);
    Task<InspectionRecordDto?> GetByIdAsync(int id);
    Task<InspectionRecordDto> CreateAsync(CreateInspectionRecordDto dto, int inspectorId);
    Task<bool> DeleteAsync(int id);
    Task<List<InspectionRecordDto>> GetDeviceInspectionHistoryAsync(int deviceId);
    Task<InspectionPhotoDto> UploadPhotoAsync(int recordId, IFormFile file, string? description);
    Task<InspectionStatisticsDto> GetStatisticsAsync();
}

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetPagedAsync(int userId, NotificationQueryDto query);
    Task<NotificationDto?> GetByIdAsync(int id, int userId);
    Task<NotificationStatisticsDto> GetStatisticsAsync(int userId);
    Task<NotificationDto> CreateAsync(CreateNotificationDto dto);
    Task BatchCreateAsync(BatchCreateNotificationDto dto);
    Task EnqueueAsync(CreateNotificationDto dto);
    Task BatchEnqueueAsync(BatchCreateNotificationDto dto);
    Task<NotificationDto?> MarkAsReadAsync(int id, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<bool> DeleteAsync(int id, int userId);
    Task<int> DeleteReadAsync(int userId);
}

public interface INotificationQueue
{
    bool Enqueue(Notification notification);
    int EnqueueRange(IEnumerable<Notification> notifications);
    Task<List<Notification>> DequeueBatchAsync(int batchSize, CancellationToken stoppingToken);
    int GetQueueCount();
}

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(SupplierQueryDto query);
    Task<SupplierDetailDto?> GetByIdAsync(int id);
    Task<SupplierDto> CreateAsync(CreateSupplierDto dto);
    Task<SupplierDto?> UpdateAsync(int id, UpdateSupplierDto dto);
    Task<bool> DeleteAsync(int id);
    Task<SupplierStatisticsDto> GetStatisticsAsync();
    Task<List<SupplierDto>> GetAllAsync();
}

public interface IMaintenanceContractService
{
    Task<PagedResult<MaintenanceContractDto>> GetPagedAsync(MaintenanceContractQueryDto query);
    Task<MaintenanceContractDetailDto?> GetByIdAsync(int id);
    Task<MaintenanceContractDto> CreateAsync(CreateMaintenanceContractDto dto);
    Task<MaintenanceContractDto?> UpdateAsync(int id, UpdateMaintenanceContractDto dto);
    Task<bool> DeleteAsync(int id);
    Task<MaintenanceContractStatisticsDto> GetStatisticsAsync();
    Task<List<MaintenanceContractDto>> GetDeviceContractsAsync(int deviceId);
    Task<int> SendExpiringRemindersAsync(int daysAhead = 30);
}

public interface IDeviceBorrowService
{
    Task<PagedResult<DeviceBorrowRecordDto>> GetPagedAsync(DeviceBorrowQueryDto query);
    Task<DeviceBorrowRecordDto?> GetByIdAsync(int id);
    Task<DeviceBorrowRecordDto> BorrowAsync(CreateDeviceBorrowDto dto, int operatorId);
    Task<DeviceBorrowRecordDto?> ReturnAsync(int id, ReturnDeviceBorrowDto dto, int operatorId);
    Task<bool> DeleteAsync(int id);
    Task<DeviceBorrowStatisticsDto> GetStatisticsAsync();
    Task<List<DeviceBorrowRecordDto>> GetDeviceBorrowRecordsAsync(int deviceId);
}

public interface IKnowledgeBaseService
{
    Task<PagedResult<KnowledgeBaseArticleBriefDto>> GetPagedAsync(KnowledgeBaseArticleQueryDto query);
    Task<KnowledgeBaseArticleDto?> GetByIdAsync(int id);
    Task<KnowledgeBaseArticleDto> CreateAsync(CreateKnowledgeBaseArticleDto dto, int authorId);
    Task<KnowledgeBaseArticleDto?> UpdateAsync(int id, UpdateKnowledgeBaseArticleDto dto);
    Task<bool> DeleteAsync(int id);
    Task<KnowledgeBaseStatisticsDto> GetStatisticsAsync();
    Task<KnowledgeBaseArticleDto?> IncrementViewCountAsync(int id);
    Task<List<KnowledgeBaseArticleBriefDto>> GetRecommendedArticlesByDeviceIdAsync(int deviceId, int limit = 5);
}
