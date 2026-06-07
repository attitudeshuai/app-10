using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class FaultReportService : IFaultReportService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public FaultReportService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<FaultReportDto>> GetPagedAsync(FaultReportQueryDto query)
    {
        var queryable = _context.FaultReports
            .Include(f => f.Device)
            .Include(f => f.Reporter)
            .Include(f => f.AssignedTechnician)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(f =>
                f.ReportCode.ToLower().Contains(keyword) ||
                f.Title.ToLower().Contains(keyword) ||
                f.Description.ToLower().Contains(keyword));
        }

        if (query.Status.HasValue)
            queryable = queryable.Where(f => f.Status == query.Status.Value);

        if (query.Priority.HasValue)
            queryable = queryable.Where(f => f.Priority == query.Priority.Value);

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(f => f.DeviceId == query.DeviceId.Value);

        if (query.ReporterId.HasValue)
            queryable = queryable.Where(f => f.ReporterId == query.ReporterId.Value);

        if (query.AssignedTechnicianId.HasValue)
            queryable = queryable.Where(f => f.AssignedTechnicianId == query.AssignedTechnicianId.Value);

        if (query.StartDate.HasValue)
            queryable = queryable.Where(f => f.ReportTime >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            queryable = queryable.Where(f => f.ReportTime <= query.EndDate.Value);

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "reportcode" => query.SortDesc ? queryable.OrderByDescending(f => f.ReportCode) : queryable.OrderBy(f => f.ReportCode),
            "title" => query.SortDesc ? queryable.OrderByDescending(f => f.Title) : queryable.OrderBy(f => f.Title),
            "priority" => query.SortDesc ? queryable.OrderByDescending(f => f.Priority) : queryable.OrderBy(f => f.Priority),
            "status" => query.SortDesc ? queryable.OrderByDescending(f => f.Status) : queryable.OrderBy(f => f.Status),
            "reporttime" => query.SortDesc ? queryable.OrderByDescending(f => f.ReportTime) : queryable.OrderBy(f => f.ReportTime),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(f => f.CreatedAt) : queryable.OrderBy(f => f.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(f => f.Id) : queryable.OrderBy(f => f.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<FaultReportDto>>(items);
        return new PagedResult<FaultReportDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<FaultReportDto?> GetByIdAsync(int id)
    {
        var report = await _context.FaultReports
            .Include(f => f.Device)
            .Include(f => f.Reporter)
            .Include(f => f.AssignedTechnician)
            .FirstOrDefaultAsync(f => f.Id == id);
        return report == null ? null : _mapper.Map<FaultReportDto>(report);
    }

    public async Task<FaultReportDto> CreateAsync(CreateFaultReportDto dto, int reporterId)
    {
        if (await _context.FaultReports.AnyAsync(f => f.ReportCode == dto.ReportCode))
        {
            throw new InvalidOperationException("故障报修编号已存在");
        }

        var device = await _context.Devices.FindAsync(dto.DeviceId);
        if (device == null)
        {
            throw new KeyNotFoundException("设备不存在");
        }

        var report = _mapper.Map<FaultReport>(dto);
        report.ReporterId = reporterId;
        report.Status = FaultStatus.Pending;
        report.ReportTime = DateTime.UtcNow;
        report.CreatedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        if (device.Status == DeviceStatus.Running || device.Status == DeviceStatus.Standby)
        {
            device.Status = DeviceStatus.Fault;
            device.UpdatedAt = DateTime.UtcNow;
        }

        _context.FaultReports.Add(report);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(report.Id) ?? _mapper.Map<FaultReportDto>(report);
    }

    public async Task<FaultReportDto?> UpdateAsync(int id, UpdateFaultReportDto dto)
    {
        var report = await _context.FaultReports.FindAsync(id);
        if (report == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Title))
            report.Title = dto.Title;
        if (dto.DeviceId.HasValue)
            report.DeviceId = dto.DeviceId.Value;
        if (dto.Priority.HasValue)
            report.Priority = dto.Priority.Value;
        if (dto.Description != null)
            report.Description = dto.Description;
        if (dto.FaultLocation != null)
            report.FaultLocation = dto.FaultLocation;

        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var report = await _context.FaultReports.FindAsync(id);
        if (report == null) return false;

        _context.FaultReports.Remove(report);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FaultReportDto?> AssignAsync(int id, AssignFaultReportDto dto)
    {
        var report = await _context.FaultReports.FindAsync(id);
        if (report == null) return null;
        if (report.Status != FaultStatus.Pending)
            throw new InvalidOperationException("只有待派单的故障报修才能派单");

        var tech = await _context.Users.FindAsync(dto.TechnicianId);
        if (tech == null || tech.Role != UserRole.Technician || !tech.IsActive)
            throw new InvalidOperationException("指定的技术员不存在或角色不正确");

        report.AssignedTechnicianId = dto.TechnicianId;
        report.Status = FaultStatus.Assigned;
        report.AssignTime = DateTime.UtcNow;
        if (dto.Remark != null)
            report.Remark = dto.Remark;
        report.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<FaultReportDto?> StartAsync(int id)
    {
        var report = await _context.FaultReports.FindAsync(id);
        if (report == null) return null;
        if (report.Status != FaultStatus.Assigned)
            throw new InvalidOperationException("只有已派单的故障报修才能开始维修");

        report.Status = FaultStatus.InProgress;
        report.StartTime = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<FaultReportDto?> CompleteAsync(int id, CompleteFaultReportDto dto)
    {
        var report = await _context.FaultReports
            .Include(f => f.Device)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (report == null) return null;
        if (report.Status != FaultStatus.InProgress)
            throw new InvalidOperationException("只有维修中的故障报修才能完成");

        report.Status = FaultStatus.Completed;
        report.CompleteTime = DateTime.UtcNow;
        report.Solution = dto.Solution;
        if (dto.Remark != null)
            report.Remark = dto.Remark;
        report.UpdatedAt = DateTime.UtcNow;

        var device = report.Device;
        if (device != null && device.Status == DeviceStatus.Fault)
        {
            device.Status = DeviceStatus.Running;
            device.UpdatedAt = DateTime.UtcNow;
        }

        if (dto.SparePartConsumptions != null && dto.SparePartConsumptions.Any())
        {
            var sparePartIds = dto.SparePartConsumptions.Select(c => c.SparePartId).Distinct().ToList();
            var spareParts = await _context.SpareParts
                .Where(s => sparePartIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            foreach (var item in dto.SparePartConsumptions)
            {
                if (!spareParts.TryGetValue(item.SparePartId, out var sparePart))
                {
                    throw new KeyNotFoundException($"备件 ID {item.SparePartId} 不存在");
                }

                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException($"备件 {sparePart.Name} 的消耗数量必须大于0");
                }

                if (sparePart.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"备件 {sparePart.Name} 库存不足，当前库存：{sparePart.StockQuantity}，需要：{item.Quantity}");
                }

                if (report.DeviceId != sparePart.DeviceId)
                {
                    throw new InvalidOperationException($"备件 {sparePart.Name} 不属于当前故障设备");
                }

                sparePart.StockQuantity -= item.Quantity;
                sparePart.UpdatedAt = DateTime.UtcNow;

                var consumption = new SparePartConsumption
                {
                    SparePartId = item.SparePartId,
                    FaultReportId = id,
                    Quantity = item.Quantity,
                    ConsumedAt = DateTime.UtcNow,
                    Remark = item.Remark,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SparePartConsumptions.Add(consumption);
            }
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<FaultReportDto?> CancelAsync(int id)
    {
        var report = await _context.FaultReports.FindAsync(id);
        if (report == null) return null;
        if (report.Status == FaultStatus.Completed || report.Status == FaultStatus.Cancelled)
            throw new InvalidOperationException("已完成或已取消的故障报修不能取消");

        report.Status = FaultStatus.Cancelled;
        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<FaultStatisticsDto> GetStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var total = await _context.FaultReports.CountAsync();
        var pending = await _context.FaultReports.CountAsync(f => f.Status == FaultStatus.Pending);
        var assigned = await _context.FaultReports.CountAsync(f => f.Status == FaultStatus.Assigned);
        var inProgress = await _context.FaultReports.CountAsync(f => f.Status == FaultStatus.InProgress);
        var completed = await _context.FaultReports.CountAsync(f => f.Status == FaultStatus.Completed);
        var cancelled = await _context.FaultReports.CountAsync(f => f.Status == FaultStatus.Cancelled);

        var thisMonth = await _context.FaultReports.CountAsync(f => f.ReportTime >= monthStart);
        var thisMonthCompleted = await _context.FaultReports.CountAsync(f =>
            f.Status == FaultStatus.Completed && f.CompleteTime >= monthStart);

        var completedReports = await _context.FaultReports
            .Where(f => f.Status == FaultStatus.Completed && f.StartTime.HasValue && f.CompleteTime.HasValue)
            .ToListAsync();

        double? avgHours = null;
        if (completedReports.Any())
        {
            avgHours = completedReports.Average(f => (f.CompleteTime!.Value - f.StartTime!.Value).TotalHours);
            avgHours = Math.Round(avgHours.Value, 2);
        }

        return new FaultStatisticsDto
        {
            TotalCount = total,
            PendingCount = pending,
            AssignedCount = assigned,
            InProgressCount = inProgress,
            CompletedCount = completed,
            CancelledCount = cancelled,
            ThisMonthCount = thisMonth,
            ThisMonthCompletedCount = thisMonthCompleted,
            AverageResolutionHours = avgHours
        };
    }
}
