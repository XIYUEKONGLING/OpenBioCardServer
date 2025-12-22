using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities;

namespace OpenBioCardServer.Controllers.Classic;

[Route("classic/admin")]
[ApiController]
public class ClassicAdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ClassicAuthService _authService;
    private readonly ILogger<ClassicAdminController> _logger;

    public ClassicAdminController(
        AppDbContext context,
        ClassicAuthService authService,
        ILogger<ClassicAdminController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has admin permissions
    /// </summary>
    [HttpPost("check-permission")]
    public async Task<IActionResult> CheckPermission([FromBody] ClassicAdminRequest request)
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

            return Ok(new 
            { 
                success = true, 
                type = account.Type.ToString().ToLower() 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin permission");
            return StatusCode(500, new ClassicErrorResponse("Permission check failed"));
        }
    }

    /// <summary>
    /// Get list of all users (POST version for frontend)
    /// </summary>
    [HttpPost("users/list")]
    public async Task<IActionResult> GetUsersPost([FromBody] ClassicAdminRequest request) =>
        await GetUsersInternal(request.Token, request.Username);

    /// <summary>
    /// Get list of all users (GET version)
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromBody] ClassicAdminRequest request)
    {
        var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        
        if (string.IsNullOrEmpty(token))
        {
            token = request.Token;
        }

        return await GetUsersInternal(token, request.Username);
    }

    private async Task<IActionResult> GetUsersInternal(string token, string username)
    {
        try
        {
            var (isValid, account) = await _authService.ValidateTokenAsync(token);

            if (!isValid || account == null)
            {
                return Unauthorized(new ClassicErrorResponse("Invalid token"));
            }

            if (account.UserName != username)
            {
                return Unauthorized(new ClassicErrorResponse("Token does not match username"));
            }

            if (!await _authService.HasAdminPermissionAsync(account))
            {
                return StatusCode(403, new ClassicErrorResponse("Insufficient permissions"));
            }

            // Get all non-root users
            var users = await _context.Accounts
                .Where(a => a.Type != UserType.Root)
                .Select(a => new ClassicUserInfo
                {
                    Username = a.UserName,
                    Type = a.Type.ToString().ToLower()
                })
                .ToListAsync();

            return Ok(new ClassicUserListResponse { Users = users });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user list");
            return StatusCode(500, new ClassicErrorResponse("Failed to get user list"));
        }
    }

    /// <summary>
    /// Create a new user (admin only)
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] ClassicCreateUserRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
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

            // Validate new user type
            if (!Enum.TryParse<UserType>(request.Type, true, out var userType))
            {
                return BadRequest(new ClassicErrorResponse("Invalid user type"));
            }

            // Cannot create root users
            if (userType == UserType.Root)
            {
                return StatusCode(403, new ClassicErrorResponse("Cannot create root users"));
            }

            // Check if username already exists
            if (await _context.Accounts.AnyAsync(a => a.UserName == request.NewUsername))
            {
                return Conflict(new ClassicErrorResponse("Username already exists"));
            }

            // Create new account
            var (hash, salt) = PasswordHasher.HashPassword(request.Password);
            var newAccount = new Account
            {
                UserName = request.NewUsername,
                PasswordHash = hash,
                PasswordSalt = salt,
                Type = userType
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            // Create default profile
            var profile = new ProfileEntity
            {
                AccountId = newAccount.Id,
                Username = request.NewUsername,
                AvatarType = AssetType.Text,
                AvatarText = "👤"
            };

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            // Generate token for new user
            var newToken = await _authService.CreateTokenAsync(newAccount);

            await transaction.CommitAsync();

            _logger.LogInformation("Admin {AdminUser} created new user: {NewUser} (Type: {Type})", 
                request.Username, request.NewUsername, userType);

            return Ok(new 
            { 
                message = "User created", 
                token = newToken 
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating new user");
            return StatusCode(500, new ClassicErrorResponse("User creation failed"));
        }
    }

    /// <summary>
    /// Delete a user (admin only)
    /// </summary>
    [HttpDelete("users/{targetUsername}")]
    public async Task<IActionResult> DeleteUser(string targetUsername, [FromBody] ClassicAdminRequest request)
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

            // Cannot delete self
            if (account.UserName == targetUsername)
            {
                return StatusCode(403, new ClassicErrorResponse("Cannot delete your own account"));
            }

            var targetAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserName == targetUsername);

            if (targetAccount == null)
            {
                return NotFound(new ClassicErrorResponse("User not found"));
            }

            // Cannot delete root account
            if (targetAccount.Type == UserType.Root)
            {
                return StatusCode(403, new ClassicErrorResponse("Cannot delete root account"));
            }

            // Delete account (cascade deletes will handle profile and tokens)
            _context.Accounts.Remove(targetAccount);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminUser} deleted user: {TargetUser}", 
                request.Username, targetUsername);

            return Ok(new { message = "User deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return StatusCode(500, new ClassicErrorResponse("User deletion failed"));
        }
    }
}
