using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class MaintenanceScheduleService : IMaintenanceScheduleService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public MaintenanceScheduleService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<MaintenanceScheduleDto>> GetPagedAsync(MaintenanceScheduleQueryDto query)
    {
        var queryable = _context.MaintenanceSchedules
            .Include(s => s.Device)
            .Include(s => s.ResponsibleTechnician)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(s =>
                s.ScheduleCode.ToLower().Contains(keyword) ||
                s.Title.ToLower().Contains(keyword) ||
                s.Content.ToLower().Contains(keyword));
        }

        if (query.Status.HasValue)
            queryable = queryable.Where(s => s.Status == query.Status.Value);

        if (query.Cycle.HasValue)
            queryable = queryable.Where(s => s.Cycle == query.Cycle.Value);

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(s => s.DeviceId == query.DeviceId.Value);

        if (query.ResponsibleTechnicianId.HasValue)
            queryable = queryable.Where(s => s.ResponsibleTechnicianId == query.ResponsibleTechnicianId.Value);

        if (query.StartDateFrom.HasValue)
            queryable = queryable.Where(s => s.StartDate >= query.StartDateFrom.Value);

        if (query.StartDateTo.HasValue)
            queryable = queryable.Where(s => s.StartDate <= query.StartDateTo.Value);

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "schedulecode" => query.SortDesc ? queryable.OrderByDescending(s => s.ScheduleCode) : queryable.OrderBy(s => s.ScheduleCode),
            "title" => query.SortDesc ? queryable.OrderByDescending(s => s.Title) : queryable.OrderBy(s => s.Title),
            "startdate" => query.SortDesc ? queryable.OrderByDescending(s => s.StartDate) : queryable.OrderBy(s => s.StartDate),
            "status" => query.SortDesc ? queryable.OrderByDescending(s => s.Status) : queryable.OrderBy(s => s.Status),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(s => s.CreatedAt) : queryable.OrderBy(s => s.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(s => s.Id) : queryable.OrderBy(s => s.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<MaintenanceScheduleDto>>(items);
        return new PagedResult<MaintenanceScheduleDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<MaintenanceScheduleDto?> GetByIdAsync(int id)
    {
        var schedule = await _context.MaintenanceSchedules
            .Include(s => s.Device)
            .Include(s => s.ResponsibleTechnician)
            .FirstOrDefaultAsync(s => s.Id == id);
        return schedule == null ? null : _mapper.Map<MaintenanceScheduleDto>(schedule);
    }

    public async Task<MaintenanceScheduleDto> CreateAsync(CreateMaintenanceScheduleDto dto)
    {
        if (await _context.MaintenanceSchedules.AnyAsync(s => s.ScheduleCode == dto.ScheduleCode))
        {
            throw new InvalidOperationException("保养周期规则编号已存在");
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

        if (dto.Cycle == MaintenanceCycle.Custom && (!dto.CustomIntervalDays.HasValue || dto.CustomIntervalDays.Value <= 0))
        {
            throw new InvalidOperationException("自定义周期必须指定大于0的间隔天数");
        }

        var schedule = _mapper.Map<MaintenanceSchedule>(dto);
        schedule.Status = MaintenanceScheduleStatus.Active;
        schedule.CreatedAt = DateTime.UtcNow;
        schedule.UpdatedAt = DateTime.UtcNow;

        _context.MaintenanceSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        if (dto.GeneratePlanCount > 0)
        {
            await GeneratePlansAsync(schedule.Id, dto.GeneratePlanCount);
        }

        return await GetByIdAsync(schedule.Id) ?? _mapper.Map<MaintenanceScheduleDto>(schedule);
    }

    public async Task<MaintenanceScheduleDto?> UpdateAsync(int id, UpdateMaintenanceScheduleDto dto)
    {
        var schedule = await _context.MaintenanceSchedules.FindAsync(id);
        if (schedule == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Title))
            schedule.Title = dto.Title;
        if (dto.DeviceId.HasValue)
        {
            var device = await _context.Devices.FindAsync(dto.DeviceId.Value);
            if (device == null)
                throw new KeyNotFoundException("设备不存在");
            schedule.DeviceId = dto.DeviceId.Value;
        }
        if (dto.Cycle.HasValue)
        {
            if (dto.Cycle.Value == MaintenanceCycle.Custom && (!dto.CustomIntervalDays.HasValue || dto.CustomIntervalDays.Value <= 0))
                throw new InvalidOperationException("自定义周期必须指定大于0的间隔天数");
            schedule.Cycle = dto.Cycle.Value;
        }
        if (dto.CustomIntervalDays.HasValue)
            schedule.CustomIntervalDays = dto.CustomIntervalDays.Value;
        if (dto.StartDate.HasValue)
            schedule.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue)
            schedule.EndDate = dto.EndDate.Value;
        if (dto.ResponsibleTechnicianId.HasValue)
        {
            var tech = await _context.Users.FindAsync(dto.ResponsibleTechnicianId.Value);
            if (tech == null || tech.Role != UserRole.Technician)
                throw new InvalidOperationException("指定的技术员不存在或角色不正确");
            schedule.ResponsibleTechnicianId = dto.ResponsibleTechnicianId.Value;
        }
        if (dto.Content != null)
            schedule.Content = dto.Content;
        if (dto.Remark != null)
            schedule.Remark = dto.Remark;

        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var schedule = await _context.MaintenanceSchedules.FindAsync(id);
        if (schedule == null) return false;

        _context.MaintenanceSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MaintenanceScheduleDto?> PauseAsync(int id)
    {
        var schedule = await _context.MaintenanceSchedules.FindAsync(id);
        if (schedule == null) return null;
        if (schedule.Status != MaintenanceScheduleStatus.Active)
            throw new InvalidOperationException("只有启用状态的周期规则才能暂停");

        schedule.Status = MaintenanceScheduleStatus.Paused;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<MaintenanceScheduleDto?> ResumeAsync(int id)
    {
        var schedule = await _context.MaintenanceSchedules.FindAsync(id);
        if (schedule == null) return null;
        if (schedule.Status != MaintenanceScheduleStatus.Paused)
            throw new InvalidOperationException("只有暂停状态的周期规则才能恢复");

        schedule.Status = MaintenanceScheduleStatus.Active;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<MaintenanceScheduleDto?> CancelAsync(int id)
    {
        var schedule = await _context.MaintenanceSchedules.FindAsync(id);
        if (schedule == null) return null;
        if (schedule.Status == MaintenanceScheduleStatus.Completed || schedule.Status == MaintenanceScheduleStatus.Cancelled)
            throw new InvalidOperationException("已完成或已取消的周期规则不能取消");

        schedule.Status = MaintenanceScheduleStatus.Cancelled;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<int> GeneratePlansAsync(int scheduleId, int count)
    {
        var schedule = await _context.MaintenanceSchedules.FindAsync(scheduleId);
        if (schedule == null)
        {
            throw new KeyNotFoundException("保养周期规则不存在");
        }

        if (schedule.Status != MaintenanceScheduleStatus.Active && schedule.Status != MaintenanceScheduleStatus.Paused)
        {
            throw new InvalidOperationException("只有启用或暂停状态的周期规则才能生成保养计划");
        }

        if (count <= 0 || count > 365)
        {
            throw new InvalidOperationException("生成数量必须在1-365之间");
        }

        var lastPlan = await _context.MaintenancePlans
            .Where(p => p.MaintenanceScheduleId == scheduleId)
            .OrderByDescending(p => p.PlannedDate)
            .FirstOrDefaultAsync();

        var generatedCount = 0;
        var plans = new List<MaintenancePlan>();

        for (int i = 0; i < count; i++)
        {
            var index = schedule.GeneratedPlanCount + i;
            var plannedDate = GetNextPlannedDate(schedule.StartDate, schedule.Cycle, schedule.CustomIntervalDays, index);

            if (schedule.EndDate.HasValue && plannedDate > schedule.EndDate.Value)
            {
                break;
            }

            var sequence = await GetNextPlanSequenceAsync(scheduleId, plannedDate);
            var planCode = $"MP{schedule.ScheduleCode}-{plannedDate:yyyyMMdd}-{sequence:D3}";

            var plan = new MaintenancePlan
            {
                PlanCode = planCode,
                Title = schedule.Title,
                DeviceId = schedule.DeviceId,
                Cycle = schedule.Cycle,
                PlannedDate = plannedDate,
                ResponsibleTechnicianId = schedule.ResponsibleTechnicianId,
                Content = schedule.Content,
                Status = MaintenancePlanStatus.Pending,
                MaintenanceScheduleId = scheduleId,
                Remark = schedule.Remark,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            plans.Add(plan);
            generatedCount++;
        }

        _context.MaintenancePlans.AddRange(plans);
        schedule.GeneratedPlanCount += generatedCount;
        schedule.LastGeneratedDate = DateTime.UtcNow;
        schedule.UpdatedAt = DateTime.UtcNow;

        if (schedule.EndDate.HasValue && generatedCount < count)
        {
            var lastPlannedDate = plans.LastOrDefault()?.PlannedDate;
            if (lastPlannedDate.HasValue && lastPlannedDate.Value >= schedule.EndDate.Value)
            {
                schedule.Status = MaintenanceScheduleStatus.Completed;
            }
        }

        await _context.SaveChangesAsync();
        return generatedCount;
    }

    public async Task<int> GenerateUpcomingPlansAsync(int monthsAhead = 3)
    {
        var now = DateTime.UtcNow;
        var targetDate = now.AddMonths(monthsAhead);

        var activeSchedules = await _context.MaintenanceSchedules
            .Where(s => s.Status == MaintenanceScheduleStatus.Active)
            .ToListAsync();

        var totalGenerated = 0;

        foreach (var schedule in activeSchedules)
        {
            try
            {
                var lastPlan = await _context.MaintenancePlans
                    .Where(p => p.MaintenanceScheduleId == schedule.Id)
                    .OrderByDescending(p => p.PlannedDate)
                    .FirstOrDefaultAsync();

                var nextIndex = schedule.GeneratedPlanCount;
                var generatedCount = 0;
                var plans = new List<MaintenancePlan>();

                while (true)
                {
                    var plannedDate = GetNextPlannedDate(schedule.StartDate, schedule.Cycle, schedule.CustomIntervalDays, nextIndex);

                    if (plannedDate > targetDate)
                        break;

                    if (schedule.EndDate.HasValue && plannedDate > schedule.EndDate.Value)
                        break;

                    if (lastPlan != null && plannedDate <= lastPlan.PlannedDate)
                    {
                        nextIndex++;
                        continue;
                    }

                    var sequence = await GetNextPlanSequenceAsync(schedule.Id, plannedDate);
                    var planCode = $"MP{schedule.ScheduleCode}-{plannedDate:yyyyMMdd}-{sequence:D3}";

                    var plan = new MaintenancePlan
                    {
                        PlanCode = planCode,
                        Title = schedule.Title,
                        DeviceId = schedule.DeviceId,
                        Cycle = schedule.Cycle,
                        PlannedDate = plannedDate,
                        ResponsibleTechnicianId = schedule.ResponsibleTechnicianId,
                        Content = schedule.Content,
                        Status = MaintenancePlanStatus.Pending,
                        MaintenanceScheduleId = schedule.Id,
                        Remark = schedule.Remark,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    plans.Add(plan);
                    generatedCount++;
                    nextIndex++;

                    if (generatedCount >= 100)
                        break;
                }

                if (plans.Count > 0)
                {
                    _context.MaintenancePlans.AddRange(plans);
                    schedule.GeneratedPlanCount += generatedCount;
                    schedule.LastGeneratedDate = DateTime.UtcNow;
                    schedule.UpdatedAt = DateTime.UtcNow;

                    if (schedule.EndDate.HasValue)
                    {
                        var lastPlannedDate = plans.Last().PlannedDate;
                        if (lastPlannedDate >= schedule.EndDate.Value)
                        {
                            schedule.Status = MaintenanceScheduleStatus.Completed;
                        }
                    }

                    totalGenerated += generatedCount;
                }
            }
            catch (Exception)
            {
                continue;
            }
        }

        if (totalGenerated > 0)
        {
            await _context.SaveChangesAsync();
        }

        return totalGenerated;
    }

    private async Task<int> GetNextPlanSequenceAsync(int scheduleId, DateTime plannedDate)
    {
        var dateStart = plannedDate.Date;
        var dateEnd = dateStart.AddDays(1);

        var count = await _context.MaintenancePlans
            .Where(p => p.MaintenanceScheduleId == scheduleId
                && p.PlannedDate >= dateStart
                && p.PlannedDate < dateEnd)
            .CountAsync();

        return count + 1;
    }

    private DateTime GetNextPlannedDate(DateTime startDate, MaintenanceCycle cycle, int? customIntervalDays, int index)
    {
        return cycle switch
        {
            MaintenanceCycle.Daily => startDate.AddDays(index),
            MaintenanceCycle.Weekly => startDate.AddDays(index * 7),
            MaintenanceCycle.Monthly => GetMonthlyPlannedDate(startDate, index),
            MaintenanceCycle.Quarterly => GetMonthlyPlannedDate(startDate, index * 3),
            MaintenanceCycle.Yearly => GetYearlyPlannedDate(startDate, index),
            MaintenanceCycle.Custom => startDate.AddDays((customIntervalDays ?? 1) * index),
            _ => startDate.AddDays(index)
        };
    }

    private DateTime GetMonthlyPlannedDate(DateTime startDate, int monthsToAdd)
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

    private DateTime GetYearlyPlannedDate(DateTime startDate, int yearsToAdd)
    {
        var targetYear = startDate.Year + yearsToAdd;
        var daysInMonth = DateTime.DaysInMonth(targetYear, startDate.Month);
        var day = Math.Min(startDate.Day, daysInMonth);

        return new DateTime(targetYear, startDate.Month, day,
            startDate.Hour, startDate.Minute, startDate.Second, startDate.Kind);
    }
}
