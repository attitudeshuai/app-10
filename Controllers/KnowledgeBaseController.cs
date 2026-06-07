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

    [HttpGet("tags")]
    public async Task<ActionResult<PagedResult<TagDto>>> GetTags([FromQuery] TagQueryDto query)
    {
        var result = await _knowledgeBaseService.GetTagsPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("tags/all")]
    public async Task<ActionResult<List<TagDto>>> GetAllTags([FromQuery] TagType? type = null)
    {
        var tags = await _knowledgeBaseService.GetAllTagsAsync(type);
        return Ok(tags);
    }

    [HttpGet("tags/{id}")]
    public async Task<ActionResult<TagDto>> GetTagById(int id)
    {
        var tag = await _knowledgeBaseService.GetTagByIdAsync(id);
        if (tag == null) return NotFound();
        return Ok(tag);
    }

    [HttpPost("tags")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagDto dto)
    {
        try
        {
            var tag = await _knowledgeBaseService.CreateTagAsync(dto);
            return CreatedAtAction(nameof(GetTagById), new { id = tag.Id }, tag);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("tags/{id}")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Technician)}")]
    public async Task<ActionResult<TagDto>> UpdateTag(int id, [FromBody] UpdateTagDto dto)
    {
        try
        {
            var tag = await _knowledgeBaseService.UpdateTagAsync(id, dto);
            if (tag == null) return NotFound();
            return Ok(tag);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("tags/{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var result = await _knowledgeBaseService.DeleteTagAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
