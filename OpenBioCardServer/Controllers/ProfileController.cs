using Microsoft.AspNetCore.Mvc;
using OpenBioCardServer.Models.DTOs;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Services;

namespace OpenBioCardServer.Controllers;

[Route("api/profile")]
[ApiController]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;
    private readonly AuthService _authService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        ProfileService profileService,
        AuthService authService,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户资料（公开）
    /// </summary>
    [HttpGet("@{username}")]
    public async Task<ActionResult<ProfileDto>> GetProfile(string username)
    {
        try
        {
            var profileDto = await _profileService.GetProfileAsync(username);

            if (profileDto == null)
            {
                return NotFound(new { Error = "User not found" });
            }

            return profileDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user: {Username}", username);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// 更新用户资料（需要认证）
    /// </summary>
    [HttpPut("@{username}")]
    public async Task<IActionResult> UpdateProfile(string username, [FromBody] ProfileDto request)
    {
        try
        {
            var token = GetTokenFromHeader();
            var (isValid, account) = await ValidateTokenAndUser(token, username);
        
            if (!isValid || account == null)
            {
                return Unauthorized(new { Error = "Invalid token or token does not match username" });
            }

            var success = await _profileService.UpdateProfileAsync(username, request);

            if (!success)
            {
                return NotFound(new { Error = "Profile not found" });
            }

            return Ok(new { Success = true });
        }
        catch (Exception)
        {
            // 日志已在 Service 层记录
            return StatusCode(500, new { Error = "Profile update failed" });
        }
    }

    /// <summary>
    /// 获取当前登录用户的资料（私有）
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> GetMyProfile()
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { Error = "Missing authentication token" });
        }

        var (isValid, account) = await _authService.ValidateTokenAsync(token);
        if (!isValid || account == null)
        {
            return Unauthorized(new { Error = "Invalid token" });
        }

        var profileDto = await _profileService.GetProfileByAccountIdAsync(account.Id);

        if (profileDto == null)
        {
            return NotFound(new { Error = "Profile not found" });
        }

        return profileDto;
    }

    private async Task<(bool isValid, Account? account)> ValidateTokenAndUser(string? token, string username)
    {
        if (string.IsNullOrEmpty(token))
        {
            return (false, null);
        }

        var (isValid, account) = await _authService.ValidateTokenAsync(token);
        return (isValid && account != null && account.AccountName == username, account);
    }

    private string? GetTokenFromHeader() =>
        Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
}
