using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public SupplierService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<SupplierDto>> GetPagedAsync(SupplierQueryDto query)
    {
        var queryable = _context.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(s =>
                s.Name.ToLower().Contains(keyword) ||
                s.ContactPerson.ToLower().Contains(keyword) ||
                s.ContactPhone.ToLower().Contains(keyword) ||
                s.Address.ToLower().Contains(keyword) ||
                s.Description != null && s.Description.ToLower().Contains(keyword));
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(s => s.Status == query.Status.Value);
        }

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "name" => query.SortDesc ? queryable.OrderByDescending(s => s.Name) : queryable.OrderBy(s => s.Name),
            "contactperson" => query.SortDesc ? queryable.OrderByDescending(s => s.ContactPerson) : queryable.OrderBy(s => s.ContactPerson),
            "contactphone" => query.SortDesc ? queryable.OrderByDescending(s => s.ContactPhone) : queryable.OrderBy(s => s.ContactPhone),
            "status" => query.SortDesc ? queryable.OrderByDescending(s => s.Status) : queryable.OrderBy(s => s.Status),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(s => s.CreatedAt) : queryable.OrderBy(s => s.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(s => s.Id) : queryable.OrderBy(s => s.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new { Supplier = s, DeviceCount = s.Devices.Count })
            .ToListAsync();

        var dtos = items.Select(x =>
        {
            var dto = _mapper.Map<SupplierDto>(x.Supplier);
            dto.DeviceCount = x.DeviceCount;
            return dto;
        }).ToList();

        return new PagedResult<SupplierDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        var items = await _context.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new { Supplier = s, DeviceCount = s.Devices.Count })
            .ToListAsync();

        var dtos = items.Select(x =>
        {
            var dto = _mapper.Map<SupplierDto>(x.Supplier);
            dto.DeviceCount = x.DeviceCount;
            return dto;
        }).ToList();

        return dtos;
    }

    public async Task<SupplierDetailDto?> GetByIdAsync(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Devices)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null) return null;

        var detail = _mapper.Map<SupplierDetailDto>(supplier);
        detail.DeviceCount = supplier.Devices.Count;
        detail.Devices = supplier.Devices
            .Select(d => _mapper.Map<SupplierDeviceDto>(d))
            .OrderBy(d => d.DeviceCode)
            .ToList();

        return detail;
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("供应商名称不能为空");
        }

        var trimmedName = dto.Name.Trim();
        if (await _context.Suppliers.AnyAsync(s => s.Name == trimmedName))
        {
            throw new InvalidOperationException("供应商名称已存在");
        }

        if (string.IsNullOrWhiteSpace(dto.ContactPerson))
        {
            throw new InvalidOperationException("联系人不能为空");
        }

        if (string.IsNullOrWhiteSpace(dto.ContactPhone))
        {
            throw new InvalidOperationException("联系电话不能为空");
        }

        var supplier = _mapper.Map<Supplier>(dto);
        supplier.Name = trimmedName;
        supplier.ContactPerson = dto.ContactPerson.Trim();
        supplier.ContactPhone = dto.ContactPhone.Trim();
        supplier.Address = dto.Address?.Trim() ?? string.Empty;
        supplier.CreatedAt = DateTime.UtcNow;
        supplier.UpdatedAt = DateTime.UtcNow;

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var result = _mapper.Map<SupplierDto>(supplier);
        result.DeviceCount = 0;
        return result;
    }

    public async Task<SupplierDto?> UpdateAsync(int id, UpdateSupplierDto dto)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return null;

        if (dto.Name != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new InvalidOperationException("供应商名称不能为空");
            }
            var trimmedName = dto.Name.Trim();
            if (await _context.Suppliers.AnyAsync(s => s.Name == trimmedName && s.Id != id))
            {
                throw new InvalidOperationException("供应商名称已存在");
            }
            supplier.Name = trimmedName;
        }

        if (dto.ContactPerson != null)
        {
            if (string.IsNullOrWhiteSpace(dto.ContactPerson))
            {
                throw new InvalidOperationException("联系人不能为空");
            }
            supplier.ContactPerson = dto.ContactPerson.Trim();
        }

        if (dto.ContactPhone != null)
        {
            if (string.IsNullOrWhiteSpace(dto.ContactPhone))
            {
                throw new InvalidOperationException("联系电话不能为空");
            }
            supplier.ContactPhone = dto.ContactPhone.Trim();
        }

        if (dto.Address != null)
            supplier.Address = dto.Address.Trim();

        if (dto.Status.HasValue)
            supplier.Status = dto.Status.Value;

        if (dto.Description != null)
            supplier.Description = dto.Description;

        supplier.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var deviceCount = await _context.Devices.CountAsync(d => d.SupplierId == id);
        var result = _mapper.Map<SupplierDto>(supplier);
        result.DeviceCount = deviceCount;
        return result;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return false;

        var hasDevices = await _context.Devices.AnyAsync(d => d.SupplierId == id);
        if (hasDevices)
        {
            throw new InvalidOperationException("该供应商存在关联设备，无法删除");
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SupplierStatisticsDto> GetStatisticsAsync()
    {
        var total = await _context.Suppliers.CountAsync();
        var active = await _context.Suppliers.CountAsync(s => s.Status == CooperationStatus.Active);
        var suspended = await _context.Suppliers.CountAsync(s => s.Status == CooperationStatus.Suspended);
        var terminated = await _context.Suppliers.CountAsync(s => s.Status == CooperationStatus.Terminated);

        var deviceStats = await _context.Suppliers
            .Select(s => new SupplierDeviceStatDto
            {
                SupplierId = s.Id,
                SupplierName = s.Name,
                TotalDevices = s.Devices.Count,
                RunningCount = s.Devices.Count(d => d.Status == DeviceStatus.Running),
                StandbyCount = s.Devices.Count(d => d.Status == DeviceStatus.Standby),
                MaintenanceCount = s.Devices.Count(d => d.Status == DeviceStatus.Maintenance),
                FaultCount = s.Devices.Count(d => d.Status == DeviceStatus.Fault),
                ScrappedCount = s.Devices.Count(d => d.Status == DeviceStatus.Scrapped),
                FaultReportCount = s.Devices.Sum(d => d.FaultReports.Count),
                FaultRate = s.Devices.Count > 0
                    ? Math.Round((decimal)s.Devices.Count(d => d.Status == DeviceStatus.Fault) / s.Devices.Count * 100, 2)
                    : 0
            })
            .OrderByDescending(s => s.TotalDevices)
            .ToListAsync();

        return new SupplierStatisticsDto
        {
            TotalCount = total,
            ActiveCount = active,
            SuspendedCount = suspended,
            TerminatedCount = terminated,
            DeviceStats = deviceStats
        };
    }
}
