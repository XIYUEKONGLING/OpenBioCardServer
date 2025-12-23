using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenBioCardServer.Constants;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Models.DTOs.Classic.General;
using OpenBioCardServer.Models.DTOs.Classic.Profile;
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
                return NotFound(new ClassicErrorResponse("User not found"));
            }
            
            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user: {Username}", username);
            return StatusCode(500, new ClassicErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Patch user profile (Partial Update)
    /// Only updates fields present in the JSON body.
    /// </summary>
    [HttpPost("{username}")]
    public async Task<IActionResult> PatchProfile(string username, [FromBody] ClassicProfilePatch request)
    {
        var token = GetTokenFromHeader();
        
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new ClassicErrorResponse("Missing authentication token"));

        var (isValid, account) = await _authService.ValidateTokenAsync(token);

        if (!isValid || account == null || account.AccountName != username)
            return Unauthorized(new ClassicErrorResponse("Invalid token"));

        try
        {
            var success = await _profileService.PatchProfileAsync(username, request);
            
            if (!success)
                return NotFound(new ClassicErrorResponse("Profile not found"));

            return Ok(new ClassicSuccessResponse(true));
        }
        catch (Exception)
        {
            return StatusCode(500, new ClassicErrorResponse("Profile update failed"));
        }
    }

    /// <summary>
    /// Full Update user profile (Legacy/Full Replace)
    /// Replaces the entire profile with the provided data.
    /// </summary>
    [HttpPost("{username}/update")]
    public async Task<IActionResult> FullUpdateProfile(string username, [FromBody] ClassicProfile request)
    {
        var token = GetTokenFromHeader();
        
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new ClassicErrorResponse("Missing authentication token"));

        var (isValid, account) = await _authService.ValidateTokenAsync(token);

        if (!isValid || account == null || account.AccountName != username)
            return Unauthorized(new ClassicErrorResponse("Invalid token"));

        try
        {
            var success = await _profileService.UpdateProfileAsync(username, request);
            
            if (!success)
                return NotFound(new ClassicErrorResponse("Profile not found"));

            return Ok(new ClassicSuccessResponse(true));
        }
        catch (Exception)
        {
            return StatusCode(500, new ClassicErrorResponse("Profile update failed"));
        }
    }

    /// <summary>
    /// Export user data (requires authentication)
    /// </summary>
    [HttpGet("{username}/export")]
    [EnableRateLimiting(RateLimitPolicies.General)]
    public async Task<IActionResult> ExportData(string username)
    {
        var token = GetTokenFromHeader();
        
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new ClassicErrorResponse("Missing authentication token"));

        var (isValid, account) = await _authService.ValidateTokenAsync(token);

        if (!isValid || account == null || account.AccountName != username)
            return Unauthorized(new ClassicErrorResponse("Invalid token"));

        try
        {
            var exportData = await _profileService.GetExportDataAsync(username, token);
            
            if (exportData == null)
                return NotFound(new ClassicErrorResponse("User data not found"));

            return Ok(exportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data for user: {Username}", username);
            return StatusCode(500, new ClassicErrorResponse("Export failed"));
        }
    }

    /// <summary>
    /// Import user data (requires authentication)
    /// </summary>
    [HttpPost("{username}/import")]
    public async Task<IActionResult> ImportData(string username, [FromBody] ClassicUserImportDto request)
    {
        var token = GetTokenFromHeader();
        
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new ClassicErrorResponse("Missing authentication token"));

        var (isValid, account) = await _authService.ValidateTokenAsync(token);

        if (!isValid || account == null || account.AccountName != username)
            return Unauthorized(new ClassicErrorResponse("Invalid token"));

        try
        {
            var success = await _profileService.ImportDataAsync(username, request);
            
            if (!success)
                return BadRequest(new ClassicErrorResponse("Import failed or username mismatch"));

            return Ok(new ClassicSuccessResponse(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing data for user: {Username}", username);
            return StatusCode(500, new ClassicErrorResponse("Import failed"));
        }
    }

    private string? GetTokenFromHeader()
    {
        return Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
    }
}
