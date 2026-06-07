using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class MaintenanceContractService : IMaintenanceContractService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public MaintenanceContractService(
        AppDbContext context,
        IMapper mapper,
        INotificationService notificationService)
    {
        _context = context;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<MaintenanceContractDto>> GetPagedAsync(MaintenanceContractQueryDto query)
    {
        var queryable = _context.MaintenanceContracts
            .Include(c => c.Device)
            .Include(c => c.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(c =>
                c.ContractCode.ToLower().Contains(keyword) ||
                c.ContractName.ToLower().Contains(keyword) ||
                c.ServiceDescription.ToLower().Contains(keyword));
        }

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(c => c.DeviceId == query.DeviceId.Value);

        if (query.SupplierId.HasValue)
            queryable = queryable.Where(c => c.SupplierId == query.SupplierId.Value);

        if (query.StartDateFrom.HasValue)
            queryable = queryable.Where(c => c.StartDate >= query.StartDateFrom.Value);

        if (query.StartDateTo.HasValue)
            queryable = queryable.Where(c => c.StartDate <= query.StartDateTo.Value);

        if (query.EndDateFrom.HasValue)
            queryable = queryable.Where(c => c.EndDate >= query.EndDateFrom.Value);

        if (query.EndDateTo.HasValue)
            queryable = queryable.Where(c => c.EndDate <= query.EndDateTo.Value);

        var now = DateTime.UtcNow;
        if (query.Status.HasValue)
        {
            queryable = query.Status.Value switch
            {
                ContractStatus.Active => queryable.Where(c => c.EndDate > now),
                ContractStatus.ExpiringSoon => queryable.Where(c => c.EndDate > now && c.EndDate <= now.AddDays(30)),
                ContractStatus.Expired => queryable.Where(c => c.EndDate <= now),
                ContractStatus.Cancelled => queryable.Where(c => false),
                _ => queryable
            };
        }

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "contractcode" => query.SortDesc ? queryable.OrderByDescending(c => c.ContractCode) : queryable.OrderBy(c => c.ContractCode),
            "contractname" => query.SortDesc ? queryable.OrderByDescending(c => c.ContractName) : queryable.OrderBy(c => c.ContractName),
            "startdate" => query.SortDesc ? queryable.OrderByDescending(c => c.StartDate) : queryable.OrderBy(c => c.StartDate),
            "enddate" => query.SortDesc ? queryable.OrderByDescending(c => c.EndDate) : queryable.OrderBy(c => c.EndDate),
            "amount" => query.SortDesc ? queryable.OrderByDescending(c => c.Amount) : queryable.OrderBy(c => c.Amount),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(c => c.CreatedAt) : queryable.OrderBy(c => c.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(c => c.Id) : queryable.OrderBy(c => c.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<MaintenanceContractDto>>(items);
        foreach (var dto in dtos)
        {
            dto.Status = CalculateContractStatus(dto.EndDate, now);
        }

        return new PagedResult<MaintenanceContractDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<MaintenanceContractDetailDto?> GetByIdAsync(int id)
    {
        var contract = await _context.MaintenanceContracts
            .Include(c => c.Device)
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null) return null;

        var dto = _mapper.Map<MaintenanceContractDetailDto>(contract);
        dto.Status = CalculateContractStatus(dto.EndDate, DateTime.UtcNow);
        return dto;
    }

    public async Task<MaintenanceContractDto> CreateAsync(CreateMaintenanceContractDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ContractCode))
        {
            throw new InvalidOperationException("合同编号不能为空");
        }

        if (string.IsNullOrWhiteSpace(dto.ContractName))
        {
            throw new InvalidOperationException("合同名称不能为空");
        }

        if (dto.Amount < 0)
        {
            throw new InvalidOperationException("合同金额不能为负数");
        }

        if (await _context.MaintenanceContracts.AnyAsync(c => c.ContractCode == dto.ContractCode))
        {
            throw new InvalidOperationException("合同编号已存在");
        }

        var device = await _context.Devices.FindAsync(dto.DeviceId);
        if (device == null)
        {
            throw new KeyNotFoundException("设备不存在");
        }

        if (dto.SupplierId.HasValue)
        {
            var supplier = await _context.Suppliers.FindAsync(dto.SupplierId.Value);
            if (supplier == null)
            {
                throw new KeyNotFoundException("供应商不存在");
            }
        }

        if (dto.EndDate <= dto.StartDate)
        {
            throw new InvalidOperationException("结束日期必须晚于开始日期");
        }

        var contract = _mapper.Map<MaintenanceContract>(dto);
        contract.CreatedAt = DateTime.UtcNow;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.ReminderSent = false;

        _context.MaintenanceContracts.Add(contract);
        await _context.SaveChangesAsync();

        var result = _mapper.Map<MaintenanceContractDto>(contract);
        result.Status = CalculateContractStatus(result.EndDate, DateTime.UtcNow);
        return result;
    }

    public async Task<MaintenanceContractDto?> UpdateAsync(int id, UpdateMaintenanceContractDto dto)
    {
        var contract = await _context.MaintenanceContracts.FindAsync(id);
        if (contract == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.ContractName))
            contract.ContractName = dto.ContractName;

        if (dto.Amount.HasValue)
        {
            if (dto.Amount.Value < 0)
            {
                throw new InvalidOperationException("合同金额不能为负数");
            }
            contract.Amount = dto.Amount.Value;
        }

        if (dto.StartDate.HasValue || dto.EndDate.HasValue)
        {
            var newStartDate = dto.StartDate ?? contract.StartDate;
            var newEndDate = dto.EndDate ?? contract.EndDate;

            if (newEndDate <= newStartDate)
            {
                throw new InvalidOperationException("结束日期必须晚于开始日期");
            }

            if (dto.StartDate.HasValue)
                contract.StartDate = dto.StartDate.Value;

            if (dto.EndDate.HasValue)
            {
                contract.EndDate = dto.EndDate.Value;
                contract.ReminderSent = false;
                contract.ReminderSentAt = null;
            }
        }

        if (dto.ServiceDescription != null)
            contract.ServiceDescription = dto.ServiceDescription;

        if (dto.SupplierId.HasValue)
        {
            var supplier = await _context.Suppliers.FindAsync(dto.SupplierId.Value);
            if (supplier == null)
            {
                throw new KeyNotFoundException("供应商不存在");
            }
            contract.SupplierId = dto.SupplierId.Value;
        }

        if (dto.Remarks != null)
            contract.Remarks = dto.Remarks;

        contract.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var result = _mapper.Map<MaintenanceContractDto>(contract);
        result.Status = CalculateContractStatus(result.EndDate, DateTime.UtcNow);
        return result;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var contract = await _context.MaintenanceContracts.FindAsync(id);
        if (contract == null) return false;

        _context.MaintenanceContracts.Remove(contract);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MaintenanceContractStatisticsDto> GetStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var expiringDate = now.AddDays(30);

        var total = await _context.MaintenanceContracts.CountAsync();
        var active = await _context.MaintenanceContracts.CountAsync(c => c.EndDate > now);
        var expiringSoon = await _context.MaintenanceContracts.CountAsync(c => c.EndDate > now && c.EndDate <= expiringDate);
        var expired = await _context.MaintenanceContracts.CountAsync(c => c.EndDate <= now);
        var totalAmount = await _context.MaintenanceContracts.SumAsync(c => (decimal?)c.Amount) ?? 0;

        var expiredByDevice = await _context.MaintenanceContracts
            .Include(c => c.Device)
            .Where(c => c.EndDate <= now)
            .GroupBy(c => new { c.DeviceId, c.Device!.Name, c.Device.DeviceCode })
            .Select(g => new ContractDeviceStatDto
            {
                DeviceId = g.Key.DeviceId,
                DeviceName = g.Key.Name ?? string.Empty,
                DeviceCode = g.Key.DeviceCode ?? string.Empty,
                ContractCount = g.Count()
            })
            .OrderByDescending(s => s.ContractCount)
            .Take(10)
            .ToListAsync();

        var sixMonthsLater = now.AddMonths(6);
        var expiryByMonth = await _context.MaintenanceContracts
            .Where(c => c.EndDate >= now && c.EndDate <= sixMonthsLater)
            .GroupBy(c => new { c.EndDate.Year, c.EndDate.Month })
            .Select(g => new ContractExpiryStatDto
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                ExpiringCount = g.Count(),
                ExpiredCount = 0
            })
            .OrderBy(s => s.Month)
            .ToListAsync();

        return new MaintenanceContractStatisticsDto
        {
            TotalCount = total,
            ActiveCount = active,
            ExpiringSoonCount = expiringSoon,
            ExpiredCount = expired,
            CancelledCount = 0,
            TotalAmount = totalAmount,
            ExpiredByDevice = expiredByDevice,
            ExpiryByMonth = expiryByMonth
        };
    }

    public async Task<List<MaintenanceContractDto>> GetDeviceContractsAsync(int deviceId)
    {
        var contracts = await _context.MaintenanceContracts
            .Include(c => c.Supplier)
            .Where(c => c.DeviceId == deviceId)
            .OrderByDescending(c => c.EndDate)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var dtos = _mapper.Map<List<MaintenanceContractDto>>(contracts);
        foreach (var dto in dtos)
        {
            dto.Status = CalculateContractStatus(dto.EndDate, now);
        }
        return dtos;
    }

    public async Task<int> SendExpiringRemindersAsync(int daysAhead = 30)
    {
        var now = DateTime.UtcNow;
        var reminderDate = now.AddDays(daysAhead);

        var expiringContracts = await _context.MaintenanceContracts
            .Include(c => c.Device)
            .Include(c => c.Supplier)
            .Where(c => c.EndDate > now
                && c.EndDate <= reminderDate
                && !c.ReminderSent)
            .ToListAsync();

        var expiredContracts = await _context.MaintenanceContracts
            .Include(c => c.Device)
            .Include(c => c.Supplier)
            .Where(c => c.EndDate <= now
                && !c.ReminderSent)
            .ToListAsync();

        var adminUsers = await _context.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .ToListAsync();

        var count = 0;

        foreach (var contract in expiringContracts)
        {
            var daysLeft = (int)(contract.EndDate - now).TotalDays;
            foreach (var admin in adminUsers)
            {
                _ = _notificationService.EnqueueAsync(new CreateNotificationDto
                {
                    UserId = admin.Id,
                    Title = $"维保合同即将到期: {contract.ContractCode}",
                    Content = $"维保合同 {contract.ContractName}（{contract.ContractCode}）将于 {daysLeft} 天后到期，请及时续签。设备：{contract.Device?.Name ?? contract.DeviceId.ToString()}。",
                    Type = NotificationType.ContractExpiringSoon,
                    Priority = daysLeft <= 7 ? NotificationPriority.Urgent : daysLeft <= 15 ? NotificationPriority.High : NotificationPriority.Medium,
                    RelatedEntityType = RelatedEntityType.MaintenanceContract,
                    RelatedEntityId = contract.Id
                });
            }

            contract.ReminderSent = true;
            contract.ReminderSentAt = now;
            count++;
        }

        foreach (var contract in expiredContracts)
        {
            var daysExpired = (int)(now - contract.EndDate).TotalDays;
            foreach (var admin in adminUsers)
            {
                _ = _notificationService.EnqueueAsync(new CreateNotificationDto
                {
                    UserId = admin.Id,
                    Title = $"维保合同已过期: {contract.ContractCode}",
                    Content = $"维保合同 {contract.ContractName}（{contract.ContractCode}）已过期 {daysExpired} 天，请尽快处理续签事宜。设备：{contract.Device?.Name ?? contract.DeviceId.ToString()}。",
                    Type = NotificationType.ContractExpired,
                    Priority = NotificationPriority.Urgent,
                    RelatedEntityType = RelatedEntityType.MaintenanceContract,
                    RelatedEntityId = contract.Id
                });
            }

            contract.ReminderSent = true;
            contract.ReminderSentAt = now;
            count++;
        }

        if (count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return count;
    }

    private static ContractStatus CalculateContractStatus(DateTime endDate, DateTime now)
    {
        if (endDate <= now)
            return ContractStatus.Expired;

        if (endDate <= now.AddDays(30))
            return ContractStatus.ExpiringSoon;

        return ContractStatus.Active;
    }
}
