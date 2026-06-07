using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class DeviceBorrowService : IDeviceBorrowService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public DeviceBorrowService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<DeviceBorrowRecordDto>> GetPagedAsync(DeviceBorrowQueryDto query)
    {
        var queryable = _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Operator)
            .Include(r => r.ReturnOperator)
            .AsQueryable();

        if (query.DeviceId.HasValue)
        {
            queryable = queryable.Where(r => r.DeviceId == query.DeviceId.Value);
        }

        if (query.BorrowType.HasValue)
        {
            queryable = queryable.Where(r => r.BorrowType == query.BorrowType.Value);
        }

        if (query.IsReturned.HasValue)
        {
            queryable = queryable.Where(r => r.IsReturned == query.IsReturned.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.BorrowerName))
        {
            var keyword = query.BorrowerName.ToLower();
            queryable = queryable.Where(r => r.BorrowerName.ToLower().Contains(keyword));
        }

        if (query.BorrowTimeFrom.HasValue)
        {
            queryable = queryable.Where(r => r.BorrowTime >= query.BorrowTimeFrom.Value);
        }

        if (query.BorrowTimeTo.HasValue)
        {
            queryable = queryable.Where(r => r.BorrowTime <= query.BorrowTimeTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(r =>
                r.RecordCode.ToLower().Contains(keyword) ||
                r.BorrowerName.ToLower().Contains(keyword) ||
                (r.Device != null && r.Device.DeviceCode.ToLower().Contains(keyword)) ||
                (r.Device != null && r.Device.Name.ToLower().Contains(keyword)));
        }

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "recordcode" => query.SortDesc ? queryable.OrderByDescending(r => r.RecordCode) : queryable.OrderBy(r => r.RecordCode),
            "borrowtime" => query.SortDesc ? queryable.OrderByDescending(r => r.BorrowTime) : queryable.OrderBy(r => r.BorrowTime),
            "expectedreturntime" => query.SortDesc ? queryable.OrderByDescending(r => r.ExpectedReturnTime) : queryable.OrderBy(r => r.ExpectedReturnTime),
            "borrowertype" => query.SortDesc ? queryable.OrderByDescending(r => r.BorrowType) : queryable.OrderBy(r => r.BorrowType),
            "isreturned" => query.SortDesc ? queryable.OrderByDescending(r => r.IsReturned) : queryable.OrderBy(r => r.IsReturned),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(r => r.CreatedAt) : queryable.OrderBy(r => r.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(r => r.Id) : queryable.OrderBy(r => r.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<DeviceBorrowRecordDto>
        {
            Items = _mapper.Map<List<DeviceBorrowRecordDto>>(items),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<DeviceBorrowRecordDto?> GetByIdAsync(int id)
    {
        var record = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Operator)
            .Include(r => r.ReturnOperator)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null) return null;

        return _mapper.Map<DeviceBorrowRecordDto>(record);
    }

    public async Task<DeviceBorrowRecordDto> BorrowAsync(CreateDeviceBorrowDto dto, int operatorId)
    {
        var device = await _context.Devices.FindAsync(dto.DeviceId);
        if (device == null)
        {
            throw new InvalidOperationException("设备不存在");
        }

        if (device.Status == DeviceStatus.Scrapped)
        {
            throw new InvalidOperationException("设备已报废，无法借出");
        }

        if (device.Status == DeviceStatus.Borrowed)
        {
            throw new InvalidOperationException("设备当前已借出，无法重复借出");
        }

        var hasActiveBorrow = await _context.DeviceBorrowRecords
            .AnyAsync(r => r.DeviceId == dto.DeviceId && !r.IsReturned);
        if (hasActiveBorrow)
        {
            throw new InvalidOperationException("设备存在未归还的借出记录，无法再次借出");
        }

        if (dto.ExpectedReturnTime <= dto.BorrowTime)
        {
            throw new InvalidOperationException("预计归还时间必须晚于借出时间");
        }

        var recordCode = await GenerateRecordCodeAsync();

        var record = _mapper.Map<DeviceBorrowRecord>(dto);
        record.RecordCode = recordCode;
        record.OperatorId = operatorId;
        record.StatusBeforeBorrow = device.Status;
        record.IsReturned = false;
        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        _context.DeviceBorrowRecords.Add(record);

        device.Status = DeviceStatus.Borrowed;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var result = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Operator)
            .FirstOrDefaultAsync(r => r.Id == record.Id);

        return _mapper.Map<DeviceBorrowRecordDto>(result);
    }

    public async Task<DeviceBorrowRecordDto?> ReturnAsync(int id, ReturnDeviceBorrowDto dto, int operatorId)
    {
        var record = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null) return null;

        if (record.IsReturned)
        {
            throw new InvalidOperationException("该借出记录已归还，无需重复操作");
        }

        var device = record.Device;
        if (device == null)
        {
            throw new InvalidOperationException("关联设备不存在");
        }

        if (device.Status == DeviceStatus.Scrapped)
        {
            throw new InvalidOperationException("设备已被标记为报废，无法执行归还操作");
        }

        var hasActiveMaintenancePlans = await _context.MaintenancePlans
            .AnyAsync(p => p.DeviceId == device.Id && p.Status == MaintenancePlanStatus.InProgress);
        if (hasActiveMaintenancePlans)
        {
            throw new InvalidOperationException("设备有关联的正在执行中的保养计划，无法执行归还操作");
        }

        record.IsReturned = true;
        record.ActualReturnTime = DateTime.UtcNow;
        record.ReturnRemark = dto.ReturnRemark;
        record.ReturnOperatorId = operatorId;
        record.UpdatedAt = DateTime.UtcNow;

        device.Status = record.StatusBeforeBorrow;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var result = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Operator)
            .Include(r => r.ReturnOperator)
            .FirstOrDefaultAsync(r => r.Id == id);

        return _mapper.Map<DeviceBorrowRecordDto>(result);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _context.DeviceBorrowRecords.FindAsync(id);
        if (record == null) return false;

        if (!record.IsReturned)
        {
            var device = await _context.Devices.FindAsync(record.DeviceId);
            if (device != null && device.Status == DeviceStatus.Borrowed)
            {
                device.Status = record.StatusBeforeBorrow;
                device.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.DeviceBorrowRecords.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DeviceBorrowStatisticsDto> GetStatisticsAsync()
    {
        var total = await _context.DeviceBorrowRecords.CountAsync();
        var borrowing = await _context.DeviceBorrowRecords.CountAsync(r => !r.IsReturned);
        var returned = await _context.DeviceBorrowRecords.CountAsync(r => r.IsReturned);
        var external = await _context.DeviceBorrowRecords.CountAsync(r => r.BorrowType == BorrowType.External);
        var internalBorrow = await _context.DeviceBorrowRecords.CountAsync(r => r.BorrowType == BorrowType.Internal);
        var overdue = await _context.DeviceBorrowRecords.CountAsync(r => !r.IsReturned && r.ExpectedReturnTime < DateTime.UtcNow);

        return new DeviceBorrowStatisticsDto
        {
            TotalCount = total,
            BorrowingCount = borrowing,
            ReturnedCount = returned,
            ExternalBorrowCount = external,
            InternalBorrowCount = internalBorrow,
            OverdueCount = overdue
        };
    }

    public async Task<List<DeviceBorrowRecordDto>> GetDeviceBorrowRecordsAsync(int deviceId)
    {
        var records = await _context.DeviceBorrowRecords
            .Include(r => r.Operator)
            .Include(r => r.ReturnOperator)
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.BorrowTime)
            .ToListAsync();

        return _mapper.Map<List<DeviceBorrowRecordDto>>(records);
    }

    private async Task<string> GenerateRecordCodeAsync()
    {
        var datePart = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"BR{datePart}";
        var lastRecord = await _context.DeviceBorrowRecords
            .Where(r => r.RecordCode.StartsWith(prefix))
            .OrderByDescending(r => r.RecordCode)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastRecord != null)
        {
            var seqPart = lastRecord.RecordCode.Substring(prefix.Length);
            if (int.TryParse(seqPart, out var seq))
            {
                sequence = seq + 1;
            }
        }

        return $"{prefix}{sequence:D4}";
    }
}
