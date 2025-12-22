using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Constants;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Models.DTOs.Classic.Profile;
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
    /// 获取用户 Profile
    /// </summary>
    public async Task<ClassicProfile?> GetProfileAsync(string username)
    {
        var cacheKey = CacheKeys.GetClassicProfileCacheKey(username);

        return await _cache.GetOrSetAsync<ClassicProfile?>(
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
                    .FirstOrDefaultAsync(p => p.Username == username, token);
                return profile == null ? null : ClassicMapper.ToClassicProfile(profile);
            });
    }

    /// <summary>
    /// 获取用户完整导出数据 (User + Profile)
    /// </summary>
    public async Task<ClassicUserImportDto?> GetExportDataAsync(string username, string currentToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserName == username);

        if (account == null) return null;

        var profileDto = await GetProfileAsync(username);
        if (profileDto == null) return null;

        return new ClassicUserImportDto
        {
            User = new ClassicUserExportDto
            {
                Username = account.UserName,
                Type = account.Type.ToString().ToLowerInvariant(),
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
        // 验证导入的数据是否属于当前用户
        if (!string.Equals(data.User.Username, username, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Import username mismatch. Target: {Target}, Import: {Import}", 
                username, data.User.Username);
            return false;
        }

        return await UpdateProfileAsync(username, data.Profile);
    }

    /// <summary>
    /// 更新用户 Profile
    /// </summary>
    public async Task<bool> UpdateProfileAsync(string username, ClassicProfile request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var profile = await _context.Profiles
                .AsTracking()
                .FirstOrDefaultAsync(p => p.Username == username);

            if (profile == null)
            {
                _logger.LogWarning("Attempted to update non-existent profile: {Username}", username);
                return false;
            }

            // 1. Update basic profile fields
            ClassicMapper.UpdateProfileFromClassic(profile, request);

            // 2. Clear all existing collections efficiently
            // Note: ExecuteDeleteAsync executes immediately against the DB
            await _context.ContactItems.Where(c => c.ProfileId == profile.Id).ExecuteDeleteAsync();
            await _context.SocialLinkItems.Where(s => s.ProfileId == profile.Id).ExecuteDeleteAsync();
            await _context.ProjectItems.Where(p => p.ProfileId == profile.Id).ExecuteDeleteAsync();
            await _context.WorkExperienceItems.Where(w => w.ProfileId == profile.Id).ExecuteDeleteAsync();
            await _context.SchoolExperienceItems.Where(s => s.ProfileId == profile.Id).ExecuteDeleteAsync();
            await _context.GalleryItems.Where(g => g.ProfileId == profile.Id).ExecuteDeleteAsync();

            // 3. Add new collections from request
            if (request.Contacts?.Any() == true)
            {
                var contacts = ClassicMapper.ToContactEntities(request.Contacts, profile.Id);
                await _context.ContactItems.AddRangeAsync(contacts);
            }

            if (request.SocialLinks?.Any() == true)
            {
                var socialLinks = ClassicMapper.ToSocialLinkEntities(request.SocialLinks, profile.Id);
                await _context.SocialLinkItems.AddRangeAsync(socialLinks);
            }

            if (request.Projects?.Any() == true)
            {
                var projects = ClassicMapper.ToProjectEntities(request.Projects, profile.Id);
                await _context.ProjectItems.AddRangeAsync(projects);
            }

            if (request.WorkExperiences?.Any() == true)
            {
                var workExperiences = ClassicMapper.ToWorkExperienceEntities(request.WorkExperiences, profile.Id);
                await _context.WorkExperienceItems.AddRangeAsync(workExperiences);
            }

            if (request.SchoolExperiences?.Any() == true)
            {
                var schoolExperiences = ClassicMapper.ToSchoolExperienceEntities(request.SchoolExperiences, profile.Id);
                await _context.SchoolExperienceItems.AddRangeAsync(schoolExperiences);
            }

            if (request.Gallery?.Any() == true)
            {
                var gallery = ClassicMapper.ToGalleryEntities(request.Gallery, profile.Id);
                await _context.GalleryItems.AddRangeAsync(gallery);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 4. Invalidate Cache
            await _cache.RemoveAsync(CacheKeys.GetClassicProfileCacheKey(username));
            
            _logger.LogInformation("Profile updated successfully for user: {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating profile for user: {Username}", username);
            throw; // Re-throw to let controller handle the 500 response
        }
    }
}
