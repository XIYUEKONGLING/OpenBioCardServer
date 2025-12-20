using Microsoft.AspNetCore.Mvc;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities;
using OpenBioCardServer.Utilities.Mappers;

namespace OpenBioCardServer.Controllers.Classic;

[Route("classic")]
[ApiController]
public class ClassicSettingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ClassicAuthService _authService;
    private readonly ILogger<ClassicSettingsController> _logger;

    public ClassicSettingsController(
        AppDbContext context,
        ClassicAuthService authService,
        ILogger<ClassicSettingsController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get public system settings (no authentication required)
    /// GET /settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetPublicSettings()
    {
        try
        {
            var settings = await _context.SystemSettings.FindAsync(1);
            
            var response = new
            {
                title = settings?.Title ?? "OpenBioCard",
                logo = settings?.LogoType.HasValue == true
                    ? ClassicMapper.AssetToString(settings.LogoType.Value, settings.LogoText, settings.LogoData)
                    : string.Empty
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public settings");
            return StatusCode(500, new { error = "Failed to get settings" });
        }
    }

    /// <summary>
    /// Get admin system settings (admin only)
    /// POST /admin/settings
    /// </summary>
    [HttpPost("admin/settings")]
    public async Task<IActionResult> GetAdminSettings([FromBody] ClassicAdminRequest request)
    {
        try
        {
            var (isValid, account) = await _authService.ValidateTokenAsync(request.Token);

            if (!isValid || account == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            if (account.UserName != request.Username)
            {
                return Unauthorized(new { error = "Token does not match username" });
            }

            if (!await _authService.HasAdminPermissionAsync(account))
            {
                return StatusCode(403, new { error = "Insufficient permissions" });
            }

            var settings = await _context.SystemSettings.FindAsync(1);
            
            var response = new
            {
                title = settings?.Title ?? "OpenBioCard",
                logo = settings?.LogoType.HasValue == true
                    ? ClassicMapper.AssetToString(settings.LogoType.Value, settings.LogoText, settings.LogoData)
                    : string.Empty
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin settings");
            return StatusCode(500, new { error = "Failed to get settings" });
        }
    }

    /// <summary>
    /// Update system settings (admin only)
    /// POST /admin/settings/update
    /// </summary>
    [HttpPost("admin/settings/update")]
    public async Task<IActionResult> UpdateSettings([FromBody] ClassicUpdateSettingsRequest request)
    {
        try
        {
            var (isValid, account) = await _authService.ValidateTokenAsync(request.Token);

            if (!isValid || account == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            if (account.UserName != request.Username)
            {
                return Unauthorized(new { error = "Token does not match username" });
            }

            if (!await _authService.HasAdminPermissionAsync(account))
            {
                return StatusCode(403, new { error = "Insufficient permissions" });
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new { error = "Title is required" });
            }

            var settings = await _context.SystemSettings.FindAsync(1);
            
            if (settings == null)
            {
                settings = new SystemSettingsEntity
                {
                    Id = 1,
                    Title = request.Title
                };
                _context.SystemSettings.Add(settings);
            }
            else
            {
                settings.Title = request.Title;
            }

            // Parse and update logo
            if (!string.IsNullOrEmpty(request.Logo))
            {
                var (logoType, logoText, logoData) = ClassicMapper.ParseAsset(request.Logo);
                settings.LogoType = logoType;
                settings.LogoText = logoText;
                settings.LogoData = logoData;
            }
            else
            {
                settings.LogoType = null;
                settings.LogoText = null;
                settings.LogoData = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {Username} updated system settings", request.Username);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }

    public static string AssetToString(AssetType type, string? text, byte[]? data) => type switch
    {
        AssetType.Text => text ?? string.Empty,
        AssetType.Remote => text ?? string.Empty,
        AssetType.Image => data != null 
            ? $"data:{ImageHelper.DetectMimeType(data)};base64,{Convert.ToBase64String(data)}" 
            : string.Empty,
        _ => string.Empty
    };
}
