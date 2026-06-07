using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace DeviceMaintenanceSystem.Services;

public class InspectionRecordService : IInspectionRecordService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly string _uploadPath;

    public InspectionRecordService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "inspections");
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<PagedResult<InspectionRecordDto>> GetPagedAsync(InspectionRecordQueryDto query)
    {
        var queryable = _context.InspectionRecords
            .Include(r => r.Device)
            .Include(r => r.Inspector)
            .Include(r => r.InspectionPlan)
            .Include(r => r.Photos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(r =>
                r.RecordCode.ToLower().Contains(keyword) ||
                r.AbnormalDescription != null && r.AbnormalDescription.ToLower().Contains(keyword) ||
                r.Remark != null && r.Remark.ToLower().Contains(keyword));
        }

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(r => r.DeviceId == query.DeviceId.Value);

        if (!string.IsNullOrWhiteSpace(query.DeviceName))
        {
            var deviceName = query.DeviceName.ToLower();
            queryable = queryable.Where(r => r.Device != null && r.Device.Name.ToLower().Contains(deviceName));
        }

        if (query.InspectorId.HasValue)
            queryable = queryable.Where(r => r.InspectorId == query.InspectorId.Value);

        if (query.Result.HasValue)
            queryable = queryable.Where(r => r.Result == query.Result.Value);

        if (query.DeviceStatus.HasValue)
            queryable = queryable.Where(r => r.DeviceStatus == query.DeviceStatus.Value);

        if (query.StartDate.HasValue)
            queryable = queryable.Where(r => r.InspectionTime >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            queryable = queryable.Where(r => r.InspectionTime <= query.EndDate.Value);

        if (query.InspectionPlanId.HasValue)
            queryable = queryable.Where(r => r.InspectionPlanId == query.InspectionPlanId.Value);

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "inspectiontime";
        queryable = sortBy switch
        {
            "recordcode" => query.SortDesc ? queryable.OrderByDescending(r => r.RecordCode) : queryable.OrderBy(r => r.RecordCode),
            "inspectiontime" => query.SortDesc ? queryable.OrderByDescending(r => r.InspectionTime) : queryable.OrderBy(r => r.InspectionTime),
            "result" => query.SortDesc ? queryable.OrderByDescending(r => r.Result) : queryable.OrderBy(r => r.Result),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(r => r.CreatedAt) : queryable.OrderBy(r => r.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(r => r.Id) : queryable.OrderBy(r => r.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<InspectionRecordDto>>(items);
        return new PagedResult<InspectionRecordDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<InspectionRecordDto?> GetByIdAsync(int id)
    {
        var record = await _context.InspectionRecords
            .Include(r => r.Device)
            .Include(r => r.Inspector)
            .Include(r => r.InspectionPlan)
            .Include(r => r.Photos)
            .FirstOrDefaultAsync(r => r.Id == id);
        return record == null ? null : _mapper.Map<InspectionRecordDto>(record);
    }

    public async Task<InspectionRecordDto> CreateAsync(CreateInspectionRecordDto dto, int inspectorId)
    {
        var device = await _context.Devices.FindAsync(dto.DeviceId);
        if (device == null)
        {
            throw new KeyNotFoundException("设备不存在");
        }

        var inspector = await _context.Users.FindAsync(inspectorId);
        if (inspector == null)
        {
            throw new KeyNotFoundException("巡检员不存在");
        }

        InspectionPlan? plan = null;
        if (dto.InspectionPlanId.HasValue)
        {
            plan = await _context.InspectionPlans.FindAsync(dto.InspectionPlanId.Value);
            if (plan == null)
            {
                throw new KeyNotFoundException("巡检计划不存在");
            }
        }

        var recordCode = $"IR{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

        var record = _mapper.Map<InspectionRecord>(dto);
        record.RecordCode = recordCode;
        record.InspectorId = inspectorId;
        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        _context.InspectionRecords.Add(record);

        if (plan != null && plan.Status == InspectionPlanStatus.InProgress)
        {
            plan.Status = InspectionPlanStatus.Completed;
            plan.ActualInspectionDate = dto.InspectionTime;
            plan.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(record.Id) ?? _mapper.Map<InspectionRecordDto>(record);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _context.InspectionRecords
            .Include(r => r.Photos)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record == null) return false;

        foreach (var photo in record.Photos)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), photo.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _context.InspectionRecords.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<InspectionRecordDto>> GetDeviceInspectionHistoryAsync(int deviceId)
    {
        var records = await _context.InspectionRecords
            .Include(r => r.Device)
            .Include(r => r.Inspector)
            .Include(r => r.InspectionPlan)
            .Include(r => r.Photos)
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.InspectionTime)
            .ToListAsync();

        return _mapper.Map<List<InspectionRecordDto>>(records);
    }

    public async Task<InspectionPhotoDto> UploadPhotoAsync(int recordId, IFormFile file, string? description)
    {
        var record = await _context.InspectionRecords.FindAsync(recordId);
        if (record == null)
        {
            throw new KeyNotFoundException("巡检记录不存在");
        }

        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("请选择要上传的照片");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new InvalidOperationException("不支持的图片格式");
        }

        var maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            throw new InvalidOperationException("图片大小不能超过10MB");
        }

        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var relativePath = $"/uploads/inspections/{fileName}";
        var fullPath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var photo = new InspectionPhoto
        {
            InspectionRecordId = recordId,
            FileName = file.FileName,
            FilePath = relativePath,
            FileSize = file.Length,
            Description = description,
            UploadedAt = DateTime.UtcNow
        };

        _context.InspectionPhotos.Add(photo);
        record.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return _mapper.Map<InspectionPhotoDto>(photo);
    }

    public async Task<InspectionStatisticsDto> GetStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalPlans = await _context.InspectionPlans.CountAsync();
        var pendingPlans = await _context.InspectionPlans.CountAsync(p => p.Status == InspectionPlanStatus.Pending);
        var inProgressPlans = await _context.InspectionPlans.CountAsync(p => p.Status == InspectionPlanStatus.InProgress);
        var completedPlans = await _context.InspectionPlans.CountAsync(p => p.Status == InspectionPlanStatus.Completed);

        var totalRecords = await _context.InspectionRecords.CountAsync();
        var normalRecords = await _context.InspectionRecords.CountAsync(r => r.Result == InspectionResult.Normal);
        var abnormalRecords = await _context.InspectionRecords.CountAsync(r => r.Result == InspectionResult.Abnormal);
        var thisMonthRecords = await _context.InspectionRecords.CountAsync(r => r.InspectionTime >= monthStart);

        decimal? abnormalRate = totalRecords > 0 ? Math.Round((decimal)abnormalRecords / totalRecords * 100, 2) : null;

        return new InspectionStatisticsDto
        {
            TotalPlanCount = totalPlans,
            PendingPlanCount = pendingPlans,
            InProgressPlanCount = inProgressPlans,
            CompletedPlanCount = completedPlans,
            TotalRecordCount = totalRecords,
            NormalRecordCount = normalRecords,
            AbnormalRecordCount = abnormalRecords,
            ThisMonthRecordCount = thisMonthRecords,
            AbnormalRate = abnormalRate
        };
    }
}
