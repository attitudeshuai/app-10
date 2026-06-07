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
public class MaintenancePlansController : ControllerBase
{
    private readonly IMaintenancePlanService _maintenancePlanService;

    public MaintenancePlansController(IMaintenancePlanService maintenancePlanService)
    {
        _maintenancePlanService = maintenancePlanService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MaintenancePlanDto>>> GetPaged([FromQuery] MaintenancePlanQueryDto query)
    {
        var result = await _maintenancePlanService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<MaintenanceStatisticsDto>> GetStatistics()
    {
        var result = await _maintenancePlanService.GetStatisticsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MaintenancePlanDto>> GetById(int id)
    {
        var plan = await _maintenancePlanService.GetByIdAsync(id);
        if (plan == null) return NotFound();
        return Ok(plan);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MaintenancePlanDto>> Create([FromBody] CreateMaintenancePlanDto dto)
    {
        try
        {
            var plan = await _maintenancePlanService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
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
    public async Task<ActionResult<MaintenancePlanDto>> Update(int id, [FromBody] UpdateMaintenancePlanDto dto)
    {
        var plan = await _maintenancePlanService.UpdateAsync(id, dto);
        if (plan == null) return NotFound();
        return Ok(plan);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _maintenancePlanService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/start")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<MaintenancePlanDto>> Start(int id)
    {
        try
        {
            var plan = await _maintenancePlanService.StartAsync(id);
            if (plan == null) return NotFound();
            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<MaintenancePlanDto>> Complete(int id, [FromBody] ExecuteMaintenancePlanDto dto)
    {
        try
        {
            var plan = await _maintenancePlanService.CompleteAsync(id, dto);
            if (plan == null) return NotFound();
            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MaintenancePlanDto>> Cancel(int id)
    {
        try
        {
            var plan = await _maintenancePlanService.CancelAsync(id);
            if (plan == null) return NotFound();
            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
