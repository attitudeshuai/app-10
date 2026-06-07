using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class SparePartService : ISparePartService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public SparePartService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<SparePartDto>> GetPagedAsync(SparePartQueryDto query)
    {
        var queryable = _context.SpareParts
            .Include(s => s.Device)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(s =>
                s.Name.ToLower().Contains(keyword) ||
                s.Specification.ToLower().Contains(keyword) ||
                s.Description != null && s.Description.ToLower().Contains(keyword));
        }

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(s => s.DeviceId == query.DeviceId.Value);

        if (query.LowStockOnly == true)
            queryable = queryable.Where(s => s.StockQuantity <= s.MinStockWarning);

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "name" => query.SortDesc ? queryable.OrderByDescending(s => s.Name) : queryable.OrderBy(s => s.Name),
            "specification" => query.SortDesc ? queryable.OrderByDescending(s => s.Specification) : queryable.OrderBy(s => s.Specification),
            "stockquantity" => query.SortDesc ? queryable.OrderByDescending(s => s.StockQuantity) : queryable.OrderBy(s => s.StockQuantity),
            "minstockwarning" => query.SortDesc ? queryable.OrderByDescending(s => s.MinStockWarning) : queryable.OrderBy(s => s.MinStockWarning),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(s => s.CreatedAt) : queryable.OrderBy(s => s.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(s => s.Id) : queryable.OrderBy(s => s.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<SparePartDto>>(items);
        return new PagedResult<SparePartDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<SparePartDto?> GetByIdAsync(int id)
    {
        var sparePart = await _context.SpareParts
            .Include(s => s.Device)
            .FirstOrDefaultAsync(s => s.Id == id);
        return sparePart == null ? null : _mapper.Map<SparePartDto>(sparePart);
    }

    public async Task<SparePartDto> CreateAsync(CreateSparePartDto dto)
    {
        var device = await _context.Devices.FindAsync(dto.DeviceId);
        if (device == null)
        {
            throw new KeyNotFoundException("设备不存在");
        }

        if (await _context.SpareParts.AnyAsync(s =>
            s.DeviceId == dto.DeviceId &&
            s.Name == dto.Name &&
            s.Specification == dto.Specification))
        {
            throw new InvalidOperationException("该设备下已存在同名同规格的备件");
        }

        if (dto.StockQuantity < 0)
        {
            throw new InvalidOperationException("库存数量不能为负数");
        }

        if (dto.MinStockWarning < 0)
        {
            throw new InvalidOperationException("最低库存预警线不能为负数");
        }

        var sparePart = _mapper.Map<SparePart>(dto);
        sparePart.CreatedAt = DateTime.UtcNow;
        sparePart.UpdatedAt = DateTime.UtcNow;

        _context.SpareParts.Add(sparePart);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(sparePart.Id) ?? _mapper.Map<SparePartDto>(sparePart);
    }

    public async Task<SparePartDto?> UpdateAsync(int id, UpdateSparePartDto dto)
    {
        var sparePart = await _context.SpareParts.FindAsync(id);
        if (sparePart == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Name))
            sparePart.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Specification))
            sparePart.Specification = dto.Specification;
        if (dto.StockQuantity.HasValue)
        {
            if (dto.StockQuantity.Value < 0)
                throw new InvalidOperationException("库存数量不能为负数");
            sparePart.StockQuantity = dto.StockQuantity.Value;
        }
        if (dto.MinStockWarning.HasValue)
        {
            if (dto.MinStockWarning.Value < 0)
                throw new InvalidOperationException("最低库存预警线不能为负数");
            sparePart.MinStockWarning = dto.MinStockWarning.Value;
        }
        if (dto.Description != null)
            sparePart.Description = dto.Description;

        sparePart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sparePart = await _context.SpareParts.FindAsync(id);
        if (sparePart == null) return false;

        var hasConsumptions = await _context.SparePartConsumptions.AnyAsync(c => c.SparePartId == id);
        if (hasConsumptions)
        {
            throw new InvalidOperationException("该备件存在消耗记录，无法删除");
        }

        _context.SpareParts.Remove(sparePart);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SparePartStatisticsDto> GetStatisticsAsync()
    {
        var total = await _context.SpareParts.CountAsync();
        var lowStock = await _context.SpareParts.CountAsync(s => s.StockQuantity <= s.MinStockWarning);
        var totalStock = await _context.SpareParts.SumAsync(s => s.StockQuantity);

        var lowStockItems = await _context.SpareParts
            .Include(s => s.Device)
            .Where(s => s.StockQuantity <= s.MinStockWarning)
            .OrderBy(s => s.StockQuantity)
            .Take(20)
            .ToListAsync();

        decimal lowStockRatio = total > 0 ? Math.Round((decimal)lowStock / total * 100, 2) : 0;

        return new SparePartStatisticsDto
        {
            TotalCount = total,
            LowStockCount = lowStock,
            LowStockRatio = lowStockRatio,
            TotalStockQuantity = totalStock,
            LowStockItems = _mapper.Map<List<SparePartDto>>(lowStockItems)
        };
    }

    public async Task<PagedResult<SparePartConsumptionDto>> GetConsumptionsAsync(SparePartConsumptionQueryDto query)
    {
        var queryable = _context.SparePartConsumptions
            .Include(c => c.SparePart)
            .Include(c => c.FaultReport)
            .AsQueryable();

        if (query.SparePartId.HasValue)
            queryable = queryable.Where(c => c.SparePartId == query.SparePartId.Value);

        if (query.FaultReportId.HasValue)
            queryable = queryable.Where(c => c.FaultReportId == query.FaultReportId.Value);

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(c => c.SparePart != null && c.SparePart.DeviceId == query.DeviceId.Value);

        if (query.StartDate.HasValue)
            queryable = queryable.Where(c => c.ConsumedAt >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            queryable = queryable.Where(c => c.ConsumedAt <= query.EndDate.Value);

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "consumedat" => query.SortDesc ? queryable.OrderByDescending(c => c.ConsumedAt) : queryable.OrderBy(c => c.ConsumedAt),
            "quantity" => query.SortDesc ? queryable.OrderByDescending(c => c.Quantity) : queryable.OrderBy(c => c.Quantity),
            _ => query.SortDesc ? queryable.OrderByDescending(c => c.Id) : queryable.OrderBy(c => c.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<SparePartConsumptionDto>>(items);
        return new PagedResult<SparePartConsumptionDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }
}
