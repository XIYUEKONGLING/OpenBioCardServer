using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using OpenBioCardServer.Data;
using OpenBioCardServer.Interfaces;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities.Mappers;
using ZiggyCreatures.Caching.Fusion;

namespace OpenBioCardServer.Controllers.Classic;

[Route("classic")]
[ApiController]
public class ClassicSettingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ClassicAuthService _authService;
    private readonly IFusionCache _cache;
    private readonly ILogger<ClassicSettingsController> _logger;
    
    // 缓存 Key 常量
    private const string PublicSettingsCacheKey = "Classic:System:Settings:Public";

    public ClassicSettingsController(
        AppDbContext context,
        ClassicAuthService authService,
        IFusionCache cache,
        ILogger<ClassicSettingsController> logger)
    {
        _context = context;
        _authService = authService;
        _cache = cache;
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
            var response = await _cache.GetOrSetAsync<ClassicPublicSettingsResponse>(
                PublicSettingsCacheKey, 
                async (ctx, token) =>
                {
                    var settings = await _context.SystemSettings.FindAsync(new object[] { 1 }, token);
                    return new ClassicPublicSettingsResponse
                    {
                        Title = settings?.Title ?? "OpenBioCard",
                        Logo = settings?.LogoType.HasValue == true
                            ? ClassicMapper.AssetToString(settings.LogoType.Value, settings.LogoText, settings.LogoData)
                            : string.Empty
                    };
                });
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public settings");
            return StatusCode(500, new ClassicErrorResponse("Failed to get settings"));
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
                return Unauthorized(new ClassicErrorResponse("Invalid token"));
            }

            if (account.UserName != request.Username)
            {
                return Unauthorized(new ClassicErrorResponse("Token does not match username"));
            }

            if (!await _authService.HasAdminPermissionAsync(account))
            {
                return StatusCode(403, new ClassicErrorResponse("Insufficient permissions"));
            }

            var settings = await _context.SystemSettings.FindAsync(1);
            
            var response = new ClassicSystemSettingsResponse
            {
                Title = settings?.Title ?? "OpenBioCard",
                Logo = settings?.LogoType.HasValue == true
                    ? ClassicMapper.AssetToString(settings.LogoType.Value, settings.LogoText, settings.LogoData)
                    : string.Empty
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin settings");
            return StatusCode(500, new ClassicErrorResponse("Failed to get settings"));
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
                return Unauthorized(new ClassicErrorResponse("Invalid token"));
            }

            if (account.UserName != request.Username)
            {
                return Unauthorized(new ClassicErrorResponse("Token does not match username"));
            }

            if (!await _authService.HasAdminPermissionAsync(account))
            {
                return StatusCode(403, new ClassicErrorResponse("Insufficient permissions"));
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new ClassicErrorResponse("Title is required"));
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

            // 更新成功后清除公共设置的缓存以确保前端获取到最新数据
            await _cache.RemoveAsync(PublicSettingsCacheKey);

            _logger.LogInformation("Admin {Username} updated system settings", request.Username);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return StatusCode(500, new ClassicErrorResponse("Failed to update settings"));
        }
    }
}
