using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;

namespace OpenBioCardServer.Services;

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public TokenCleanupService(
        IServiceProvider services,
        ILogger<TokenCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning expired tokens");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredTokensAsync()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expiredTokens = await context.Tokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            context.Tokens.RemoveRange(expiredTokens);
            await context.SaveChangesAsync();
            
            _logger.LogInformation($"Cleared {expiredTokens.Count} expired tokens");
        }
    }
}