using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs.Admin;
using OpenBioCardServer.Models.DTOs.Auth;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Services;

namespace OpenBioCardServer.Controllers;

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        AppDbContext context,
        AuthService authService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 检查管理员权限
    /// </summary>
    [HttpGet("permission")]
    public async Task<IActionResult> CheckPermission()
    {
        var token = GetTokenFromHeader();
        var (isValid, account) = await _authService.ValidateTokenAsync(token);
        
        if (!isValid || account == null)
        {
            return Unauthorized(new { Error = "Invalid token" });
        }

        if (!await _authService.HasAdminPermissionAsync(account))
        {
            return Forbid("Insufficient permissions");
        }

        return Ok(new 
        { 
            Success = true, 
            Type = account.Role.ToString().ToLower() 
        });
    }

    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<UserListResponse>> GetUsers()
    {
        var token = GetTokenFromHeader();
        var result = await ValidateAdminTokenAsync(token); 
        if (!result.isValid)
        {
            return Unauthorized(new { Error = "Invalid token or insufficient permissions" });
        }

        var users = await _context.Accounts
            .Where(a => a.Role != AccountRole.Root)
            .Select(a => new UserInfoDto
            {
                Username = a.AccountName,
                Type = a.Role.ToString().ToLower()
            })
            .ToListAsync();

        return Ok(new UserListResponse { Users = users });
    }

    /// <summary>
    /// 创建新用户
    /// </summary>
    [HttpPost("users")]
    public async Task<ActionResult<TokenResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
    
        try
        {
            var token = GetTokenFromHeader();
            var (isValid, account) = await ValidateAdminTokenAsync(token);
        
            if (!isValid || account == null)
            {
                return Unauthorized(new { Error = "Invalid token or insufficient permissions" });
            }

            if (!Enum.TryParse<AccountRole>(request.Type, true, out var userType) || userType == AccountRole.Root)
            {
                return BadRequest(new { Error = "Invalid user type" });
            }

            if (await _authService.UsernameExistsAsync(request.NewUsername))
            {
                return Conflict(new { Error = "Username already exists" });
            }

            var newAccount = await _authService.CreateAccountAsync(request.NewUsername, request.Password, userType);
            await _authService.CreateDefaultProfileAsync(newAccount.Id, request.NewUsername);
        
            var newToken = await _authService.CreateTokenAsync(newAccount);

            await transaction.CommitAsync();

            _logger.LogInformation("Admin {AdminUser} created new user: {NewUser} (Type: {Type})", 
                account.AccountName, request.NewUsername, userType);

            return Ok(new TokenResponse { Token = newToken });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating new user");
            return StatusCode(500, new { Error = "User creation failed" });
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("users/@{username}")]
    public async Task<IActionResult> DeleteUser(string username)
    {
        var token = GetTokenFromHeader();
        var (isValid, adminAccount) = await ValidateAdminTokenAsync(token);
        
        if (!isValid || adminAccount == null)
        {
            return Unauthorized(new { Error = "Invalid token or insufficient permissions" });
        }

        if (adminAccount.AccountName == username)
        {
            return BadRequest(new { Error = "Cannot delete your own account" });
        }

        var targetAccount = await _authService.FindAccountByUsernameAsync(username);
        if (targetAccount == null)
        {
            return NotFound(new { Error = "User not found" });
        }

        if (targetAccount.Role == AccountRole.Root)
        {
            return Forbid("Cannot delete root account");
        }

        await _authService.InvalidateAllTokensAsync(targetAccount.Id);
        await _authService.DeleteAccountAsync(targetAccount);

        _logger.LogInformation("Admin {AdminUser} deleted user: {TargetUser}", 
            adminAccount.AccountName, username);

        return Ok(new { Message = "User deleted successfully" });
    }

    private async Task<(bool isValid, Account? account)> ValidateAdminTokenAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return (false, null);
        }

        var (isValid, account) = await _authService.ValidateTokenAsync(token);
        if (!isValid || account == null)
        {
            return (false, null);
        }

        return (await _authService.HasAdminPermissionAsync(account), account);
    }

    private string? GetTokenFromHeader() =>
        Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
}
