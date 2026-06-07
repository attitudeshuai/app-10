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
public class InspectionRecordsController : ControllerBase
{
    private readonly IInspectionRecordService _inspectionRecordService;

    public InspectionRecordsController(IInspectionRecordService inspectionRecordService)
    {
        _inspectionRecordService = inspectionRecordService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InspectionRecordDto>>> GetPaged([FromQuery] InspectionRecordQueryDto query)
    {
        var result = await _inspectionRecordService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<InspectionStatisticsDto>> GetStatistics()
    {
        var result = await _inspectionRecordService.GetStatisticsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InspectionRecordDto>> GetById(int id)
    {
        var record = await _inspectionRecordService.GetByIdAsync(id);
        if (record == null) return NotFound();
        return Ok(record);
    }

    [HttpGet("device/{deviceId}/history")]
    public async Task<ActionResult<List<InspectionRecordDto>>> GetDeviceHistory(int deviceId)
    {
        var records = await _inspectionRecordService.GetDeviceInspectionHistoryAsync(deviceId);
        return Ok(records);
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<InspectionRecordDto>> Create([FromBody] CreateInspectionRecordDto dto)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized(new { message = "用户未授权" });
            }

            var record = await _inspectionRecordService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/photos")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<InspectionPhotoDto>> UploadPhoto(int id, IFormFile file, string? description = null)
    {
        try
        {
            var photo = await _inspectionRecordService.UploadPhotoAsync(id, file, description);
            return Ok(photo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _inspectionRecordService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
