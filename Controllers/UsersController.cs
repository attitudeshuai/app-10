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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<PagedResult<UserDto>>> GetPaged([FromQuery] UserQueryDto query)
    {
        var result = await _userService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
    {
        try
        {
            var user = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _userService.UpdateAsync(id, dto);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _userService.ChangePasswordAsync(userId, dto);
            if (!result) return NotFound();
            return Ok(new { message = "密码修改成功" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
    {
        var result = await _userService.ResetPasswordAsync(id, dto.NewPassword);
        if (!result) return NotFound();
        return Ok(new { message = "密码重置成功" });
    }
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}
