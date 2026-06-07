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

    public InspectionPlansController(IInspectionPlanService inspectionPlanService)
    {
        _inspectionPlanService = inspectionPlanService;
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

    [HttpPost("{id}/start")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<InspectionPlanDto>> Start(int id)
    {
        try
        {
            var plan = await _inspectionPlanService.StartAsync(id);
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
    public async Task<ActionResult<InspectionPlanDto>> Complete(int id)
    {
        try
        {
            var plan = await _inspectionPlanService.CompleteAsync(id);
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
}
