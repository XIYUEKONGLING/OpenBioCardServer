using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Constants;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Utilities.Mappers;
using ZiggyCreatures.Caching.Fusion;

namespace OpenBioCardServer.Services;

public class ProfileService
{
    private readonly AppDbContext _context;
    private readonly IFusionCache _cacheService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        AppDbContext context,
        IFusionCache cacheService,
        ILogger<ProfileService> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户资料
    /// </summary>
    public async Task<ProfileDto?> GetProfileAsync(string username)
    {
        string cacheKey = CacheKeys.GetProfileCacheKey(username);

        return await _cacheService.GetOrSetAsync<ProfileDto?>(
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
                    .FirstOrDefaultAsync(p => p.AccountName == username && p.Language == null, token);
                
                return profile == null ? null : DataMapper.ToProfileDto(profile);
            });
    }

    /// <summary>
    /// 通过 AccountId 获取用户资料（无缓存，用于 "Me" 接口）
    /// </summary>
    public async Task<ProfileDto?> GetProfileByAccountIdAsync(Guid accountId)
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
            .FirstOrDefaultAsync(p => p.AccountId == accountId && p.Language == null);

        return profile == null ? null : DataMapper.ToProfileDto(profile);
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    public async Task<bool> UpdateProfileAsync(string username, ProfileDto request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
    
        try
        {
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.AccountName == username && p.Language == null);

            if (profile == null)
            {
                _logger.LogWarning("Attempted to update non-existent profile: {Username}", username);
                return false;
            }

            // 更新基本信息
            DataMapper.UpdateProfileEntity(profile, request);

            // 清理旧集合数据
            await CleanupCollectionsAsync(profile.Id);

            // 添加新集合数据
            await AddNewCollectionsAsync(profile, request);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKeys.GetProfileCacheKey(username));

            _logger.LogInformation("Profile updated for user: {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating profile for user: {Username}", username);
            throw;
        }
    }

    private async Task CleanupCollectionsAsync(Guid profileId)
    {
        await _context.ContactItems.Where(x => x.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.SocialLinkItems.Where(x => x.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.ProjectItems.Where(x => x.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.WorkExperienceItems.Where(x => x.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.SchoolExperienceItems.Where(x => x.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.GalleryItems.Where(x => x.ProfileId == profileId).ExecuteDeleteAsync();
    }

    private async Task AddNewCollectionsAsync(ProfileEntity profile, ProfileDto dto)
    {
        if (dto.Contacts?.Any() == true)
        {
            var contacts = dto.Contacts.Select(c => DataMapper.ToContactItemEntity(c, profile.Id));
            await _context.ContactItems.AddRangeAsync(contacts);
        }

        if (dto.SocialLinks?.Any() == true)
        {
            var socialLinks = dto.SocialLinks.Select(s => DataMapper.ToSocialLinkItemEntity(s, profile.Id));
            await _context.SocialLinkItems.AddRangeAsync(socialLinks);
        }

        if (dto.Projects?.Any() == true)
        {
            var projects = dto.Projects.Select(p => DataMapper.ToProjectItemEntity(p, profile.Id));
            await _context.ProjectItems.AddRangeAsync(projects);
        }

        if (dto.WorkExperiences?.Any() == true)
        {
            var workExperiences = dto.WorkExperiences.Select(w => DataMapper.ToWorkExperienceItemEntity(w, profile.Id));
            await _context.WorkExperienceItems.AddRangeAsync(workExperiences);
        }

        if (dto.SchoolExperiences?.Any() == true)
        {
            var schoolExperiences = dto.SchoolExperiences.Select(s => DataMapper.ToSchoolExperienceItemEntity(s, profile.Id));
            await _context.SchoolExperienceItems.AddRangeAsync(schoolExperiences);
        }

        if (dto.Gallery?.Any() == true)
        {
            var gallery = dto.Gallery.Select(g => DataMapper.ToGalleryItemEntity(g, profile.Id));
            await _context.GalleryItems.AddRangeAsync(gallery);
        }
    }
}
