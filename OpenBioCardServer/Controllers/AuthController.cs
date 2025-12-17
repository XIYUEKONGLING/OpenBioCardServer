using Microsoft.AspNetCore.Mvc;
using OpenBioCardServer.Models.DTOs;
using OpenBioCardServer.Services;

namespace OpenBioCardServer.Controllers;

[ApiController]
[Route("")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// POST /signup/create - 用户注册
    /// </summary>
    [HttpPost("signup/create")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        var (success, token, error) = await _authService.SignupAsync(
            request.Username, 
            request.Password, 
            request.Type
        );

        if (!success)
        {
            return StatusCode(500, new { error });
        }

        return Ok(new AuthResponse { Token = token! });
    }

    /// <summary>
    /// POST /signin - 用户登录
    /// </summary>
    [HttpPost("signin")]
    public async Task<IActionResult> Signin([FromBody] SigninRequest request)
    {
        var (success, token, error) = await _authService.SigninAsync(
            request.Username, 
            request.Password
        );

        if (!success)
        {
            return Unauthorized(new { error });
        }

        return Ok(new AuthResponse { Token = token! });
    }

    /// <summary>
    /// POST /delete - 删除账号
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        // 验证 Token
        var user = await _authService.ValidateTokenAsync(request.Token);
        if (user == null || user.Username != request.Username)
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        var success = await _authService.DeleteAccountAsync(request.Username);
        if (!success)
        {
            return StatusCode(500, new { error = "Failed to delete account" });
        }

        return Ok(new { message = "Account deleted successfully" });
    }
}