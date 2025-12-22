using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Constants;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs.Auth;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities;

namespace OpenBioCardServer.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext context,
        AuthService authService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("signup")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<TokenResponse>> SignUp([FromBody] SignUpRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
    
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.UserType))
            {
                return BadRequest(new { Error = "Missing required fields" });
            }

            if (!Enum.TryParse<UserType>(request.UserType, true, out var userType))
            {
                return BadRequest(new { Error = "Invalid user type" });
            }

            if (userType == UserType.Root)
            {
                return Forbid("Cannot create root users");
            }

            if (await _authService.UsernameExistsAsync(request.Username))
            {
                return Conflict(new { Error = "Username already exists" });
            }

            var account = await _authService.CreateAccountAsync(request.Username, request.Password, userType);
            await _authService.CreateDefaultProfileAsync(account.Id, request.Username);

            var token = await _authService.CreateTokenAsync(account);

            await transaction.CommitAsync();

            _logger.LogInformation("New user registered: {Username} (Type: {Type})", 
                request.Username, userType);

            return Ok(new TokenResponse { Token = token });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { Error = "Account creation failed" });
        }
    }


    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("signin")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<TokenResponse>> SignIn([FromBody] SignInRequest request)
    {
        var account = await _authService.FindAccountByUsernameAsync(request.Username);
        if (account == null || !await _authService.ValidatePasswordAsync(account, request.Password))
        {
            return Unauthorized(new { Error = "Invalid username or password" });
        }

        var userToken = await _authService.CreateTokenAsync(account);
        
        account.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User logged in: {Username}", request.Username);
        return Ok(new TokenResponse { Token = userToken });
    }

    /// <summary>
    /// 删除当前用户账户
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteRequest request)
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { Error = "Missing authentication token" });
        }

        var (isValid, account) = await _authService.ValidateTokenAsync(token);
        if (!isValid || account == null || account.UserName != request.Username)
        {
            return Unauthorized(new { Error = "Invalid token or token does not match username" });
        }

        if (account.Type == UserType.Root)
        {
            return Forbid("Cannot delete root account");
        }

        await _authService.InvalidateAllTokensAsync(account.Id);
        await _authService.DeleteAccountAsync(account);

        _logger.LogInformation("User deleted their account: {Username}", request.Username);
        return Ok(new { Message = "Account deleted successfully" });
    }

    /// <summary>
    /// 登出（使当前令牌失效）
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { Error = "Missing authentication token" });
        }

        var tokenEntity = await _context.Tokens
            .FirstOrDefaultAsync(t => t.TokenValue == token);

        if (tokenEntity != null)
        {
            _context.Tokens.Remove(tokenEntity);
            await _context.SaveChangesAsync();
        }

        return Ok(new { Message = "Logged out successfully" });
    }

    private string? GetTokenFromHeader() =>
        Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
}
