using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using DeviceMaintenanceSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceMaintenanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SparePartsController : ControllerBase
{
    private readonly ISparePartService _sparePartService;

    public SparePartsController(ISparePartService sparePartService)
    {
        _sparePartService = sparePartService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SparePartDto>>> GetPaged([FromQuery] SparePartQueryDto query)
    {
        var result = await _sparePartService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<SparePartStatisticsDto>> GetStatistics()
    {
        var result = await _sparePartService.GetStatisticsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SparePartDto>> GetById(int id)
    {
        var sparePart = await _sparePartService.GetByIdAsync(id);
        if (sparePart == null) return NotFound();
        return Ok(sparePart);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<SparePartDto>> Create([FromBody] CreateSparePartDto dto)
    {
        try
        {
            var sparePart = await _sparePartService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = sparePart.Id }, sparePart);
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
    public async Task<ActionResult<SparePartDto>> Update(int id, [FromBody] UpdateSparePartDto dto)
    {
        try
        {
            var sparePart = await _sparePartService.UpdateAsync(id, dto);
            if (sparePart == null) return NotFound();
            return Ok(sparePart);
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
        try
        {
            var result = await _sparePartService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("consumptions")]
    public async Task<ActionResult<PagedResult<SparePartConsumptionDto>>> GetConsumptions([FromQuery] SparePartConsumptionQueryDto query)
    {
        var result = await _sparePartService.GetConsumptionsAsync(query);
        return Ok(result);
    }

    [HttpGet("by-device/{deviceId}")]
    public async Task<ActionResult<List<SparePartDto>>> GetByDeviceId(int deviceId)
    {
        var result = await _sparePartService.GetByDeviceIdAsync(deviceId);
        return Ok(result);
    }
}
