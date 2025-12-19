using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.DTOs.Classic;
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
    private readonly IConfiguration _config;
    private readonly ILogger<ClassicAuthController> _logger;

    public ClassicAuthController(
        AppDbContext context,
        ClassicAuthService authService,
        IConfiguration config,
        ILogger<ClassicAuthController> logger)
    {
        _context = context;
        _authService = authService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// User registration
    /// </summary>
    [HttpPost("signup/create")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> SignUp([FromBody] ClassicSignUpRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Type))
            {
                return BadRequest(new { error = "Missing required fields" });
            }

            // Check if username already exists
            if (await _context.Accounts.AnyAsync(a => a.UserName == request.Username))
            {
                return Conflict(new { error = "Username already exists" });
            }

            // Validate and parse user type
            if (!Enum.TryParse<UserType>(request.Type, true, out var userType))
            {
                return BadRequest(new { error = "Invalid user type" });
            }

            // Cannot create root users via signup
            if (userType == UserType.Root)
            {
                return StatusCode(403, new { error = "Cannot create root users" });
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

            _logger.LogInformation("New user registered: {Username} (Type: {Type})", 
                request.Username, userType);

            return Ok(new ClassicTokenResponse { Token = token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { error = "Account creation failed" });
        }
    }

    /// <summary>
    /// User login
    /// </summary>
    [HttpPost("signin")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> SignIn([FromBody] ClassicSignInRequest request)
    {
        try
        {
            var rootUsername = _config["AuthSettings:RootUsername"];
            var rootPassword = _config["AuthSettings:RootPassword"];

            // Handle root user login
            if (request.Username == rootUsername)
            {
                if (request.Password == rootPassword)
                {
                    var rootAccount = await _authService.GetRootAccountAsync();
                    if (rootAccount == null)
                    {
                        return StatusCode(500, new { error = "Root account not initialized" });
                    }

                    var token = await _authService.CreateTokenAsync(rootAccount);
                    
                    // Update last login timestamp
                    rootAccount.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Root user logged in");

                    return Ok(new ClassicTokenResponse { Token = token });
                }
                
                return Unauthorized(new { error = "Invalid username or password" });
            }

            // Handle regular user login
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserName == request.Username);

            if (account == null)
            {
                return Unauthorized(new { error = "Invalid username or password" });
            }

            if (!PasswordHasher.VerifyPassword(request.Password, account.PasswordHash, account.PasswordSalt))
            {
                return Unauthorized(new { error = "Invalid username or password" });
            }

            var userToken = await _authService.CreateTokenAsync(account);
            
            // Update last login timestamp
            account.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User logged in: {Username}", request.Username);

            return Ok(new ClassicTokenResponse { Token = userToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign in");
            return StatusCode(503, new { error = "Service temporarily unavailable" });
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
                return Unauthorized(new { error = "Invalid token" });
            }

            if (account.UserName != request.Username)
            {
                return Unauthorized(new { error = "Token does not match username" });
            }

            // Root account cannot be deleted
            if (account.Type == UserType.Root)
            {
                return StatusCode(403, new { error = "Cannot delete root account" });
            }

            // Delete account (cascade deletes will handle profile and tokens)
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User deleted their account: {Username}", request.Username);

            return Ok(new { message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account deletion");
            return StatusCode(500, new { error = "Account deletion failed" });
        }
    }

    /// <summary>
    /// Initialize admin user (for first-time setup only)
    /// </summary>
    [HttpGet("init-admin")]
    public async Task<IActionResult> InitAdmin()
    {
        try
        {
            // Only allow if no users exist yet
            if (await _context.Accounts.AnyAsync())
            {
                return Ok("Admin already initialized");
            }

            // Create default admin account
            var (hash, salt) = PasswordHasher.HashPassword("admin");
            var admin = new Account
            {
                UserName = "admin",
                PasswordHash = hash,
                PasswordSalt = salt,
                Type = UserType.Admin
            };

            _context.Accounts.Add(admin);
            await _context.SaveChangesAsync();

            // Create default profile
            var profile = new ProfileEntity
            {
                AccountId = admin.Id,
                Username = "admin",
                AvatarType = AssetType.Text,
                AvatarText = "👤"
            };

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin user initialized");

            return Ok("Admin initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin initialization");
            return StatusCode(500, new { error = "Initialization failed" });
        }
    }
}
