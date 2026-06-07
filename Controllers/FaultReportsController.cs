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
public class FaultReportsController : ControllerBase
{
    private readonly IFaultReportService _faultReportService;

    public FaultReportsController(IFaultReportService faultReportService)
{
        _faultReportService = faultReportService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<FaultReportDto>>> GetPaged([FromQuery] FaultReportQueryDto query)
    {
        var result = await _faultReportService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<FaultStatisticsDto>> GetStatistics()
    {
        var result = await _faultReportService.GetStatisticsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FaultReportDto>> GetById(int id)
    {
        var report = await _faultReportService.GetByIdAsync(id);
        if (report == null) return NotFound();
        return Ok(report);
    }

    [HttpPost]
    public async Task<ActionResult<FaultReportDto>> Create([FromBody] CreateFaultReportDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var report = await _faultReportService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
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

    [HttpPut("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<FaultReportDto>> Update(int id, [FromBody] UpdateFaultReportDto dto)
    {
        var report = await _faultReportService.UpdateAsync(id, dto);
        if (report == null) return NotFound();
        return Ok(report);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _faultReportService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/assign")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<FaultReportDto>> Assign(int id, [FromBody] AssignFaultReportDto dto)
    {
        try
        {
            var report = await _faultReportService.AssignAsync(id, dto);
            if (report == null) return NotFound();
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/start")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<FaultReportDto>> Start(int id)
    {
        try
        {
            var report = await _faultReportService.StartAsync(id);
            if (report == null) return NotFound();
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<FaultReportDto>> Complete(int id, [FromBody] CompleteFaultReportDto dto)
    {
        try
        {
            var report = await _faultReportService.CompleteAsync(id, dto);
            if (report == null) return NotFound();
            return Ok(report);
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

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<FaultReportDto>> Cancel(int id)
    {
        try
        {
            var report = await _faultReportService.CancelAsync(id);
            if (report == null) return NotFound();
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
