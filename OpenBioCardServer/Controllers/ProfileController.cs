using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities.Mappers;

namespace OpenBioCardServer.Controllers;

[Route("api/profile")]
[ApiController]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        AppDbContext context,
        AuthService authService,
        ILogger<ProfileController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户资料（公开）
    /// </summary>
    [HttpGet("{username}")]
    public async Task<ActionResult<ProfileDto>> GetProfile(string username)
    {
        var profile = await _context.Profiles
            .AsSplitQuery()
            .Include(p => p.Contacts)
            .Include(p => p.SocialLinks)
            .Include(p => p.Projects)
            .Include(p => p.WorkExperiences)
            .Include(p => p.SchoolExperiences)
            .Include(p => p.Gallery)
            .FirstOrDefaultAsync(p => p.Username == username);

        if (profile == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return DataMapper.ToProfileDto(profile);
    }

    /// <summary>
    /// 更新用户资料（需要认证）
    /// </summary>
    [HttpPut("{username}")]
    public async Task<IActionResult> UpdateProfile(string username, [FromBody] ProfileDto request)
    {
        var token = GetTokenFromHeader();
        var (isValid, account) = await ValidateTokenAndUser(token, username);
        
        if (!isValid || account == null)
        {
            return Unauthorized(new { error = "Invalid token or token does not match username" });
        }

        var profile = await _context.Profiles
            .Include(p => p.Contacts)
            .Include(p => p.SocialLinks)
            .Include(p => p.Projects)
            .Include(p => p.WorkExperiences)
            .Include(p => p.SchoolExperiences)
            .Include(p => p.Gallery)
            .FirstOrDefaultAsync(p => p.Username == username);

        if (profile == null)
        {
            return NotFound(new { error = "Profile not found" });
        }

        // 更新基本资料
        DataMapper.UpdateProfileEntity(profile, request);

        // 清除并替换所有子项
        await ReplaceCollectionItemsAsync(profile, request);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile updated for user: {Username}", username);
        return Ok(new { success = true });
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

    private async Task ReplaceCollectionItemsAsync(ProfileEntity profile, ProfileDto dto)
    {
        // 清除现有集合
        _context.ContactItems.RemoveRange(profile.Contacts);
        _context.SocialLinkItems.RemoveRange(profile.SocialLinks);
        _context.ProjectItems.RemoveRange(profile.Projects);
        _context.WorkExperienceItems.RemoveRange(profile.WorkExperiences);
        _context.SchoolExperienceItems.RemoveRange(profile.SchoolExperiences);
        _context.GalleryItems.RemoveRange(profile.Gallery);

        // 添加新的集合
        if (dto.Contacts?.Any() == true)
        {
            var contacts = dto.Contacts
                .Select(c => DataMapper.ToContactItemEntity(c, profile.Id));
            _context.ContactItems.AddRange(contacts);
        }

        if (dto.SocialLinks?.Any() == true)
        {
            var socialLinks = dto.SocialLinks
                .Select(s => DataMapper.ToSocialLinkItemEntity(s, profile.Id));
            _context.SocialLinkItems.AddRange(socialLinks);
        }

        if (dto.Projects?.Any() == true)
        {
            var projects = dto.Projects
                .Select(p => DataMapper.ToProjectItemEntity(p, profile.Id));
            _context.ProjectItems.AddRange(projects);
        }

        if (dto.WorkExperiences?.Any() == true)
        {
            var workExperiences = dto.WorkExperiences
                .Select(w => DataMapper.ToWorkExperienceItemEntity(w, profile.Id));
            _context.WorkExperienceItems.AddRange(workExperiences);
        }

        if (dto.SchoolExperiences?.Any() == true)
        {
            var schoolExperiences = dto.SchoolExperiences
                .Select(s => DataMapper.ToSchoolExperienceItemEntity(s, profile.Id));
            _context.SchoolExperienceItems.AddRange(schoolExperiences);
        }

        if (dto.Gallery?.Any() == true)
        {
            var gallery = dto.Gallery
                .Select(g => DataMapper.ToGalleryItemEntity(g, profile.Id));
            _context.GalleryItems.AddRange(gallery);
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
