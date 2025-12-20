using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities.Mappers;

namespace OpenBioCardServer.Controllers.Classic;

[Route("classic")]
[ApiController]
public class ClassicSettingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ClassicAuthService _authService;
    private readonly ILogger<ClassicSettingsController> _logger;

    // 缓存相关依赖
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    
    // 缓存配置
    private readonly bool _isCacheEnabled;
    private readonly bool _useRedis;
    private readonly TimeSpan _absoluteExpiration;
    private readonly TimeSpan _slidingExpiration;

    // 缓存 Key 常量
    private const string PublicSettingsCacheKey = "System:Settings:Public";

    public ClassicSettingsController(
        AppDbContext context,
        ClassicAuthService authService,
        ILogger<ClassicSettingsController> logger,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
        _memoryCache = memoryCache;

        // 读取缓存配置
        _isCacheEnabled = configuration.GetValue<bool>("CacheSettings:Enabled", true);
        _useRedis = configuration.GetValue<bool>("CacheSettings:UseRedis");
        var absMinutes = configuration.GetValue<int>("CacheSettings:ExpirationMinutes", 30);
        var slideMinutes = configuration.GetValue<int>("CacheSettings:SlidingExpirationMinutes", 5);
        _absoluteExpiration = TimeSpan.FromMinutes(absMinutes);
        _slidingExpiration = TimeSpan.FromMinutes(slideMinutes);

        // 如果启用了 Redis，尝试获取 IDistributedCache 服务
        if (_useRedis)
        {
            _distributedCache = serviceProvider.GetService<IDistributedCache>();
        }
    }

    /// <summary>
    /// Get public system settings (no authentication required)
    /// GET /settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetPublicSettings()
    {
        ClassicSystemSettingsResponse? response = null;

        // 尝试读取缓存
        if (_isCacheEnabled)
        {
            try
            {
                if (_useRedis && _distributedCache != null)
                {
                    // Redis: 读取字符串并反序列化
                    var jsonStr = await _distributedCache.GetStringAsync(PublicSettingsCacheKey);
                    if (!string.IsNullOrEmpty(jsonStr))
                    {
                        response = JsonSerializer.Deserialize<ClassicSystemSettingsResponse>(jsonStr);
                    }
                }
                else
                {
                    // Memory: 直接读取对象引用
                    _memoryCache.TryGetValue(PublicSettingsCacheKey, out response);
                }

                if (response != null)
                {
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache read failed for {Key}", PublicSettingsCacheKey);
            }
        }

        try
        {
            var settings = await _context.SystemSettings.FindAsync(1);
            
            response = new ClassicSystemSettingsResponse
            {
                Title = settings?.Title ?? "OpenBioCard",
                Logo = settings?.LogoType.HasValue == true
                    ? ClassicMapper.AssetToString(settings.LogoType.Value, settings.LogoText, settings.LogoData)
                    : string.Empty
            };

            // 写入缓存
            if (_isCacheEnabled)
            {
                try
                {
                    if (_useRedis && _distributedCache != null)
                    {
                        // Redis: 序列化为 JSON 存储
                        var jsonStr = JsonSerializer.Serialize(response);
                        await _distributedCache.SetStringAsync(PublicSettingsCacheKey, jsonStr, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _absoluteExpiration
                        });
                    }
                    else
                    {
                        // Memory: 存储对象引用
                        _memoryCache.Set(PublicSettingsCacheKey, response, new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(_absoluteExpiration)
                            .SetSlidingExpiration(_slidingExpiration)
                            .SetSize(1));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cache write failed for {Key}", PublicSettingsCacheKey);
                }
            }

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

            // 更新成功后清除公共设置的缓存以确保前端获取到最新数据
            if (_isCacheEnabled)
            {
                try
                {
                    if (_useRedis && _distributedCache != null)
                    {
                        await _distributedCache.RemoveAsync(PublicSettingsCacheKey);
                    }
                    else
                    {
                        _memoryCache.Remove(PublicSettingsCacheKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cache removal failed for {Key}", PublicSettingsCacheKey);
                }
            }

            _logger.LogInformation("Admin {Username} updated system settings", request.Username);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }
}
