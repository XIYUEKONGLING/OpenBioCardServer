using Microsoft.AspNetCore.Mvc;
using OpenBioCardServer.Models;
using OpenBioCardServer.Models.DTOs;
using OpenBioCardServer.Services;

namespace OpenBioCardServer.Controllers;

[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AuthService _authService;

    public UserController(UserService userService, AuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    /// <summary>
    /// GET /user/:username - 获取用户公开资料
    /// </summary>
    [HttpGet("{username}")]
    public async Task<IActionResult> GetUserProfile(string username)
    {
        var user = await _userService.GetUserProfileAsync(username);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// POST /user/:username - 更新用户资料
    /// </summary>
    [HttpPost("{username}")]
    public async Task<IActionResult> UpdateUserProfile(
        string username, 
        [FromBody] User updatedProfile,
        [FromHeader(Name = "Authorization")] string? authHeader)
    {
        // 从 Header 获取 Token
        string? token = null;
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            token = authHeader.Substring(7);
        }

        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { error = "Token required" });
        }

        // 验证 Token
        var currentUser = await _authService.ValidateTokenAsync(token);
        if (currentUser == null || currentUser.Username != username)
        {
            return Unauthorized(new { error = "Invalid token or unauthorized" });
        }

        var success = await _userService.UpdateUserProfileAsync(username, updatedProfile);
        if (!success)
        {
            return StatusCode(500, new { error = "Failed to update profile" });
        }

        return Ok(new { success = true });
    }
}
