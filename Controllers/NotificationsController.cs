using System.Security.Claims;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using DeviceMaintenanceSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceMaintenanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("用户未登录");
        }
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetPaged([FromQuery] NotificationQueryDto query)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.GetPagedAsync(userId, query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<NotificationStatisticsDto>> GetStatistics()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.GetStatisticsAsync(userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationDto>> GetById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var notification = await _notificationService.GetByIdAsync(id, userId);
            if (notification == null) return NotFound();
            return Ok(notification);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<NotificationDto>> Create([FromBody] CreateNotificationDto dto)
    {
        try
        {
            var notification = await _notificationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = notification.Id }, notification);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("batch")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> BatchCreate([FromBody] BatchCreateNotificationDto dto)
    {
        await _notificationService.BatchCreateAsync(dto);
        return Ok(new { message = "批量通知创建成功" });
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<NotificationDto>> MarkAsRead(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var notification = await _notificationService.MarkAsReadAsync(id, userId);
            if (notification == null) return NotFound();
            return Ok(notification);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "全部标记为已读" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.DeleteAsync(id, userId);
            if (!result) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpDelete("read")]
    public async Task<ActionResult<int>> DeleteRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.DeleteReadAsync(userId);
            return Ok(new { deletedCount = count });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
