using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class MaintenancePlanService : IMaintenancePlanService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public MaintenancePlanService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<MaintenancePlanDto>> GetPagedAsync(MaintenancePlanQueryDto query)
    {
        var queryable = _context.MaintenancePlans
            .Include(p => p.Device)
            .Include(p => p.ResponsibleTechnician)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(p =>
                p.PlanCode.ToLower().Contains(keyword) ||
                p.Title.ToLower().Contains(keyword) ||
                p.Content.ToLower().Contains(keyword));
        }

        if (query.Status.HasValue)
            queryable = queryable.Where(p => p.Status == query.Status.Value);

        if (query.Cycle.HasValue)
            queryable = queryable.Where(p => p.Cycle == query.Cycle.Value);

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(p => p.DeviceId == query.DeviceId.Value);

        if (query.ResponsibleTechnicianId.HasValue)
            queryable = queryable.Where(p => p.ResponsibleTechnicianId == query.ResponsibleTechnicianId.Value);

        if (query.StartDate.HasValue)
            queryable = queryable.Where(p => p.PlannedDate >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            queryable = queryable.Where(p => p.PlannedDate <= query.EndDate.Value);

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "plancode" => query.SortDesc ? queryable.OrderByDescending(p => p.PlanCode) : queryable.OrderBy(p => p.PlanCode),
            "title" => query.SortDesc ? queryable.OrderByDescending(p => p.Title) : queryable.OrderBy(p => p.Title),
            "planneddate" => query.SortDesc ? queryable.OrderByDescending(p => p.PlannedDate) : queryable.OrderBy(p => p.PlannedDate),
            "status" => query.SortDesc ? queryable.OrderByDescending(p => p.Status) : queryable.OrderBy(p => p.Status),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(p => p.CreatedAt) : queryable.OrderBy(p => p.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(p => p.Id) : queryable.OrderBy(p => p.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<MaintenancePlanDto>>(items);
        return new PagedResult<MaintenancePlanDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<MaintenancePlanDto?> GetByIdAsync(int id)
    {
        var plan = await _context.MaintenancePlans
            .Include(p => p.Device)
            .Include(p => p.ResponsibleTechnician)
            .FirstOrDefaultAsync(p => p.Id == id);
        return plan == null ? null : _mapper.Map<MaintenancePlanDto>(plan);
    }

    public async Task<MaintenancePlanDto> CreateAsync(CreateMaintenancePlanDto dto)
    {
        if (await _context.MaintenancePlans.AnyAsync(p => p.PlanCode == dto.PlanCode))
        {
            throw new InvalidOperationException("保养计划编号已存在");
        }

        var device = await _context.Devices.FindAsync(dto.DeviceId);
        if (device == null)
        {
            throw new KeyNotFoundException("设备不存在");
        }

        if (dto.ResponsibleTechnicianId.HasValue)
        {
            var tech = await _context.Users.FindAsync(dto.ResponsibleTechnicianId.Value);
            if (tech == null || tech.Role != UserRole.Technician)
            {
                throw new InvalidOperationException("指定的技术员不存在或角色不正确");
            }
        }

        var plan = _mapper.Map<MaintenancePlan>(dto);
        plan.Status = MaintenancePlanStatus.Pending;
        plan.CreatedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;

        _context.MaintenancePlans.Add(plan);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(plan.Id) ?? _mapper.Map<MaintenancePlanDto>(plan);
    }

    public async Task<MaintenancePlanDto?> UpdateAsync(int id, UpdateMaintenancePlanDto dto)
    {
        var plan = await _context.MaintenancePlans.FindAsync(id);
        if (plan == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Title))
            plan.Title = dto.Title;
        if (dto.DeviceId.HasValue)
            plan.DeviceId = dto.DeviceId.Value;
        if (dto.Cycle.HasValue)
            plan.Cycle = dto.Cycle.Value;
        if (dto.PlannedDate.HasValue)
            plan.PlannedDate = dto.PlannedDate.Value;
        if (dto.ResponsibleTechnicianId.HasValue)
            plan.ResponsibleTechnicianId = dto.ResponsibleTechnicianId.Value;
        if (dto.Content != null)
            plan.Content = dto.Content;
        if (dto.Remark != null)
            plan.Remark = dto.Remark;

        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var plan = await _context.MaintenancePlans.FindAsync(id);
        if (plan == null) return false;

        _context.MaintenancePlans.Remove(plan);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MaintenancePlanDto?> StartAsync(int id)
    {
        var plan = await _context.MaintenancePlans.FindAsync(id);
        if (plan == null) return null;
        if (plan.Status != MaintenancePlanStatus.Pending)
            throw new InvalidOperationException("只有待执行的计划才能开始");

        plan.Status = MaintenancePlanStatus.InProgress;
        plan.ActualStartDate = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;

        var device = await _context.Devices.FindAsync(plan.DeviceId);
        if (device != null)
        {
            device.Status = DeviceStatus.Maintenance;
            device.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<MaintenancePlanDto?> CompleteAsync(int id, ExecuteMaintenancePlanDto dto)
    {
        var plan = await _context.MaintenancePlans.FindAsync(id);
        if (plan == null) return null;
        if (plan.Status != MaintenancePlanStatus.InProgress)
            throw new InvalidOperationException("只有执行中的计划才能完成");

        plan.Status = MaintenancePlanStatus.Completed;
        plan.ActualEndDate = DateTime.UtcNow;
        plan.Result = dto.Result;
        if (dto.Remark != null)
            plan.Remark = dto.Remark;
        plan.UpdatedAt = DateTime.UtcNow;

        var device = await _context.Devices.FindAsync(plan.DeviceId);
        if (device != null && device.Status == DeviceStatus.Maintenance)
        {
            device.Status = DeviceStatus.Running;
            device.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<MaintenancePlanDto?> CancelAsync(int id)
    {
        var plan = await _context.MaintenancePlans.FindAsync(id);
        if (plan == null) return null;
        if (plan.Status == MaintenancePlanStatus.Completed || plan.Status == MaintenancePlanStatus.Cancelled)
            throw new InvalidOperationException("已完成或已取消的计划不能取消");

        plan.Status = MaintenancePlanStatus.Cancelled;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<MaintenanceStatisticsDto> GetStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var total = await _context.MaintenancePlans.CountAsync();
        var pending = await _context.MaintenancePlans.CountAsync(p => p.Status == MaintenancePlanStatus.Pending);
        var inProgress = await _context.MaintenancePlans.CountAsync(p => p.Status == MaintenancePlanStatus.InProgress);
        var completed = await _context.MaintenancePlans.CountAsync(p => p.Status == MaintenancePlanStatus.Completed);
        var cancelled = await _context.MaintenancePlans.CountAsync(p => p.Status == MaintenancePlanStatus.Cancelled);

        var thisMonth = await _context.MaintenancePlans.CountAsync(p => p.PlannedDate >= monthStart);
        var thisMonthCompleted = await _context.MaintenancePlans.CountAsync(p =>
            p.Status == MaintenancePlanStatus.Completed && p.ActualEndDate >= monthStart);

        decimal? completionRate = thisMonth > 0 ? Math.Round((decimal)thisMonthCompleted / thisMonth * 100, 2) : null;

        return new MaintenanceStatisticsDto
        {
            TotalCount = total,
            PendingCount = pending,
            InProgressCount = inProgress,
            CompletedCount = completed,
            CancelledCount = cancelled,
            ThisMonthCount = thisMonth,
            ThisMonthCompletedCount = thisMonthCompleted,
            CompletionRate = completionRate
        };
    }
}
