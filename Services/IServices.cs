using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;

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
    Task<DeviceDto?> GetByIdAsync(int id);
    Task<DeviceDto> CreateAsync(CreateDeviceDto dto);
    Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceDto dto);
    Task<bool> DeleteAsync(int id);
    Task<DeviceStatisticsDto> GetStatisticsAsync();
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
