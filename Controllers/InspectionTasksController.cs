using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using DeviceMaintenanceSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceMaintenanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InspectionTasksController : ControllerBase
{
    private readonly IInspectionTaskService _inspectionTaskService;

    public InspectionTasksController(IInspectionTaskService inspectionTaskService)
    {
        _inspectionTaskService = inspectionTaskService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InspectionTaskDto>>> GetPaged([FromQuery] InspectionTaskQueryDto query)
    {
        var result = await _inspectionTaskService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InspectionTaskDto>> GetById(int id)
    {
        var task = await _inspectionTaskService.GetByIdAsync(id);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [HttpPost("{id}/start")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<InspectionTaskDto>> Start(int id)
    {
        try
        {
            var task = await _inspectionTaskService.StartAsync(id);
            if (task == null) return NotFound();
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<InspectionTaskDto>> Complete(int id)
    {
        try
        {
            var task = await _inspectionTaskService.CompleteAsync(id);
            if (task == null) return NotFound();
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<InspectionTaskDto>> Cancel(int id)
    {
        try
        {
            var task = await _inspectionTaskService.CancelAsync(id);
            if (task == null) return NotFound();
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
