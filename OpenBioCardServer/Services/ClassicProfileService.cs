using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Constants;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Models.DTOs.Classic.Profile;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Utilities.Mappers;
using ZiggyCreatures.Caching.Fusion;

namespace OpenBioCardServer.Services;

public class ClassicProfileService
{
    private readonly AppDbContext _context;
    private readonly IFusionCache _cache; 
    private readonly ILogger<ClassicProfileService> _logger;

    public ClassicProfileService(
        AppDbContext context,
        IFusionCache cache,
        ILogger<ClassicProfileService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户 Profile (包含主档案及所有语言变体)
    /// </summary>
    public async Task<ClassicProfile?> GetProfileAsync(string username)
    {
        var cacheKey = CacheKeys.GetClassicProfileCacheKey(username);

        return await _cache.GetOrSetAsync<ClassicProfile?>(
            cacheKey, 
            async (ctx, token) =>
            {
                // 1. 一次性拉取该用户的所有 Profile (主 + 多语言)
                var profiles = await _context.Profiles
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(p => p.Account)
                    .Include(p => p.Contacts)
                    .Include(p => p.SocialLinks)
                    .Include(p => p.Projects)
                    .Include(p => p.WorkExperiences)
                    .Include(p => p.SchoolExperiences)
                    .Include(p => p.Gallery)
                    .Where(p => p.AccountName == username)
                    .ToListAsync(token);

                // 2. 分离主 Profile 和 语言变体
                var mainProfile = profiles.FirstOrDefault(p => p.Language == null);
                if (mainProfile == null) return null;

                var localeProfiles = profiles.Where(p => p.Language != null).ToList();

                // 3. 映射并返回 (Mapper 需支持 List<ProfileEntity>)
                return ClassicMapper.ToClassicProfile(mainProfile, localeProfiles);
            });
    }

    /// <summary>
    /// 获取用户完整导出数据 (User + Profile)
    /// </summary>
    public async Task<ClassicUserImportDto?> GetExportDataAsync(string username, string currentToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountName == username);

        if (account == null) return null;

        var profileDto = await GetProfileAsync(username);
        if (profileDto == null) return null;

        return new ClassicUserImportDto
        {
            User = new ClassicUserExportDto
            {
                Username = account.AccountName,
                Type = account.Role.ToString().ToLowerInvariant(),
                Token = currentToken
            },
            Profile = profileDto
        };
    }

    /// <summary>
    /// 导入用户数据 (主要更新 Profile)
    /// </summary>
    public async Task<bool> ImportDataAsync(string username, ClassicUserImportDto data)
    {
        if (!string.Equals(data.User.Username, username, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Import username mismatch. Target: {Target}, Import: {Import}", 
                username, data.User.Username);
            return false;
        }

        return await UpdateProfileAsync(username, data.Profile);
    }

    /// <summary>
    /// 全量更新用户主 Profile (Legacy/Full Replace)
    /// 注意：此方法目前主要重置主语言数据，不处理 Locales 的全量替换以保护多语言数据
    /// </summary>
    public async Task<bool> UpdateProfileAsync(string username, ClassicProfile request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var profile = await _context.Profiles
                .AsTracking()
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.AccountName == username && p.Language == null);

            if (profile == null)
            {
                _logger.LogWarning("Attempted to update non-existent profile: {Username}", username);
                return false;
            }

            // 1. Update basic profile fields
            ClassicMapper.UpdateProfileFromClassic(profile, request);

            // 2. Clear and Re-add Collections (Full Replace Logic)
            await ClearAllCollectionsAsync(profile.Id);
            await AddCollectionsFromDtoAsync(profile.Id, request);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _cache.RemoveAsync(CacheKeys.GetClassicProfileCacheKey(username));
            
