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
public class DeviceBorrowsController : ControllerBase
{
    private readonly IDeviceBorrowService _deviceBorrowService;

    public DeviceBorrowsController(IDeviceBorrowService deviceBorrowService)
    {
        _deviceBorrowService = deviceBorrowService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DeviceBorrowRecordDto>>> GetPaged([FromQuery] DeviceBorrowQueryDto query)
    {
        var result = await _deviceBorrowService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<DeviceBorrowStatisticsDto>> GetStatistics()
    {
        var result = await _deviceBorrowService.GetStatisticsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeviceBorrowRecordDto>> GetById(int id)
    {
        var record = await _deviceBorrowService.GetByIdAsync(id);
        if (record == null) return NotFound();
        return Ok(record);
    }

    [HttpPost]
    public async Task<ActionResult<DeviceBorrowRecordDto>> Borrow([FromBody] CreateDeviceBorrowDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var record = await _deviceBorrowService.BorrowAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<DeviceBorrowRecordDto>> Approve(int id, [FromBody] ApproveBorrowDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var record = await _deviceBorrowService.ApproveAsync(id, dto, userId);
            if (record == null) return NotFound();
            return Ok(record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<DeviceBorrowRecordDto>> Reject(int id, [FromBody] RejectBorrowDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var record = await _deviceBorrowService.RejectAsync(id, dto, userId);
            if (record == null) return NotFound();
            return Ok(record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/return")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<DeviceBorrowRecordDto>> Return(int id, [FromBody] ReturnDeviceBorrowDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var record = await _deviceBorrowService.ReturnAsync(id, dto, userId);
            if (record == null) return NotFound();
            return Ok(record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _deviceBorrowService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpGet("device/{deviceId}")]
    public async Task<ActionResult<List<DeviceBorrowRecordDto>>> GetByDeviceId(int deviceId)
    {
        var records = await _deviceBorrowService.GetDeviceBorrowRecordsAsync(deviceId);
        return Ok(records);
    }
}
