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
        var queryable = _context.Devices
            .Include(d => d.Supplier)
            .AsQueryable();

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

        if (query.SupplierId.HasValue)
        {
            queryable = queryable.Where(d => d.SupplierId == query.SupplierId.Value);
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
            .Include(d => d.Supplier)
            .Include(d => d.InspectionRecords)
                .ThenInclude(r => r.Photos)
            .Include(d => d.InspectionRecords)
                .ThenInclude(r => r.Inspector)
            .Include(d => d.BorrowRecords)
                .ThenInclude(r => r.Operator)
            .Include(d => d.BorrowRecords)
                .ThenInclude(r => r.ReturnOperator)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null) return null;

        var detail = _mapper.Map<DeviceDetailDto>(device);
        detail.InspectionRecordCount = device.InspectionRecords.Count;
        detail.RecentInspectionRecords = device.InspectionRecords
            .OrderByDescending(r => r.InspectionTime)
            .Take(10)
            .Select(r => _mapper.Map<InspectionRecordDto>(r))
            .ToList();

        var borrowRecords = device.BorrowRecords
            .OrderByDescending(r => r.BorrowTime)
            .ToList();
        detail.BorrowRecordCount = borrowRecords.Count;
        detail.BorrowRecords = borrowRecords
            .Take(10)
            .Select(r => _mapper.Map<DeviceBorrowRecordDto>(r))
            .ToList();
        detail.CurrentBorrowRecord = borrowRecords
            .FirstOrDefault(r => !r.IsReturned) != null
            ? _mapper.Map<DeviceBorrowRecordDto>(borrowRecords.First(r => !r.IsReturned))
            : null;

        return detail;
    }

    public async Task<DeviceDto> CreateAsync(CreateDeviceDto dto)
    {
        if (await _context.Devices.AnyAsync(d => d.DeviceCode == dto.DeviceCode))
        {
            throw new InvalidOperationException("设备编号已存在");
        }

        if (dto.SupplierId.HasValue)
        {
            var supplier = await _context.Suppliers.FindAsync(dto.SupplierId.Value);
            if (supplier == null)
            {
                throw new InvalidOperationException("供应商不存在");
            }
            if (supplier.Status != CooperationStatus.Active)
            {
                throw new InvalidOperationException("该供应商当前非合作状态，无法关联新设备");
            }
        }

        var device = _mapper.Map<Device>(dto);
        device.CreatedAt = DateTime.UtcNow;
        device.UpdatedAt = DateTime.UtcNow;

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        var result = await _context.Devices
            .Include(d => d.Supplier)
            .FirstOrDefaultAsync(d => d.Id == device.Id);

        return _mapper.Map<DeviceDto>(result);
    }

    public async Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceDto dto)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device == null) return null;

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
        if (dto.Description != null)
            device.Description = dto.Description;

        if (dto.SupplierId.HasValue)
        {
            var supplier = await _context.Suppliers.FindAsync(dto.SupplierId.Value);
            if (supplier == null)
            {
                throw new InvalidOperationException("供应商不存在");
            }
            if (supplier.Status != CooperationStatus.Active)
            {
                throw new InvalidOperationException("该供应商当前非合作状态，无法关联设备");
            }
            device.SupplierId = dto.SupplierId.Value;
        }

        device.UpdatedAt = DateTime.UtcNow;

        if (dto.Status.HasValue && dto.Status.Value != device.Status)
        {
            await UpdateStatusCoreAsync(device, dto.Status.Value);
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        var result = await _context.Devices
            .Include(d => d.Supplier)
            .FirstOrDefaultAsync(d => d.Id == id);

        return _mapper.Map<DeviceDto>(result);
    }

    public async Task<DeviceDto?> UpdateStatusAsync(int id, DeviceStatus newStatus)
    {
        var device = await _context.Devices
            .Include(d => d.Supplier)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (device == null) return null;

        if (device.Status == newStatus)
        {
            return _mapper.Map<DeviceDto>(device);
        }

        await UpdateStatusCoreAsync(device, newStatus);

        var result = await _context.Devices
            .Include(d => d.Supplier)
            .FirstOrDefaultAsync(d => d.Id == id);

        return _mapper.Map<DeviceDto>(result);
    }

    private async Task UpdateStatusCoreAsync(Device device, DeviceStatus newStatus)
    {
        var oldStatus = device.Status;
        device.Status = newStatus;
        device.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (newStatus == DeviceStatus.Fault)
        {
            var adminUserIds = await _context.Users
                .Where(u => u.Role == UserRole.Admin && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            if (adminUserIds.Count > 0)
            {
                _ = _notificationService.BatchEnqueueAsync(new BatchCreateNotificationDto
                {
                    UserIds = adminUserIds,
                    Title = $"设备故障告警: {device.DeviceCode}",
                    Content = $"设备 {device.Name}（{device.DeviceCode}）状态已从 {oldStatus} 变为故障，请及时安排维修。",
                    Type = NotificationType.DeviceStatusChanged,
                    Priority = NotificationPriority.High,
                    RelatedEntityType = RelatedEntityType.Device,
                    RelatedEntityId = device.Id
                });
            }
        }
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
        var hasRelatedBorrowRecords = await _context.DeviceBorrowRecords.AnyAsync(r => r.DeviceId == id);
        if (hasRelatedPlans || hasRelatedFaults || hasRelatedInspectionPlans || hasRelatedInspectionTasks || hasRelatedInspectionRecords || hasRelatedBorrowRecords)
        {
            throw new InvalidOperationException("该设备有关联的保养计划、故障报修、巡检记录或借还记录，无法删除");
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
        var borrowed = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Borrowed);

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
            BorrowedCount = borrowed,
            ByCategory = byCategory
        };
    }

    public async Task<DeviceImportResultDto> ImportFromCsvAsync(Stream csvStream)
    {
        var result = new DeviceImportResultDto();
        var errors = new List<DeviceImportErrorDto>();
        var validDevices = new List<Device>();
        var deviceCodesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowNumber = 0;

        using var reader = new StreamReader(csvStream);
        var headerLine = await reader.ReadLineAsync();
        if (headerLine == null)
        {
            errors.Add(new DeviceImportErrorDto { RowNumber = 0, ErrorMessage = "CSV 文件为空或无法读取" });
            result.Errors = errors;
            return result;
        }

        var headers = ParseCsvLine(headerLine)
            .Select(h => h.Trim())
            .ToList();

        var deviceCodeIndex = FindColumnIndex(headers, "DeviceCode", "设备编号");
        var nameIndex = FindColumnIndex(headers, "Name", "设备名称");
        var categoryIndex = FindColumnIndex(headers, "Category", "类别", "设备类别");
        var modelIndex = FindColumnIndex(headers, "Model", "型号");
        var manufacturerIndex = FindColumnIndex(headers, "Manufacturer", "制造商");
        var purchaseDateIndex = FindColumnIndex(headers, "PurchaseDate", "采购日期");
        var purchasePriceIndex = FindColumnIndex(headers, "PurchasePrice", "采购价格");
        var locationIndex = FindColumnIndex(headers, "Location", "位置", "存放位置");
        var statusIndex = FindColumnIndex(headers, "Status", "状态", "设备状态");
        var descriptionIndex = FindColumnIndex(headers, "Description", "描述", "备注");
        var supplierIdIndex = FindColumnIndex(headers, "SupplierId", "供应商ID", "供应商编号");

        if (deviceCodeIndex < 0 || nameIndex < 0)
        {
            errors.Add(new DeviceImportErrorDto
            {
                RowNumber = 1,
                ErrorMessage = "CSV 表头缺少必要字段：设备编号(DeviceCode) 和 设备名称(Name) 为必填"
            });
            result.Errors = errors;
            return result;
        }

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = ParseCsvLine(line);
            if (fields.Count < headers.Count)
            {
                errors.Add(new DeviceImportErrorDto
                {
                    RowNumber = rowNumber + 1,
                    ErrorMessage = $"列数不足，期望 {headers.Count} 列，实际 {fields.Count} 列"
                });
                continue;
            }

            var deviceCode = GetFieldValue(fields, deviceCodeIndex)?.Trim() ?? string.Empty;
            var name = GetFieldValue(fields, nameIndex)?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(deviceCode))
            {
                errors.Add(new DeviceImportErrorDto
                {
                    RowNumber = rowNumber + 1,
                    DeviceCode = deviceCode,
                    ErrorMessage = "设备编号不能为空"
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new DeviceImportErrorDto
                {
                    RowNumber = rowNumber + 1,
                    DeviceCode = deviceCode,
                    ErrorMessage = "设备名称不能为空"
                });
                continue;
            }

            if (deviceCodesInFile.Contains(deviceCode))
            {
                errors.Add(new DeviceImportErrorDto
                {
                    RowNumber = rowNumber + 1,
                    DeviceCode = deviceCode,
                    ErrorMessage = "文件内存在重复的设备编号"
                });
                continue;
            }

            deviceCodesInFile.Add(deviceCode);

            var device = new Device
            {
                DeviceCode = deviceCode,
                Name = name,
                Category = GetFieldValue(fields, categoryIndex)?.Trim() ?? string.Empty,
                Model = GetFieldValue(fields, modelIndex)?.Trim() ?? string.Empty,
                Manufacturer = GetFieldValue(fields, manufacturerIndex)?.Trim() ?? string.Empty,
                Location = GetFieldValue(fields, locationIndex)?.Trim() ?? string.Empty,
                Description = GetFieldValue(fields, descriptionIndex)?.Trim()
            };

            var purchaseDateStr = GetFieldValue(fields, purchaseDateIndex)?.Trim();
            if (!string.IsNullOrWhiteSpace(purchaseDateStr))
            {
                if (DateTime.TryParse(purchaseDateStr, out var purchaseDate))
                {
                    device.PurchaseDate = DateTime.SpecifyKind(purchaseDate, DateTimeKind.Utc);
                }
                else
                {
                    errors.Add(new DeviceImportErrorDto
                    {
                        RowNumber = rowNumber + 1,
                        DeviceCode = deviceCode,
                        ErrorMessage = $"采购日期格式不正确：{purchaseDateStr}"
                    });
                    continue;
                }
            }

            var purchasePriceStr = GetFieldValue(fields, purchasePriceIndex)?.Trim();
            if (!string.IsNullOrWhiteSpace(purchasePriceStr))
            {
                if (decimal.TryParse(purchasePriceStr, out var purchasePrice))
                {
                    device.PurchasePrice = purchasePrice;
                }
                else
                {
                    errors.Add(new DeviceImportErrorDto
                    {
                        RowNumber = rowNumber + 1,
                        DeviceCode = deviceCode,
                        ErrorMessage = $"采购价格格式不正确：{purchasePriceStr}"
                    });
                    continue;
                }
            }

            var statusStr = GetFieldValue(fields, statusIndex)?.Trim();
            if (!string.IsNullOrWhiteSpace(statusStr))
            {
                if (Enum.TryParse<DeviceStatus>(statusStr, true, out var status))
                {
                    device.Status = status;
                }
                else
                {
                    errors.Add(new DeviceImportErrorDto
                    {
                        RowNumber = rowNumber + 1,
                        DeviceCode = deviceCode,
                        ErrorMessage = $"设备状态格式不正确：{statusStr}，有效值为 Running/Standby/Maintenance/Fault/Scrapped/Borrowed"
                    });
                    continue;
                }
            }
            else
            {
                device.Status = DeviceStatus.Running;
            }

            var supplierIdStr = GetFieldValue(fields, supplierIdIndex)?.Trim();
            if (!string.IsNullOrWhiteSpace(supplierIdStr))
            {
                if (int.TryParse(supplierIdStr, out var supplierId))
                {
                    device.SupplierId = supplierId;
                }
                else
                {
                    errors.Add(new DeviceImportErrorDto
                    {
                        RowNumber = rowNumber + 1,
                        DeviceCode = deviceCode,
                        ErrorMessage = $"供应商ID格式不正确：{supplierIdStr}"
                    });
                    continue;
                }
            }

            validDevices.Add(device);
        }

        result.TotalCount = rowNumber;
        result.FailedCount = errors.Count;

        if (validDevices.Count == 0)
        {
            result.Errors = errors;
            return result;
        }

        var deviceCodes = validDevices.Select(d => d.DeviceCode).ToList();
        var existingCodes = await _context.Devices
            .Where(d => deviceCodes.Contains(d.DeviceCode))
            .Select(d => d.DeviceCode)
            .ToListAsync();

        var supplierIds = validDevices
            .Where(d => d.SupplierId.HasValue)
            .Select(d => d.SupplierId!.Value)
            .Distinct()
            .ToList();

        var suppliers = await _context.Suppliers
            .Where(s => supplierIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s);

        var devicesToAdd = new List<Device>();
        var now = DateTime.UtcNow;

        foreach (var device in validDevices)
        {
            if (existingCodes.Contains(device.DeviceCode, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(new DeviceImportErrorDto
                {
                    RowNumber = 0,
                    DeviceCode = device.DeviceCode,
                    ErrorMessage = "设备编号已存在于系统中"
                });
                result.FailedCount++;
                continue;
            }

            if (device.SupplierId.HasValue)
            {
                if (!suppliers.TryGetValue(device.SupplierId.Value, out var supplier))
                {
                    errors.Add(new DeviceImportErrorDto
                    {
                        RowNumber = 0,
                        DeviceCode = device.DeviceCode,
                        ErrorMessage = $"供应商不存在（ID: {device.SupplierId.Value}）"
                    });
                    result.FailedCount++;
                    continue;
                }

                if (supplier.Status != CooperationStatus.Active)
                {
                    errors.Add(new DeviceImportErrorDto
                    {
                        RowNumber = 0,
                        DeviceCode = device.DeviceCode,
                        ErrorMessage = $"供应商（{supplier.Name}）当前非合作状态，无法关联新设备"
                    });
                    result.FailedCount++;
                    continue;
                }
            }

            device.CreatedAt = now;
            device.UpdatedAt = now;
            devicesToAdd.Add(device);
        }

        if (devicesToAdd.Count > 0)
        {
            _context.Devices.AddRange(devicesToAdd);
            await _context.SaveChangesAsync();
            result.SuccessCount = devicesToAdd.Count;
        }

        result.Errors = errors;

        var adminUserIds = await _context.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        if (adminUserIds.Count > 0)
        {
            _ = _notificationService.BatchEnqueueAsync(new BatchCreateNotificationDto
            {
                UserIds = adminUserIds,
                Title = "设备批量导入完成",
                Content = $"本次设备批量导入已完成。共处理 {result.TotalCount} 条，成功 {result.SuccessCount} 条，失败 {result.FailedCount} 条。",
                Type = NotificationType.SystemNotice,
                Priority = NotificationPriority.Medium,
                RelatedEntityType = RelatedEntityType.System
            });
        }

        return result;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = string.Empty;
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current += c;
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    result.Add(current);
                    current = string.Empty;
                }
                else
                {
                    current += c;
                }
            }
        }

        result.Add(current);
        return result;
    }

    private static int FindColumnIndex(List<string> headers, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var index = headers.FindIndex(h =>
                string.Equals(h, candidate, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                return index;
        }
        return -1;
    }

    private static string? GetFieldValue(List<string> fields, int index)
    {
        if (index < 0 || index >= fields.Count)
            return null;
        return fields[index];
    }
}
