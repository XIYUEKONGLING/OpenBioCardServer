using Microsoft.AspNetCore.Mvc;
using OpenBioCardServer.Models.DTOs;
using OpenBioCardServer.Services;

namespace OpenBioCardServer.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly AuthService _authService;

    public AdminController(AdminService adminService, AuthService authService)
    {
        _adminService = adminService;
        _authService = authService;
    }

    /// <summary>
    /// 验证管理员权限的辅助方法
    /// </summary>
    private async Task<(bool isValid, Models.User? user)> ValidateAdminAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return (false, null);
        }

        var user = await _authService.ValidateTokenAsync(token);
        if (user == null || (user.Type != "admin" && user.Type != "root"))
        {
            return (false, null);
        }

        return (true, user);
    }

    /// <summary>
    /// POST /admin/check-permission - 检查管理员权限
    /// </summary>
    [HttpPost("check-permission")]
    public async Task<IActionResult> CheckPermission([FromBody] AdminRequest request)
    {
        var (isValid, user) = await ValidateAdminAsync(request.Token);
        
        if (!isValid)
        {
            return StatusCode(403, new { error = "Insufficient permissions" });
        }

        return Ok(new { success = true, type = user!.Type });
    }

    /// <summary>
    /// POST /admin/users/list - 获取用户列表（POST 方式）
    /// </summary>
    [HttpPost("users/list")]
    public async Task<IActionResult> GetUsersListPost([FromBody] AdminRequest request)
    {
        var (isValid, _) = await ValidateAdminAsync(request.Token);
        
        if (!isValid)
        {
            return StatusCode(403, new { error = "Insufficient permissions" });
        }

        var users = await _adminService.GetAllUsersAsync();
        return Ok(new { users });
    }

    /// <summary>
    /// GET /admin/users - 获取用户列表（GET 方式）
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsersList(
        [FromHeader(Name = "Authorization")] string? authHeader,
        [FromQuery] string? username,
        [FromQuery] string? token)
    {
        // 尝试从 Header 或 Query 获取 token
        string? finalToken = null;
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            finalToken = authHeader.Substring(7);
        }
        else if (!string.IsNullOrEmpty(token))
        {
            finalToken = token;
        }

        var (isValid, _) = await ValidateAdminAsync(finalToken);
        
        if (!isValid)
        {
            return StatusCode(403, new { error = "Insufficient permissions" });
        }

        var users = await _adminService.GetAllUsersAsync();
        return Ok(new { users });
    }

    /// <summary>
    /// POST /admin/users - 创建新用户
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var (isValid, _) = await ValidateAdminAsync(request.Token);
        
        if (!isValid)
        {
            return StatusCode(403, new { error = "Insufficient permissions" });
        }

        var (success, token, error) = await _adminService.CreateUserAsync(
            request.NewUsername,
            request.Password,
            request.Type
        );

        if (!success)
        {
            if (error == "Username already exists")
            {
                return Conflict(new { error });
            }
            return StatusCode(403, new { error });
        }

        return Ok(new { message = "User created", token });
    }

    /// <summary>
    /// DELETE /admin/users/:username - 删除用户
    /// </summary>
    [HttpDelete("users/{username}")]
    public async Task<IActionResult> DeleteUser(
        string username,
        [FromBody] AdminRequest request)
    {
        var (isValid, currentUser) = await ValidateAdminAsync(request.Token);
        
        if (!isValid)
        {
            return StatusCode(403, new { error = "Insufficient permissions" });
        }

        var (success, error) = await _adminService.DeleteUserAsync(username, currentUser!.Username);

        if (!success)
        {
            return StatusCode(403, new { error });
        }

        return Ok(new { message = "User deleted" });
    }
}

