using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Interfaces;
using OpenBioCardServer.Models.DTOs;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities.Mappers;
using ZiggyCreatures.Caching.Fusion;

namespace OpenBioCardServer.Controllers;

[Route("api/profile")]
[ApiController]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly IFusionCache _cacheService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        AppDbContext context,
        AuthService authService,
        IFusionCache cacheService,
        ILogger<ProfileController> logger)
    {
        _context = context;
        _authService = authService;
        _cacheService = cacheService;
        _logger = logger;
    }

    // 生成统一的 Cache Key
    private static string GetProfileCacheKey(string username) => 
        $"Profile:{username.Trim().ToLowerInvariant()}";

    /// <summary>
    /// 获取用户资料（公开）
    /// </summary>
    [HttpGet("{username}")]
    public async Task<ActionResult<ProfileDto>> GetProfile(string username)
    {
        string cacheKey = GetProfileCacheKey(username);

        try
        {
            var profileDto = await _cacheService.GetOrSetAsync<ProfileDto?>(
                cacheKey, 
                async (ctx, token) =>
                {
                    var profile = await _context.Profiles
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Include(p => p.Contacts)
                        .Include(p => p.SocialLinks)
                        .Include(p => p.Projects)
                        .Include(p => p.WorkExperiences)
                        .Include(p => p.SchoolExperiences)
                        .Include(p => p.Gallery)
                        .FirstOrDefaultAsync(p => p.Username == username, token); // 传入 token
                    return profile == null ? null : DataMapper.ToProfileDto(profile);
                });

            if (profileDto == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return profileDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user: {Username}", username);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// 更新用户资料（需要认证）
    /// </summary>
    [HttpPut("{username}")]
    public async Task<IActionResult> UpdateProfile(string username, [FromBody] ProfileDto request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
    
        try
        {
            var token = GetTokenFromHeader();
            var (isValid, account) = await ValidateTokenAndUser(token, username);
        
            if (!isValid || account == null)
            {
                return Unauthorized(new { error = "Invalid token or token does not match username" });
            }

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.Username == username);

            if (profile == null)
            {
                return NotFound(new { error = "Profile not found" });
            }

            DataMapper.UpdateProfileEntity(profile, request);

            await CleanupCollectionsAsync(profile.Id);

            await AddNewCollectionsAsync(profile, request);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync(GetProfileCacheKey(username));

            _logger.LogInformation("Profile updated for user: {Username}", username);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating profile for user: {Username}", username);
            return StatusCode(500, new { error = "Profile update failed" });
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
            return Unauthorized(new { error = "Missing authentication token" });
        }

        var (isValid, account) = await _authService.ValidateTokenAsync(token);
        if (!isValid || account == null)
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        var profile = await _context.Profiles
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Contacts)
            .Include(p => p.SocialLinks)
            .Include(p => p.Projects)
            .Include(p => p.WorkExperiences)
            .Include(p => p.SchoolExperiences)
            .Include(p => p.Gallery)
            .FirstOrDefaultAsync(p => p.AccountId == account.Id);

        if (profile == null)
        {
            return NotFound(new { error = "Profile not found" });
        }

        return DataMapper.ToProfileDto(profile);
    }

    private async Task CleanupCollectionsAsync(Guid profileId)
    {
        await _context.ContactItems
            .Where(x => x.ProfileId == profileId)
            .ExecuteDeleteAsync();
            
        await _context.SocialLinkItems
            .Where(x => x.ProfileId == profileId)
            .ExecuteDeleteAsync();
            
        await _context.ProjectItems
            .Where(x => x.ProfileId == profileId)
            .ExecuteDeleteAsync();
            
        await _context.WorkExperienceItems
            .Where(x => x.ProfileId == profileId)
            .ExecuteDeleteAsync();
            
        await _context.SchoolExperienceItems
            .Where(x => x.ProfileId == profileId)
            .ExecuteDeleteAsync();
            
        await _context.GalleryItems
            .Where(x => x.ProfileId == profileId)
            .ExecuteDeleteAsync();
    }

    /// <summary>
    /// 将 DTO 转换为实体并添加到 Context
    /// </summary>
    private async Task AddNewCollectionsAsync(ProfileEntity profile, ProfileDto dto)
    {
        if (dto.Contacts?.Any() == true)
        {
            var contacts = dto.Contacts
                .Select(c => DataMapper.ToContactItemEntity(c, profile.Id));
            await _context.ContactItems.AddRangeAsync(contacts);
        }

        if (dto.SocialLinks?.Any() == true)
        {
            var socialLinks = dto.SocialLinks
                .Select(s => DataMapper.ToSocialLinkItemEntity(s, profile.Id));
            await _context.SocialLinkItems.AddRangeAsync(socialLinks);
        }

        if (dto.Projects?.Any() == true)
        {
            var projects = dto.Projects
                .Select(p => DataMapper.ToProjectItemEntity(p, profile.Id));
            await _context.ProjectItems.AddRangeAsync(projects);
        }

        if (dto.WorkExperiences?.Any() == true)
        {
            var workExperiences = dto.WorkExperiences
                .Select(w => DataMapper.ToWorkExperienceItemEntity(w, profile.Id));
            await _context.WorkExperienceItems.AddRangeAsync(workExperiences);
        }

        if (dto.SchoolExperiences?.Any() == true)
        {
            var schoolExperiences = dto.SchoolExperiences
                .Select(s => DataMapper.ToSchoolExperienceItemEntity(s, profile.Id));
            await _context.SchoolExperienceItems.AddRangeAsync(schoolExperiences);
        }

        if (dto.Gallery?.Any() == true)
        {
            var gallery = dto.Gallery
                .Select(g => DataMapper.ToGalleryItemEntity(g, profile.Id));
            await _context.GalleryItems.AddRangeAsync(gallery);
        }
    }

    private async Task<(bool isValid, Account? account)> ValidateTokenAndUser(string? token, string username)
    {
        if (string.IsNullOrEmpty(token))
        {
            return (false, null);
        }

        var (isValid, account) = await _authService.ValidateTokenAsync(token);
        return (isValid && account != null && account.UserName == username, account);
    }

    private string? GetTokenFromHeader() =>
        Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
}
