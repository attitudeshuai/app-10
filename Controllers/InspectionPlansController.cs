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
public class InspectionPlansController : ControllerBase
{
    private readonly IInspectionPlanService _inspectionPlanService;
    private readonly IInspectionTaskService _inspectionTaskService;

    public InspectionPlansController(IInspectionPlanService inspectionPlanService, IInspectionTaskService inspectionTaskService)
    {
        _inspectionPlanService = inspectionPlanService;
        _inspectionTaskService = inspectionTaskService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InspectionPlanDto>>> GetPaged([FromQuery] InspectionPlanQueryDto query)
    {
        var result = await _inspectionPlanService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InspectionPlanDto>> GetById(int id)
    {
        var plan = await _inspectionPlanService.GetByIdAsync(id);
        if (plan == null) return NotFound();
        return Ok(plan);
    }

    [HttpGet("{id}/tasks")]
    public async Task<ActionResult<List<InspectionTaskDto>>> GetPlanTasks(int id)
    {
        var tasks = await _inspectionTaskService.GetPlanTasksAsync(id);
        return Ok(tasks);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<InspectionPlanDto>> Create([FromBody] CreateInspectionPlanDto dto)
    {
        try
        {
            var plan = await _inspectionPlanService.CreateAsync(dto);
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
    public async Task<ActionResult<InspectionPlanDto>> Update(int id, [FromBody] UpdateInspectionPlanDto dto)
    {
        var plan = await _inspectionPlanService.UpdateAsync(id, dto);
        if (plan == null) return NotFound();
        return Ok(plan);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _inspectionPlanService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/pause")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<InspectionPlanDto>> Pause(int id)
    {
        try
        {
            var plan = await _inspectionPlanService.PauseAsync(id);
            if (plan == null) return NotFound();
            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/resume")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<InspectionPlanDto>> Resume(int id)
    {
        try
        {
            var plan = await _inspectionPlanService.ResumeAsync(id);
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
    public async Task<ActionResult<InspectionPlanDto>> Cancel(int id)
    {
        try
        {
            var plan = await _inspectionPlanService.CancelAsync(id);
            if (plan == null) return NotFound();
            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/generate-tasks")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult> GenerateTasks(int id, [FromQuery] int count = 30)
    {
        try
        {
            var generated = await _inspectionPlanService.GenerateTasksAsync(id, count);
            return Ok(new { generatedCount = generated });
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
}
