using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeviceMaintenanceSystem.Services;

public class InspectionTaskService : IInspectionTaskService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly ILogger<InspectionTaskService> _logger;

    public InspectionTaskService(
        AppDbContext context,
        IMapper mapper,
        INotificationService notificationService,
        ILogger<InspectionTaskService> logger)
    {
        _context = context;
        _mapper = mapper;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PagedResult<InspectionTaskDto>> GetPagedAsync(InspectionTaskQueryDto query)
    {
        var queryable = _context.InspectionTasks
            .Include(t => t.Device)
            .Include(t => t.AssignedTechnician)
            .Include(t => t.InspectionPlan)
            .Include(t => t.InspectionRecords)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(t =>
                t.TaskCode.ToLower().Contains(keyword) ||
                t.InspectionContent.ToLower().Contains(keyword));
        }

        if (query.Status.HasValue)
            queryable = queryable.Where(t => t.Status == query.Status.Value);

        if (query.InspectionPlanId.HasValue)
            queryable = queryable.Where(t => t.InspectionPlanId == query.InspectionPlanId.Value);

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(t => t.DeviceId == query.DeviceId.Value);

        if (query.AssignedTechnicianId.HasValue)
            queryable = queryable.Where(t => t.AssignedTechnicianId == query.AssignedTechnicianId.Value);

        if (query.StartDate.HasValue)
            queryable = queryable.Where(t => t.ScheduledDate >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            queryable = queryable.Where(t => t.ScheduledDate <= query.EndDate.Value);

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "scheduleddate";
        queryable = sortBy switch
        {
            "taskcode" => query.SortDesc ? queryable.OrderByDescending(t => t.TaskCode) : queryable.OrderBy(t => t.TaskCode),
            "scheduleddate" => query.SortDesc ? queryable.OrderByDescending(t => t.ScheduledDate) : queryable.OrderBy(t => t.ScheduledDate),
            "status" => query.SortDesc ? queryable.OrderByDescending(t => t.Status) : queryable.OrderBy(t => t.Status),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(t => t.CreatedAt) : queryable.OrderBy(t => t.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(t => t.Id) : queryable.OrderBy(t => t.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<InspectionTaskDto>>(items);
        return new PagedResult<InspectionTaskDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<InspectionTaskDto?> GetByIdAsync(int id)
    {
        var task = await _context.InspectionTasks
            .Include(t => t.Device)
            .Include(t => t.AssignedTechnician)
            .Include(t => t.InspectionPlan)
            .Include(t => t.InspectionRecords)
                .ThenInclude(r => r.Photos)
            .FirstOrDefaultAsync(t => t.Id == id);
        return task == null ? null : _mapper.Map<InspectionTaskDto>(task);
    }

    public async Task<InspectionTaskDto?> StartAsync(int id)
    {
        var task = await _context.InspectionTasks.FindAsync(id);
        if (task == null) return null;
        if (task.Status != InspectionTaskStatus.Pending)
            throw new InvalidOperationException("只有待执行的任务才能开始");

        task.Status = InspectionTaskStatus.InProgress;
        task.ActualStartDate = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InspectionTaskDto?> CompleteAsync(int id)
    {
        var task = await _context.InspectionTasks.FindAsync(id);
        if (task == null) return null;
        if (task.Status != InspectionTaskStatus.InProgress && task.Status != InspectionTaskStatus.Pending)
            throw new InvalidOperationException("只有待执行或进行中的任务才能完成");

        task.Status = InspectionTaskStatus.Completed;
        task.ActualEndDate = DateTime.UtcNow;
        if (task.ActualStartDate == null)
            task.ActualStartDate = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InspectionTaskDto?> CancelAsync(int id)
    {
        var task = await _context.InspectionTasks.FindAsync(id);
        if (task == null) return null;
        if (task.Status == InspectionTaskStatus.Completed || task.Status == InspectionTaskStatus.Cancelled)
            throw new InvalidOperationException("已完成或已取消的任务不能取消");

        task.Status = InspectionTaskStatus.Cancelled;
        task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<List<InspectionTaskDto>> GetPlanTasksAsync(int planId)
    {
        var tasks = await _context.InspectionTasks
            .Include(t => t.Device)
            .Include(t => t.AssignedTechnician)
            .Include(t => t.InspectionRecords)
            .Where(t => t.InspectionPlanId == planId)
            .OrderByDescending(t => t.ScheduledDate)
            .ToListAsync();

        return _mapper.Map<List<InspectionTaskDto>>(tasks);
    }

    public async Task<int> MarkOverdueTasksAsync()
    {
        var now = DateTime.UtcNow;

        var overdueTasks = await _context.InspectionTasks
            .Include(t => t.Device)
            .Include(t => t.AssignedTechnician)
            .Where(t =>
                t.ScheduledDate < now &&
                (t.Status == InspectionTaskStatus.Pending || t.Status == InspectionTaskStatus.InProgress))
            .ToListAsync();

        if (overdueTasks.Count == 0)
            return 0;

        var adminIds = await _context.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        var nowTime = DateTime.UtcNow;
        foreach (var task in overdueTasks)
        {
            task.Status = InspectionTaskStatus.Overdue;
            task.UpdatedAt = nowTime;
        }

        await _context.SaveChangesAsync();

        foreach (var task in overdueTasks)
        {
            try
            {
                var userIds = new List<int>();

                if (task.AssignedTechnicianId.HasValue && task.AssignedTechnician?.IsActive == true)
                {
                    userIds.Add(task.AssignedTechnicianId.Value);
                }

                userIds.AddRange(adminIds);
                userIds = userIds.Distinct().ToList();

                if (userIds.Count == 0)
                    continue;

                var title = $"巡检任务逾期提醒";
                var content = $"巡检任务【{task.TaskCode}】已逾期，请及时处理。设备：{task.Device?.Name ?? "未知"}，计划时间：{task.ScheduledDate:yyyy-MM-dd HH:mm}";

                var batchDto = new BatchCreateNotificationDto
                {
                    UserIds = userIds,
                    Title = title,
                    Content = content,
                    Type = NotificationType.InspectionTaskOverdue,
                    Priority = NotificationPriority.High,
                    RelatedEntityType = RelatedEntityType.InspectionTask,
                    RelatedEntityId = task.Id
                };

                await _notificationService.BatchEnqueueAsync(batchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送逾期巡检任务通知失败，任务ID: {TaskId}", task.Id);
            }
        }

        return overdueTasks.Count;
    }
}
