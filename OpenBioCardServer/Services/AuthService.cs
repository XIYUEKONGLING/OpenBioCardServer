using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Utilities;

namespace OpenBioCardServer.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context, 
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool isValid, Account? account)> ValidateTokenAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, null);

        var tokenEntity = await _context.Tokens
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.TokenValue == token);

        if (tokenEntity == null)
            return (false, null);
        
        if (tokenEntity.IsExpired())
        {
            _context.Tokens.Remove(tokenEntity);
            await _context.SaveChangesAsync();
            return (false, null);
        }

        tokenEntity.LastUsed = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, tokenEntity.Account);
    }

    public async Task<Account?> GetRootAccountAsync()
    {
        var rootUsername = _config["AuthSettings:RootUsername"];
        return string.IsNullOrEmpty(rootUsername) 
            ? null 
            : await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserName == rootUsername && a.Type == UserType.Root);
    }

    public async Task<string> CreateTokenAsync(Account account)
    {
        const int maxTokensPerAccount = 10;
    
        var existingTokenCount = await _context.Tokens
            .CountAsync(t => t.AccountId == account.Id && t.ExpiresAt > DateTime.UtcNow);
    
        if (existingTokenCount >= maxTokensPerAccount)
        {
            var oldestToken = await _context.Tokens
                .Where(t => t.AccountId == account.Id)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        
            if (oldestToken != null)
            {
                _context.Tokens.Remove(oldestToken);
            }
        }

        var tokenValue = Guid.NewGuid().ToString();

        var token = new Token
        {
            TokenValue = tokenValue,
            AccountId = account.Id,
            DeviceInfo = account.Type == UserType.Root ? "Root Login" : null,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.Tokens.Add(token);
        await _context.SaveChangesAsync();

        return tokenValue;
    }


    public async Task<bool> HasAdminPermissionAsync(Account account) =>
        account.Type == UserType.Admin || account.Type == UserType.Root;

    public async Task<Account?> FindAccountByUsernameAsync(string username) =>
        await _context.Accounts
            .FirstOrDefaultAsync(a => a.UserName == username);

    public async Task<bool> UsernameExistsAsync(string username) =>
        await _context.Accounts.AnyAsync(a => a.UserName == username);

    public async Task<Account> CreateAccountAsync(string username, string password, UserType type)
    {
        var (hash, salt) = PasswordHasher.HashPassword(password);
        
        var account = new Account
        {
            UserName = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Type = type
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        
        return account;
    }

    public async Task CreateDefaultProfileAsync(Guid accountId, string username)
    {
        var profile = new ProfileEntity
        {
            AccountId = accountId,
            Username = username,
            AvatarType = AssetType.Text,
            AvatarText = "👤"
        };

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ValidatePasswordAsync(Account account, string password) =>
        PasswordHasher.VerifyPassword(password, account.PasswordHash, account.PasswordSalt);

    public async Task DeleteAccountAsync(Account account)
    {
        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
    }

    public async Task InvalidateAllTokensAsync(Guid accountId)
    {
        var tokens = await _context.Tokens
            .Where(t => t.AccountId == accountId)
            .ToListAsync();

        _context.Tokens.RemoveRange(tokens);
        await _context.SaveChangesAsync();
    }
}
