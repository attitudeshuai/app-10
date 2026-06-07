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
    private readonly INotificationService _notificationService;

    public DeviceBorrowService(
        AppDbContext context,
        IMapper mapper,
        INotificationService notificationService)
    {
        _context = context;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<DeviceBorrowRecordDto>> GetPagedAsync(DeviceBorrowQueryDto query)
    {
        var queryable = _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Operator)
            .Include(r => r.ReturnOperator)
            .Include(r => r.Approver)
            .Include(r => r.Applicant)
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

        if (query.ApprovalStatus.HasValue)
        {
            queryable = queryable.Where(r => r.ApprovalStatus == query.ApprovalStatus.Value);
        }

        if (query.ApplicantId.HasValue)
        {
            queryable = queryable.Where(r => r.ApplicantId == query.ApplicantId.Value);
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
            "approvalstatus" => query.SortDesc ? queryable.OrderByDescending(r => r.ApprovalStatus) : queryable.OrderBy(r => r.ApprovalStatus),
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
            .Include(r => r.Approver)
            .Include(r => r.Applicant)
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
            .AnyAsync(r => r.DeviceId == dto.DeviceId && !r.IsReturned && r.ApprovalStatus == BorrowApprovalStatus.Approved);
        if (hasActiveBorrow)
        {
            throw new InvalidOperationException("设备存在未归还的借出记录，无法再次借出");
        }

        var hasPendingApproval = await _context.DeviceBorrowRecords
            .AnyAsync(r => r.DeviceId == dto.DeviceId && r.ApprovalStatus == BorrowApprovalStatus.Pending);
        if (hasPendingApproval)
        {
            throw new InvalidOperationException("设备存在待审批的借出申请，请等待审批结果");
        }

        if (dto.ExpectedReturnTime <= dto.BorrowTime)
        {
            throw new InvalidOperationException("预计归还时间必须晚于借出时间");
        }

        var recordCode = await GenerateRecordCodeAsync();

        var record = _mapper.Map<DeviceBorrowRecord>(dto);
        record.RecordCode = recordCode;
        record.OperatorId = operatorId;
        record.ApplicantId = operatorId;
        record.StatusBeforeBorrow = device.Status;
        record.IsReturned = false;
        record.ApprovalStatus = BorrowApprovalStatus.Pending;
        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        _context.DeviceBorrowRecords.Add(record);
        await _context.SaveChangesAsync();

        await NotifyAdminsBorrowRequestSubmitted(record, device);

        var result = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Operator)
            .Include(r => r.Applicant)
            .FirstOrDefaultAsync(r => r.Id == record.Id);

        return _mapper.Map<DeviceBorrowRecordDto>(result);
    }

    public async Task<DeviceBorrowRecordDto?> ApproveAsync(int id, ApproveBorrowDto dto, int approverId)
    {
        var record = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Applicant)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null) return null;

        if (record.ApprovalStatus != BorrowApprovalStatus.Pending)
        {
            throw new InvalidOperationException("该申请已审批，无法重复操作");
        }

        var device = record.Device;
        if (device == null)
        {
            throw new InvalidOperationException("关联设备不存在");
        }

        if (device.Status == DeviceStatus.Scrapped)
        {
            throw new InvalidOperationException("设备已报废，无法审批通过");
        }

        if (device.Status == DeviceStatus.Borrowed)
        {
            throw new InvalidOperationException("设备当前已借出，无法审批通过");
        }

        var hasActiveBorrow = await _context.DeviceBorrowRecords
            .AnyAsync(r => r.DeviceId == device.Id && !r.IsReturned && r.ApprovalStatus == BorrowApprovalStatus.Approved && r.Id != id);
        if (hasActiveBorrow)
        {
            throw new InvalidOperationException("设备存在其他未归还的借出记录，无法审批通过");
        }

        record.ApprovalStatus = BorrowApprovalStatus.Approved;
        record.ApproverId = approverId;
        record.ApprovalTime = DateTime.UtcNow;
        record.ApprovalRemark = dto.ApprovalRemark;
        record.UpdatedAt = DateTime.UtcNow;

        device.Status = DeviceStatus.Borrowed;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await NotifyApplicantBorrowApproved(record, device);

        var result = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Operator)
            .Include(r => r.Approver)
            .Include(r => r.Applicant)
            .FirstOrDefaultAsync(r => r.Id == id);

        return _mapper.Map<DeviceBorrowRecordDto>(result);
    }

    public async Task<DeviceBorrowRecordDto?> RejectAsync(int id, RejectBorrowDto dto, int approverId)
    {
        var record = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Applicant)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null) return null;

        if (record.ApprovalStatus != BorrowApprovalStatus.Pending)
        {
            throw new InvalidOperationException("该申请已审批，无法重复操作");
        }

        record.ApprovalStatus = BorrowApprovalStatus.Rejected;
        record.ApproverId = approverId;
        record.ApprovalTime = DateTime.UtcNow;
        record.ApprovalRemark = dto.ApprovalRemark;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await NotifyApplicantBorrowRejected(record, record.Device);

        var result = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .Include(r => r.Operator)
            .Include(r => r.Approver)
            .Include(r => r.Applicant)
            .FirstOrDefaultAsync(r => r.Id == id);

        return _mapper.Map<DeviceBorrowRecordDto>(result);
    }

    public async Task<DeviceBorrowRecordDto?> ReturnAsync(int id, ReturnDeviceBorrowDto dto, int operatorId)
    {
        var record = await _context.DeviceBorrowRecords
            .Include(r => r.Device)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null) return null;

        if (record.ApprovalStatus != BorrowApprovalStatus.Approved)
        {
            throw new InvalidOperationException("该借出申请未通过审批，无法执行归还操作");
        }

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
            .Include(r => r.Approver)
            .Include(r => r.Applicant)
            .FirstOrDefaultAsync(r => r.Id == id);

        return _mapper.Map<DeviceBorrowRecordDto>(result);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _context.DeviceBorrowRecords.FindAsync(id);
        if (record == null) return false;

        if (!record.IsReturned && record.ApprovalStatus == BorrowApprovalStatus.Approved)
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
        var borrowing = await _context.DeviceBorrowRecords.CountAsync(r => !r.IsReturned && r.ApprovalStatus == BorrowApprovalStatus.Approved);
        var returned = await _context.DeviceBorrowRecords.CountAsync(r => r.IsReturned);
        var external = await _context.DeviceBorrowRecords.CountAsync(r => r.BorrowType == BorrowType.External);
        var internalBorrow = await _context.DeviceBorrowRecords.CountAsync(r => r.BorrowType == BorrowType.Internal);
        var overdue = await _context.DeviceBorrowRecords.CountAsync(r => !r.IsReturned && r.ApprovalStatus == BorrowApprovalStatus.Approved && r.ExpectedReturnTime < DateTime.UtcNow);
        var pendingApproval = await _context.DeviceBorrowRecords.CountAsync(r => r.ApprovalStatus == BorrowApprovalStatus.Pending);

        return new DeviceBorrowStatisticsDto
        {
            TotalCount = total,
            BorrowingCount = borrowing,
            ReturnedCount = returned,
            ExternalBorrowCount = external,
            InternalBorrowCount = internalBorrow,
            OverdueCount = overdue,
            PendingApprovalCount = pendingApproval
        };
    }

    public async Task<List<DeviceBorrowRecordDto>> GetDeviceBorrowRecordsAsync(int deviceId)
    {
        var records = await _context.DeviceBorrowRecords
            .Include(r => r.Operator)
            .Include(r => r.ReturnOperator)
            .Include(r => r.Approver)
            .Include(r => r.Applicant)
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

    private async Task NotifyAdminsBorrowRequestSubmitted(DeviceBorrowRecord record, Device device)
    {
        var adminIds = await _context.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        if (adminIds.Count == 0) return;

        var title = "新的设备借出申请待审批";
        var content = $"设备 {device.Name} ({device.DeviceCode}) 有新的借出申请待审批。\n申请人: {record.BorrowerName}\n借出时间: {record.BorrowTime:yyyy-MM-dd HH:mm}\n预计归还: {record.ExpectedReturnTime:yyyy-MM-dd HH:mm}\n借出用途: {record.BorrowPurpose}";

        var batchDto = new BatchCreateNotificationDto
        {
            UserIds = adminIds,
            Title = title,
            Content = content,
            Type = NotificationType.BorrowRequestSubmitted,
            Priority = NotificationPriority.High,
            RelatedEntityType = RelatedEntityType.DeviceBorrowRecord,
            RelatedEntityId = record.Id
        };

        await _notificationService.BatchEnqueueAsync(batchDto);
    }

    private async Task NotifyApplicantBorrowApproved(DeviceBorrowRecord record, Device? device)
    {
        if (!record.ApplicantId.HasValue) return;

        var title = "设备借出申请已通过";
        var content = $"您的设备借出申请已通过审批。\n设备: {device?.Name ?? "未知设备"} ({device?.DeviceCode ?? ""})\n借出时间: {record.BorrowTime:yyyy-MM-dd HH:mm}\n预计归还: {record.ExpectedReturnTime:yyyy-MM-dd HH:mm}";

        if (!string.IsNullOrWhiteSpace(record.ApprovalRemark))
        {
            content += $"\n审批备注: {record.ApprovalRemark}";
        }

        var notificationDto = new CreateNotificationDto
        {
            UserId = record.ApplicantId.Value,
            Title = title,
            Content = content,
            Type = NotificationType.BorrowRequestApproved,
            Priority = NotificationPriority.Medium,
            RelatedEntityType = RelatedEntityType.DeviceBorrowRecord,
            RelatedEntityId = record.Id
        };

        await _notificationService.EnqueueAsync(notificationDto);
    }

    private async Task NotifyApplicantBorrowRejected(DeviceBorrowRecord record, Device? device)
    {
        if (!record.ApplicantId.HasValue) return;

        var title = "设备借出申请被拒绝";
        var content = $"您的设备借出申请已被拒绝。\n设备: {device?.Name ?? "未知设备"} ({device?.DeviceCode ?? ""})";

        if (!string.IsNullOrWhiteSpace(record.ApprovalRemark))
        {
            content += $"\n拒绝原因: {record.ApprovalRemark}";
        }

        var notificationDto = new CreateNotificationDto
        {
            UserId = record.ApplicantId.Value,
            Title = title,
            Content = content,
            Type = NotificationType.BorrowRequestRejected,
            Priority = NotificationPriority.Medium,
            RelatedEntityType = RelatedEntityType.DeviceBorrowRecord,
            RelatedEntityId = record.Id
        };

        await _notificationService.EnqueueAsync(notificationDto);
    }
}
