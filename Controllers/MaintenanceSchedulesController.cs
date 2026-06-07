using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using DeviceMaintenanceSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceMaintenanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaintenanceSchedulesController : ControllerBase
{
    private readonly IMaintenanceScheduleService _scheduleService;

    public MaintenanceSchedulesController(IMaintenanceScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MaintenanceScheduleDto>>> GetPaged([FromQuery] MaintenanceScheduleQueryDto query)
    {
        var result = await _scheduleService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MaintenanceScheduleDto>> GetById(int id)
    {
        var schedule = await _scheduleService.GetByIdAsync(id);
        if (schedule == null) return NotFound();
        return Ok(schedule);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MaintenanceScheduleDto>> Create([FromBody] CreateMaintenanceScheduleDto dto)
    {
        try
        {
            var schedule = await _scheduleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, schedule);
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
    public async Task<ActionResult<MaintenanceScheduleDto>> Update(int id, [FromBody] UpdateMaintenanceScheduleDto dto)
    {
        try
        {
            var schedule = await _scheduleService.UpdateAsync(id, dto);
            if (schedule == null) return NotFound();
            return Ok(schedule);
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

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _scheduleService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/pause")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MaintenanceScheduleDto>> Pause(int id)
    {
        try
        {
            var schedule = await _scheduleService.PauseAsync(id);
            if (schedule == null) return NotFound();
            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/resume")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MaintenanceScheduleDto>> Resume(int id)
    {
        try
        {
            var schedule = await _scheduleService.ResumeAsync(id);
            if (schedule == null) return NotFound();
            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MaintenanceScheduleDto>> Cancel(int id)
    {
        try
        {
            var schedule = await _scheduleService.CancelAsync(id);
            if (schedule == null) return NotFound();
            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/generate-plans")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<int>> GeneratePlans(int id, [FromBody] GenerateMaintenancePlansDto dto)
    {
        try
        {
            var count = await _scheduleService.GeneratePlansAsync(id, dto.Count);
            return Ok(new { generatedCount = count, message = $"已成功生成 {count} 条保养计划" });
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

    [HttpPost("generate-upcoming")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<int>> GenerateUpcomingPlans([FromQuery] int monthsAhead = 3)
    {
        var count = await _scheduleService.GenerateUpcomingPlansAsync(monthsAhead);
        return Ok(new { generatedCount = count, message = $"已为未来 {monthsAhead} 个月生成 {count} 条保养计划" });
    }
}
