using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class InspectionPlanService : IInspectionPlanService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public InspectionPlanService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<InspectionPlanDto>> GetPagedAsync(InspectionPlanQueryDto query)
    {
        var queryable = _context.InspectionPlans
            .Include(p => p.Device)
            .Include(p => p.AssignedTechnician)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(p =>
                p.PlanCode.ToLower().Contains(keyword) ||
                p.Title.ToLower().Contains(keyword) ||
                p.InspectionContent.ToLower().Contains(keyword));
        }

        if (query.Status.HasValue)
            queryable = queryable.Where(p => p.Status == query.Status.Value);

        if (query.Cycle.HasValue)
            queryable = queryable.Where(p => p.Cycle == query.Cycle.Value);

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(p => p.DeviceId == query.DeviceId.Value);

        if (query.AssignedTechnicianId.HasValue)
            queryable = queryable.Where(p => p.AssignedTechnicianId == query.AssignedTechnicianId.Value);

        if (query.StartDate.HasValue)
            queryable = queryable.Where(p => p.StartDate >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            queryable = queryable.Where(p => p.StartDate <= query.EndDate.Value);

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "plancode" => query.SortDesc ? queryable.OrderByDescending(p => p.PlanCode) : queryable.OrderBy(p => p.PlanCode),
            "title" => query.SortDesc ? queryable.OrderByDescending(p => p.Title) : queryable.OrderBy(p => p.Title),
            "startdate" => query.SortDesc ? queryable.OrderByDescending(p => p.StartDate) : queryable.OrderBy(p => p.StartDate),
            "status" => query.SortDesc ? queryable.OrderByDescending(p => p.Status) : queryable.OrderBy(p => p.Status),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(p => p.CreatedAt) : queryable.OrderBy(p => p.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(p => p.Id) : queryable.OrderBy(p => p.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<InspectionPlanDto>>(items);
        return new PagedResult<InspectionPlanDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<InspectionPlanDto?> GetByIdAsync(int id)
    {
        var plan = await _context.InspectionPlans
            .Include(p => p.Device)
            .Include(p => p.AssignedTechnician)
            .FirstOrDefaultAsync(p => p.Id == id);
        return plan == null ? null : _mapper.Map<InspectionPlanDto>(plan);
    }

    public async Task<InspectionPlanDto> CreateAsync(CreateInspectionPlanDto dto)
    {
        if (await _context.InspectionPlans.AnyAsync(p => p.PlanCode == dto.PlanCode))
        {
            throw new InvalidOperationException("巡检计划编号已存在");
        }

        var device = await _context.Devices.FindAsync(dto.DeviceId);
        if (device == null)
        {
            throw new KeyNotFoundException("设备不存在");
        }

        if (dto.AssignedTechnicianId.HasValue)
        {
            var tech = await _context.Users.FindAsync(dto.AssignedTechnicianId.Value);
            if (tech == null || tech.Role != UserRole.Technician)
            {
                throw new InvalidOperationException("指定的技术员不存在或角色不正确");
            }
        }

        var plan = _mapper.Map<InspectionPlan>(dto);
        plan.Status = InspectionPlanStatus.Active;
        plan.CreatedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;

        _context.InspectionPlans.Add(plan);
        await _context.SaveChangesAsync();

        if (dto.GenerateTaskCount > 0)
        {
            await GenerateTasksAsync(plan.Id, dto.GenerateTaskCount);
        }

        return await GetByIdAsync(plan.Id) ?? _mapper.Map<InspectionPlanDto>(plan);
    }

    public async Task<InspectionPlanDto?> UpdateAsync(int id, UpdateInspectionPlanDto dto)
    {
        var plan = await _context.InspectionPlans.FindAsync(id);
        if (plan == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Title))
            plan.Title = dto.Title;
        if (dto.DeviceId.HasValue)
            plan.DeviceId = dto.DeviceId.Value;
        if (dto.Cycle.HasValue)
            plan.Cycle = dto.Cycle.Value;
        if (dto.StartDate.HasValue)
            plan.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue)
            plan.EndDate = dto.EndDate.Value;
        if (dto.AssignedTechnicianId.HasValue)
            plan.AssignedTechnicianId = dto.AssignedTechnicianId.Value;
        if (dto.InspectionContent != null)
            plan.InspectionContent = dto.InspectionContent;
        if (dto.Remark != null)
            plan.Remark = dto.Remark;

        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var plan = await _context.InspectionPlans.FindAsync(id);
        if (plan == null) return false;

        _context.InspectionPlans.Remove(plan);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<InspectionPlanDto?> PauseAsync(int id)
    {
        var plan = await _context.InspectionPlans.FindAsync(id);
        if (plan == null) return null;
        if (plan.Status != InspectionPlanStatus.Active)
            throw new InvalidOperationException("只有启用状态的计划才能暂停");

        plan.Status = InspectionPlanStatus.Paused;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InspectionPlanDto?> ResumeAsync(int id)
    {
        var plan = await _context.InspectionPlans.FindAsync(id);
        if (plan == null) return null;
        if (plan.Status != InspectionPlanStatus.Paused)
            throw new InvalidOperationException("只有暂停状态的计划才能恢复");

        plan.Status = InspectionPlanStatus.Active;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InspectionPlanDto?> CancelAsync(int id)
    {
        var plan = await _context.InspectionPlans.FindAsync(id);
        if (plan == null) return null;
        if (plan.Status == InspectionPlanStatus.Completed || plan.Status == InspectionPlanStatus.Cancelled)
            throw new InvalidOperationException("已完成或已取消的计划不能取消");

        plan.Status = InspectionPlanStatus.Cancelled;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<int> GenerateTasksAsync(int planId, int count)
    {
        var plan = await _context.InspectionPlans.FindAsync(planId);
        if (plan == null)
        {
            throw new KeyNotFoundException("巡检计划不存在");
        }

        if (plan.Status != InspectionPlanStatus.Active && plan.Status != InspectionPlanStatus.Paused)
        {
            throw new InvalidOperationException("只有启用或暂停状态的计划才能生成任务");
        }

        if (count <= 0 || count > 365)
        {
            throw new InvalidOperationException("生成数量必须在1-365之间");
        }

        var lastTask = await _context.InspectionTasks
            .Where(t => t.InspectionPlanId == planId)
            .OrderByDescending(t => t.ScheduledDate)
            .FirstOrDefaultAsync();

        var startDate = lastTask != null ? lastTask.ScheduledDate : plan.StartDate;
        var generatedCount = 0;
        var tasks = new List<InspectionTask>();

        for (int i = 0; i < count; i++)
        {
            var scheduledDate = GetNextScheduledDate(plan.StartDate, plan.Cycle, i + (lastTask != null ? plan.GeneratedTaskCount + 1 : 0));

            if (plan.EndDate.HasValue && scheduledDate > plan.EndDate.Value)
            {
                break;
            }

            var sequence = await GetNextTaskSequenceAsync(planId, scheduledDate);
            var taskCode = $"IT{plan.PlanCode}-{scheduledDate:yyyyMMdd}-{sequence:D3}";

            var task = new InspectionTask
            {
                TaskCode = taskCode,
                InspectionPlanId = planId,
                DeviceId = plan.DeviceId,
                AssignedTechnicianId = plan.AssignedTechnicianId,
                InspectionContent = plan.InspectionContent,
                ScheduledDate = scheduledDate,
                Status = InspectionTaskStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            tasks.Add(task);
            generatedCount++;
        }

        _context.InspectionTasks.AddRange(tasks);
        plan.GeneratedTaskCount += generatedCount;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return generatedCount;
    }

    private async Task<int> GetNextTaskSequenceAsync(int planId, DateTime scheduledDate)
    {
        var dateStart = scheduledDate.Date;
        var dateEnd = dateStart.AddDays(1);

        var count = await _context.InspectionTasks
            .Where(t => t.InspectionPlanId == planId
                && t.ScheduledDate >= dateStart
                && t.ScheduledDate < dateEnd)
            .CountAsync();

        return count + 1;
    }

    private DateTime GetNextScheduledDate(DateTime startDate, InspectionCycle cycle, int index)
    {
        return cycle switch
        {
            InspectionCycle.Daily => startDate.AddDays(index),
            InspectionCycle.Weekly => startDate.AddDays(index * 7),
            InspectionCycle.Monthly => GetMonthlyScheduledDate(startDate, index),
            _ => startDate.AddDays(index)
        };
    }

    private DateTime GetMonthlyScheduledDate(DateTime startDate, int monthsToAdd)
    {
        var targetYear = startDate.Year;
        var targetMonth = startDate.Month + monthsToAdd;

        while (targetMonth > 12)
        {
            targetYear++;
            targetMonth -= 12;
        }

        var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        var day = Math.Min(startDate.Day, daysInMonth);

        return new DateTime(targetYear, targetMonth, day,
            startDate.Hour, startDate.Minute, startDate.Second, startDate.Kind);
    }
}
