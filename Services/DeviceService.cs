using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class DeviceService : IDeviceService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public DeviceService(AppDbContext context, IMapper mapper, INotificationService notificationService)
    {
        _context = context;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<DeviceDto>> GetPagedAsync(DeviceQueryDto query)
    {
        var queryable = _context.Devices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(d =>
                d.DeviceCode.ToLower().Contains(keyword) ||
                d.Name.ToLower().Contains(keyword) ||
                d.Model.ToLower().Contains(keyword) ||
                d.Manufacturer.ToLower().Contains(keyword) ||
                d.Location.ToLower().Contains(keyword));
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(d => d.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            queryable = queryable.Where(d => d.Category == query.Category);
        }

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "devicecode" => query.SortDesc ? queryable.OrderByDescending(d => d.DeviceCode) : queryable.OrderBy(d => d.DeviceCode),
            "name" => query.SortDesc ? queryable.OrderByDescending(d => d.Name) : queryable.OrderBy(d => d.Name),
            "category" => query.SortDesc ? queryable.OrderByDescending(d => d.Category) : queryable.OrderBy(d => d.Category),
            "status" => query.SortDesc ? queryable.OrderByDescending(d => d.Status) : queryable.OrderBy(d => d.Status),
            "purchasedate" => query.SortDesc ? queryable.OrderByDescending(d => d.PurchaseDate) : queryable.OrderBy(d => d.PurchaseDate),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(d => d.CreatedAt) : queryable.OrderBy(d => d.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(d => d.Id) : queryable.OrderBy(d => d.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<DeviceDto>
        {
            Items = _mapper.Map<List<DeviceDto>>(items),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<DeviceDetailDto?> GetByIdAsync(int id)
    {
        var device = await _context.Devices
            .Include(d => d.InspectionRecords)
                .ThenInclude(r => r.Photos)
            .Include(d => d.InspectionRecords)
                .ThenInclude(r => r.Inspector)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null) return null;

        var detail = _mapper.Map<DeviceDetailDto>(device);
        detail.InspectionRecordCount = device.InspectionRecords.Count;
        detail.RecentInspectionRecords = device.InspectionRecords
            .OrderByDescending(r => r.InspectionTime)
            .Take(10)
            .Select(r => _mapper.Map<InspectionRecordDto>(r))
            .ToList();

        return detail;
    }

    public async Task<DeviceDto> CreateAsync(CreateDeviceDto dto)
    {
        if (await _context.Devices.AnyAsync(d => d.DeviceCode == dto.DeviceCode))
        {
            throw new InvalidOperationException("设备编号已存在");
        }

        var device = _mapper.Map<Device>(dto);
        device.CreatedAt = DateTime.UtcNow;
        device.UpdatedAt = DateTime.UtcNow;

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        return _mapper.Map<DeviceDto>(device);
    }

    public async Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceDto dto)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device == null) return null;

        var oldStatus = device.Status;

        if (!string.IsNullOrWhiteSpace(dto.Name))
            device.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Category))
            device.Category = dto.Category;
        if (!string.IsNullOrWhiteSpace(dto.Model))
            device.Model = dto.Model;
        if (!string.IsNullOrWhiteSpace(dto.Manufacturer))
            device.Manufacturer = dto.Manufacturer;
        if (dto.PurchaseDate.HasValue)
            device.PurchaseDate = dto.PurchaseDate.Value;
        if (dto.PurchasePrice.HasValue)
            device.PurchasePrice = dto.PurchasePrice.Value;
        if (!string.IsNullOrWhiteSpace(dto.Location))
            device.Location = dto.Location;
        if (dto.Status.HasValue)
            device.Status = dto.Status.Value;
        if (dto.Description != null)
            device.Description = dto.Description;

        device.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (dto.Status.HasValue && oldStatus != dto.Status.Value)
        {
            var adminUserIds = await _context.Users
                .Where(u => u.Role == UserRole.Admin && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            if (adminUserIds.Count > 0)
            {
                var priority = dto.Status.Value == DeviceStatus.Fault ? NotificationPriority.High : NotificationPriority.Medium;
                _ = _notificationService.BatchEnqueueAsync(new BatchCreateNotificationDto
                {
                    UserIds = adminUserIds,
                    Title = $"设备状态变更: {device.DeviceCode}",
                    Content = $"设备 {device.Name}（{device.DeviceCode}）状态已从 {oldStatus} 变更为 {dto.Status.Value}，请关注。",
                    Type = NotificationType.DeviceStatusChanged,
                    Priority = priority,
                    RelatedEntityType = RelatedEntityType.Device,
                    RelatedEntityId = device.Id
                });
            }
        }

        return _mapper.Map<DeviceDto>(device);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device == null) return false;

        var hasRelatedPlans = await _context.MaintenancePlans.AnyAsync(p => p.DeviceId == id);
        var hasRelatedFaults = await _context.FaultReports.AnyAsync(f => f.DeviceId == id);
        var hasRelatedInspectionPlans = await _context.InspectionPlans.AnyAsync(p => p.DeviceId == id);
        var hasRelatedInspectionTasks = await _context.InspectionTasks.AnyAsync(t => t.DeviceId == id);
        var hasRelatedInspectionRecords = await _context.InspectionRecords.AnyAsync(r => r.DeviceId == id);
        if (hasRelatedPlans || hasRelatedFaults || hasRelatedInspectionPlans || hasRelatedInspectionTasks || hasRelatedInspectionRecords)
        {
            throw new InvalidOperationException("该设备有关联的保养计划、故障报修或巡检记录，无法删除");
        }

        _context.Devices.Remove(device);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DeviceStatisticsDto> GetStatisticsAsync()
    {
        var total = await _context.Devices.CountAsync();
        var running = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Running);
        var standby = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Standby);
        var maintenance = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Maintenance);
        var fault = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Fault);
        var scrapped = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Scrapped);

        var byCategory = await _context.Devices
            .GroupBy(d => d.Category)
            .Select(g => new CategoryStatDto
            {
                Category = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return new DeviceStatisticsDto
        {
            TotalCount = total,
            RunningCount = running,
            StandbyCount = standby,
            MaintenanceCount = maintenance,
            FaultCount = fault,
            ScrappedCount = scrapped,
            ByCategory = byCategory
        };
    }
}
