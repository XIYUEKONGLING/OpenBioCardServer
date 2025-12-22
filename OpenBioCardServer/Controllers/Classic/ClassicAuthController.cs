using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Constants;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Models.DTOs.Classic.Auth;
using OpenBioCardServer.Models.DTOs.Classic.General;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities;

namespace OpenBioCardServer.Controllers.Classic;

[Route("classic")]
[ApiController]
public class ClassicAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ClassicAuthService _authService;
    private readonly ILogger<ClassicAuthController> _logger;

    public ClassicAuthController(
        AppDbContext context,
        ClassicAuthService authService,
        IConfiguration config,
        ILogger<ClassicAuthController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// User registration
    /// </summary>
    [HttpPost("signup/create")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> SignUp([FromBody] ClassicSignUpRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Type))
            {
                return BadRequest(new ClassicErrorResponse("Missing required fields"));
            }

            // Check if username already exists
            if (await _context.Accounts.AnyAsync(a => a.UserName == request.Username))
            {
                return Conflict(new ClassicErrorResponse("Username already exists"));
            }

            // Validate and parse user type
            if (!Enum.TryParse<UserType>(request.Type, true, out var userType))
            {
                return BadRequest(new ClassicErrorResponse("Invalid user type"));
            }

            // Cannot create root users via signup
            if (userType == UserType.Root)
            {
                return StatusCode(403, new ClassicErrorResponse("Cannot create root users"));
            }

            // Create new account with hashed password
            var (hash, salt) = PasswordHasher.HashPassword(request.Password);
            var account = new Account
            {
                UserName = request.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                Type = userType
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // Create default profile with basic avatar
            var profile = new ProfileEntity
            {
                AccountId = account.Id,
                Username = request.Username,
                AvatarType = AssetType.Text,
                AvatarText = "👤"
            };

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            // Generate authentication token
            var token = await _authService.CreateTokenAsync(account);

            await transaction.CommitAsync();

            _logger.LogInformation("New user registered: {Username} (Type: {Type})", 
                request.Username, userType);

            return Ok(new ClassicTokenResponse { Token = token });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new ClassicErrorResponse("Account creation failed"));
        }
    }

    /// <summary>
    /// User login (POST - JSON Body)
    /// </summary>
    [HttpPost("signin")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> SignIn([FromBody] ClassicSignInRequest request) => 
        await HandleSignInAsync(request);

    /// <summary>
    /// User login (GET - Query Parameters)
    /// </summary>
    [HttpGet("signin")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> SignInGet([FromQuery] ClassicSignInRequest request) => 
        await HandleSignInAsync(request);

    /// <summary>
    /// Shared login logic for both POST and GET
    /// </summary>
    private async Task<IActionResult> HandleSignInAsync(ClassicSignInRequest request)
    {
        try
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new ClassicErrorResponse("Username and password are required"));
            }

            // 查询账户
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserName == request.Username);

            // 账户不存在
            if (account == null)
            {
                // 使用通用错误消息，防止用户名枚举攻击
                return Unauthorized(new ClassicErrorResponse("Invalid username or password"));
            }

            // 验证密码（所有用户统一使用哈希验证）
            if (!PasswordHasher.VerifyPassword(request.Password, account.PasswordHash, account.PasswordSalt))
            {
                return Unauthorized(new ClassicErrorResponse("Invalid username or password"));
            }

            // 生成 Token
            var token = await _authService.CreateTokenAsync(account);
        
            // 更新最后登录时间
            account.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 记录日志（区分Root和普通用户）
            if (account.Type == UserType.Root)
            {
                _logger.LogInformation("Root user logged in");
            }
            else
            {
                _logger.LogInformation("User logged in: {Username} (Type: {Type})", 
                    request.Username, account.Type);
            }

            return Ok(new ClassicTokenResponse { Token = token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign in for user: {Username}", request.Username);
            return StatusCode(503, new ClassicErrorResponse("Service temporarily unavailable"));
        }
    }

    /// <summary>
    /// Delete current user's account
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> DeleteAccount([FromBody] ClassicDeleteRequest request)
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

            // Root account cannot be deleted
            if (account.Type == UserType.Root)
            {
                return StatusCode(403, new ClassicErrorResponse("Cannot delete root account"));
            }

            // Delete account (cascade deletes will handle profile and tokens)
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User deleted their account: {Username}", request.Username);

            return Ok(new ClassicOkResponse("Account deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account deletion");
            return StatusCode(500, new ClassicErrorResponse("Account deletion failed"));
        }
    }
}
