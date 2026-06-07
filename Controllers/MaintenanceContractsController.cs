using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using DeviceMaintenanceSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceMaintenanceSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaintenanceContractsController : ControllerBase
{
    private readonly IMaintenanceContractService _contractService;

    public MaintenanceContractsController(IMaintenanceContractService contractService)
    {
        _contractService = contractService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MaintenanceContractDto>>> GetPaged([FromQuery] MaintenanceContractQueryDto query)
    {
        var result = await _contractService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<MaintenanceContractStatisticsDto>> GetStatistics()
    {
        var result = await _contractService.GetStatisticsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MaintenanceContractDetailDto>> GetById(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();
        return Ok(contract);
    }

    [HttpGet("device/{deviceId}")]
    public async Task<ActionResult<List<MaintenanceContractDto>>> GetDeviceContracts(int deviceId)
    {
        var result = await _contractService.GetDeviceContractsAsync(deviceId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MaintenanceContractDto>> Create([FromBody] CreateMaintenanceContractDto dto)
    {
        try
        {
            var contract = await _contractService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = contract.Id }, contract);
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

    [HttpPut("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MaintenanceContractDto>> Update(int id, [FromBody] UpdateMaintenanceContractDto dto)
    {
        try
        {
            var contract = await _contractService.UpdateAsync(id, dto);
            if (contract == null) return NotFound();
            return Ok(contract);
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
        var result = await _contractService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
