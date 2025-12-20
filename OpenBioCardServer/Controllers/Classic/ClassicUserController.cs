using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using OpenBioCardServer.Data;
using OpenBioCardServer.Interfaces;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities.Mappers;

namespace OpenBioCardServer.Controllers.Classic;

[Route("classic/user")]
[ApiController]
public class ClassicUserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ClassicAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ClassicUserController> _logger;

    public ClassicUserController(
        AppDbContext context,
        ClassicAuthService authService,
        ICacheService cacheService,
        ILogger<ClassicUserController> logger)
    {
        _context = context;
        _authService = authService;
        _cacheService = cacheService;
        _logger = logger;
    }

    
    // 生成统一的 Cache Key
    private static string GetProfileCacheKey(string username) => 
        $"Classic:Profile:{username.Trim().ToLowerInvariant()}";

    /// <summary>
    /// Get user profile (public endpoint)
    /// </summary>
    [HttpGet("{username}")]
    public async Task<IActionResult> GetProfile(string username)
    {
        string cacheKey = GetProfileCacheKey(username);
        
        try
        {
            var profileDto = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
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
                    .FirstOrDefaultAsync(p => p.Username == username);
                return profile == null ? null : ClassicMapper.ToClassicProfile(profile);
            });
            
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
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Extract token from Authorization header
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            
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

            var profile = await _context.Profiles
                .AsTracking()
                .FirstOrDefaultAsync(p => p.Username == username);

            if (profile == null)
            {
                return NotFound(new { error = "Profile not found" });
            }

            // Update basic profile fields
            ClassicMapper.UpdateProfileFromClassic(profile, request);
            
            // Clear all existing collections using ExecuteDeleteAsync
            await _context.ContactItems
                .Where(c => c.ProfileId == profile.Id)
                .ExecuteDeleteAsync();
            
            await _context.SocialLinkItems
                .Where(s => s.ProfileId == profile.Id)
                .ExecuteDeleteAsync();
                
            await _context.ProjectItems
                .Where(p => p.ProfileId == profile.Id)
                .ExecuteDeleteAsync();
                
            await _context.WorkExperienceItems
                .Where(w => w.ProfileId == profile.Id)
                .ExecuteDeleteAsync();
                
            await _context.SchoolExperienceItems
                .Where(s => s.ProfileId == profile.Id)
                .ExecuteDeleteAsync();
                
            await _context.GalleryItems
                .Where(g => g.ProfileId == profile.Id)
                .ExecuteDeleteAsync();

            // Add new collections from request
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
            
            // 清除缓存
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
}