            _logger.LogInformation("Profile updated successfully for user: {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating profile for user: {Username}", username);
            throw;
        }
    }
    
    /// <summary>
    /// 增量更新用户 Profile (Smart Update)
    /// 支持更新主 Profile 字段、集合以及多语言 Locales
    /// </summary>
    public async Task<bool> PatchProfileAsync(string username, ClassicProfilePatch patch)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. 获取主 Profile
            var mainProfile = await _context.Profiles
                .AsTracking()
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.AccountName == username && p.Language == null);

            if (mainProfile == null)
            {
                _logger.LogWarning("Attempted to patch non-existent profile: {Username}", username);
                return false;
            }

            // 2. 更新主 Profile 基础字段
            ClassicMapper.UpdateProfileFromPatch(mainProfile, patch);

            // 3. 处理多语言 Locales (新增/更新)
            if (patch.Locales != null)
            {
                var existingLocales = await _context.Profiles
                    .AsTracking()
                    .Where(p => p.AccountName == username && p.Language != null)
                    .ToListAsync();

                foreach (var (langCode, localeDto) in patch.Locales)
                {
                    var localeEntity = existingLocales.FirstOrDefault(p => p.Language == langCode);

                    if (localeEntity == null)
                    {
                        // Create new locale profile
                        localeEntity = new ProfileEntity
                        {
                            AccountId = mainProfile.AccountId,
                            AccountName = mainProfile.AccountName,
                            Language = langCode,
                            AvatarType = AssetType.Text // Default
                        };
                        _context.Profiles.Add(localeEntity);
                    }

                    ClassicMapper.UpdateEntityFromLocale(localeEntity, localeDto);
                }
            }

            // 4. 处理集合 (Contacts, Projects 等)
            await HandlePatchCollectionsAsync(mainProfile.Id, patch);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _cache.RemoveAsync(CacheKeys.GetClassicProfileCacheKey(username));
            
            _logger.LogInformation("Profile patched successfully for user: {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error patching profile for user: {Username}", username);
            throw;
        }
    }

    // === Private Helpers ===

    /// <summary>
    /// 处理 Patch 请求中的集合更新
    /// 逻辑：如果 Patch 中的集合为 null，则跳过；如果不为 null，则全量替换该类型的集合。
    /// </summary>
    private async Task HandlePatchCollectionsAsync(Guid profileId, ClassicProfilePatch patch)
    {
        if (patch.Contacts != null)
        {
            await _context.ContactItems.Where(c => c.ProfileId == profileId).ExecuteDeleteAsync();
            if (patch.Contacts.Any())
            {
                var items = ClassicMapper.ToContactEntities(patch.Contacts, profileId);
                await _context.ContactItems.AddRangeAsync(items);
            }
        }

        if (patch.SocialLinks != null)
        {
            await _context.SocialLinkItems.Where(s => s.ProfileId == profileId).ExecuteDeleteAsync();
            if (patch.SocialLinks.Any())
            {
                var items = ClassicMapper.ToSocialLinkEntities(patch.SocialLinks, profileId);
                await _context.SocialLinkItems.AddRangeAsync(items);
            }
        }

        if (patch.Projects != null)
        {
            await _context.ProjectItems.Where(p => p.ProfileId == profileId).ExecuteDeleteAsync();
            if (patch.Projects.Any())
            {
                var items = ClassicMapper.ToProjectEntities(patch.Projects, profileId);
                await _context.ProjectItems.AddRangeAsync(items);
            }
        }

        if (patch.WorkExperiences != null)
        {
            await _context.WorkExperienceItems.Where(w => w.ProfileId == profileId).ExecuteDeleteAsync();
            if (patch.WorkExperiences.Any())
            {
                var items = ClassicMapper.ToWorkExperienceEntities(patch.WorkExperiences, profileId);
                await _context.WorkExperienceItems.AddRangeAsync(items);
            }
        }

        if (patch.SchoolExperiences != null)
        {
            await _context.SchoolExperienceItems.Where(s => s.ProfileId == profileId).ExecuteDeleteAsync();
            if (patch.SchoolExperiences.Any())
            {
                var items = ClassicMapper.ToSchoolExperienceEntities(patch.SchoolExperiences, profileId);
                await _context.SchoolExperienceItems.AddRangeAsync(items);
            }
        }

        if (patch.Gallery != null)
        {
            await _context.GalleryItems.Where(g => g.ProfileId == profileId).ExecuteDeleteAsync();
            if (patch.Gallery.Any())
            {
                var items = ClassicMapper.ToGalleryEntities(patch.Gallery, profileId);
                await _context.GalleryItems.AddRangeAsync(items);
            }
        }
    }

    private async Task ClearAllCollectionsAsync(Guid profileId)
    {
        await _context.ContactItems.Where(c => c.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.SocialLinkItems.Where(s => s.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.ProjectItems.Where(p => p.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.WorkExperienceItems.Where(w => w.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.SchoolExperienceItems.Where(s => s.ProfileId == profileId).ExecuteDeleteAsync();
        await _context.GalleryItems.Where(g => g.ProfileId == profileId).ExecuteDeleteAsync();
    }

    private async Task AddCollectionsFromDtoAsync(Guid profileId, ClassicProfile request)
    {
        if (request.Contacts?.Any() == true)
            await _context.ContactItems.AddRangeAsync(ClassicMapper.ToContactEntities(request.Contacts, profileId));

        if (request.SocialLinks?.Any() == true)
            await _context.SocialLinkItems.AddRangeAsync(ClassicMapper.ToSocialLinkEntities(request.SocialLinks, profileId));

        if (request.Projects?.Any() == true)
            await _context.ProjectItems.AddRangeAsync(ClassicMapper.ToProjectEntities(request.Projects, profileId));

        if (request.WorkExperiences?.Any() == true)
            await _context.WorkExperienceItems.AddRangeAsync(ClassicMapper.ToWorkExperienceEntities(request.WorkExperiences, profileId));

        if (request.SchoolExperiences?.Any() == true)
            await _context.SchoolExperienceItems.AddRangeAsync(ClassicMapper.ToSchoolExperienceEntities(request.SchoolExperiences, profileId));

        if (request.Gallery?.Any() == true)
            await _context.GalleryItems.AddRangeAsync(ClassicMapper.ToGalleryEntities(request.Gallery, profileId));
    }
}
