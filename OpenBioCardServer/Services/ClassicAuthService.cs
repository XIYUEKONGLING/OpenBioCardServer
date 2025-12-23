using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;

namespace OpenBioCardServer.Services;

public class ClassicAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<ClassicAuthService> _logger;

    public ClassicAuthService(
        AppDbContext context, 
        IConfiguration config,
        ILogger<ClassicAuthService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool isValid, Account? account)> ValidateTokenAsync(string token)
    {
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
        if (string.IsNullOrEmpty(rootUsername))
            return null;

        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountName == rootUsername && a.Role == AccountRole.Root);
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
            DeviceInfo = account.Role == AccountRole.Root ? "Root Login" : null,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.Tokens.Add(token);
        await _context.SaveChangesAsync();

        return tokenValue;
    }

    public async Task<bool> HasAdminPermissionAsync(Account account) =>
        account.Role == AccountRole.Admin || account.Role == AccountRole.Root;
}
