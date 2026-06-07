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
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public KnowledgeBaseController(IKnowledgeBaseService knowledgeBaseService)
    {
        _knowledgeBaseService = knowledgeBaseService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<KnowledgeBaseArticleBriefDto>>> GetPaged([FromQuery] KnowledgeBaseArticleQueryDto query)
    {
        var result = await _knowledgeBaseService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<KnowledgeBaseStatisticsDto>> GetStatistics()
    {
        var result = await _knowledgeBaseService.GetStatisticsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<KnowledgeBaseArticleDto>> GetById(int id)
    {
        var article = await _knowledgeBaseService.GetByIdAsync(id);
        if (article == null) return NotFound();
        return Ok(article);
    }

    [HttpPost("{id}/view")]
    public async Task<ActionResult<KnowledgeBaseArticleDto>> IncrementViewCount(int id)
    {
        var article = await _knowledgeBaseService.IncrementViewCountAsync(id);
        if (article == null) return NotFound();
        return Ok(article);
    }

    [HttpGet("recommend/device/{deviceId}")]
    public async Task<ActionResult<List<KnowledgeBaseArticleBriefDto>>> GetRecommendedByDevice(int deviceId, [FromQuery] int limit = 5)
    {
        var articles = await _knowledgeBaseService.GetRecommendedArticlesByDeviceIdAsync(deviceId, limit);
        return Ok(articles);
    }

    [HttpPost]
    public async Task<ActionResult<KnowledgeBaseArticleDto>> Create([FromBody] CreateKnowledgeBaseArticleDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var article = await _knowledgeBaseService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
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
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<KnowledgeBaseArticleDto>> Update(int id, [FromBody] UpdateKnowledgeBaseArticleDto dto)
    {
        try
        {
            var article = await _knowledgeBaseService.UpdateAsync(id, dto);
            if (article == null) return NotFound();
            return Ok(article);
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
        try
        {
            var result = await _knowledgeBaseService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
