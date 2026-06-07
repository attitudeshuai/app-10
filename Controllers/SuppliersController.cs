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
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ISupplierRatingService _ratingService;

    public SuppliersController(ISupplierService supplierService, ISupplierRatingService ratingService)
    {
        _supplierService = supplierService;
        _ratingService = ratingService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierDto>>> GetPaged([FromQuery] SupplierQueryDto query)
    {
        var result = await _supplierService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<SupplierDto>>> GetAll()
    {
        var result = await _supplierService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<SupplierStatisticsDto>> GetStatistics()
    {
        var result = await _supplierService.GetStatisticsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierDetailDto>> GetById(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);
        if (supplier == null) return NotFound();
        return Ok(supplier);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierDto dto)
    {
        try
        {
            var supplier = await _supplierService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<SupplierDto>> Update(int id, [FromBody] UpdateSupplierDto dto)
    {
        try
        {
            var supplier = await _supplierService.UpdateAsync(id, dto);
            if (supplier == null) return NotFound();
            return Ok(supplier);
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
            var result = await _supplierService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{supplierId}/ratings")]
    public async Task<ActionResult<PagedResult<SupplierRatingDto>>> GetRatings(int supplierId, [FromQuery] SupplierRatingQueryDto query)
    {
        query.SupplierId = supplierId;
        var result = await _ratingService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("{supplierId}/ratings/summary")]
    public async Task<ActionResult<SupplierRatingSummaryDto>> GetRatingSummary(int supplierId)
    {
        var result = await _ratingService.GetSummaryAsync(supplierId);
        return Ok(result);
    }

    [HttpGet("ratings/{id}")]
    public async Task<ActionResult<SupplierRatingDto>> GetRatingById(int id)
    {
        var rating = await _ratingService.GetByIdAsync(id);
        if (rating == null) return NotFound();
        return Ok(rating);
    }

    [HttpPost("ratings")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<SupplierRatingDto>> CreateRating([FromBody] CreateSupplierRatingDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var rating = await _ratingService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetRatingById), new { id = rating.Id }, rating);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("ratings/{id}")]
    public async Task<IActionResult> DeleteRating(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _ratingService.DeleteAsync(id, userId);
            if (!result) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
