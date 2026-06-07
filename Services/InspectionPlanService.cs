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
        plan.Status = InspectionPlanStatus.Pending;
        plan.CreatedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;

        _context.InspectionPlans.Add(plan);
        await _context.SaveChangesAsync();

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
        if (dto.PlannedDate.HasValue)
            plan.PlannedDate = dto.PlannedDate.Value;
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

    public async Task<InspectionPlanDto?> StartAsync(int id)
    {
        var plan = await _context.InspectionPlans.FindAsync(id);
        if (plan == null) return null;
        if (plan.Status != InspectionPlanStatus.Pending)
            throw new InvalidOperationException("只有待执行的计划才能开始");

        plan.Status = InspectionPlanStatus.InProgress;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InspectionPlanDto?> CompleteAsync(int id)
    {
        var plan = await _context.InspectionPlans.FindAsync(id);
        if (plan == null) return null;
        if (plan.Status != InspectionPlanStatus.InProgress)
            throw new InvalidOperationException("只有执行中的计划才能完成");

        plan.Status = InspectionPlanStatus.Completed;
        plan.ActualInspectionDate = DateTime.UtcNow;
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
}
