using Microsoft.AspNetCore.Mvc;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Services;

namespace OpenBioCardServer.Controllers.Classic;

[Route("classic/user")]
[ApiController]
public class ClassicUserController : ControllerBase
{
    private readonly ClassicAuthService _authService;
    private readonly ClassicProfileService _profileService;
    private readonly ILogger<ClassicUserController> _logger;

    public ClassicUserController(
        ClassicAuthService authService,
        ClassicProfileService profileService,
        ILogger<ClassicUserController> logger)
    {
        _authService = authService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get user profile (public endpoint)
    /// </summary>
    [HttpGet("{username}")]
    public async Task<IActionResult> GetProfile(string username)
    {
        try
        {
            var profileDto = await _profileService.GetProfileAsync(username);
            
            if (profileDto == null)
            {
                return NotFound(new { error = "User not found" });
            }
            
            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user: {Username}", username);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user profile (requires authentication)
    /// </summary>
    [HttpPost("{username}")]
    public async Task<IActionResult> UpdateProfile(string username, [FromBody] ClassicProfile request)
    {
        var token = GetTokenFromHeader();
        
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { error = "Missing authentication token" });
        }

        var (isValid, account) = await _authService.ValidateTokenAsync(token);

        if (!isValid || account == null)
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        if (account.UserName != username)
        {
            return Unauthorized(new { error = "Token does not match username" });
        }

        try
        {
            var success = await _profileService.UpdateProfileAsync(username, request);
            
            if (!success)
            {
                return NotFound(new { error = "Profile not found" });
            }

            return Ok(new { success = true });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Profile update failed" });
        }
    }

    /// <summary>
    /// Export user data (requires authentication)
    /// </summary>
    [HttpGet("{username}/export")]
    public async Task<IActionResult> ExportData(string username)
    {
        var token = GetTokenFromHeader();
        
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { error = "Missing authentication token" });
        }

        var (isValid, account) = await _authService.ValidateTokenAsync(token);

        if (!isValid || account == null)
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        if (account.UserName != username)
        {
            return Unauthorized(new { error = "Token does not match username" });
        }

        try
        {
            var exportData = await _profileService.GetExportDataAsync(username, token);
            
            if (exportData == null)
            {
                return NotFound(new { error = "User data not found" });
            }

            return Ok(exportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data for user: {Username}", username);
            return StatusCode(500, new { error = "Export failed" });
        }
    }

    /// <summary>
    /// Import user data (requires authentication)
    /// </summary>
    [HttpPost("{username}/import")]
    public async Task<IActionResult> ImportData(string username, [FromBody] ClassicImportExportDto request)
    {
        var token = GetTokenFromHeader();
        
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { error = "Missing authentication token" });
        }

        var (isValid, account) = await _authService.ValidateTokenAsync(token);

        if (!isValid || account == null)
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        if (account.UserName != username)
        {
            return Unauthorized(new { error = "Token does not match username" });
        }

        try
        {
            var success = await _profileService.ImportDataAsync(username, request);
            
            if (!success)
            {
                return BadRequest(new { error = "Import failed or username mismatch" });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing data for user: {Username}", username);
            return StatusCode(500, new { error = "Import failed" });
        }
    }

    private string? GetTokenFromHeader()
    {
        return Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
    }
}
