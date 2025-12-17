using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models;
using OpenBioCardServer.Utilities;

namespace OpenBioCardServer.Services;

public class SystemService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<SystemService> _logger;

    public SystemService(
        AppDbContext context, 
        IConfiguration config,
        ILogger<SystemService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// 确保 Root 用户存在，并同步密码
    /// 每次应用启动时调用
    /// </summary>
    public async Task EnsureRootUserAsync()
    {
        var rootUsername = _config["AuthSettings:RootUsername"] ?? "root";
        var rootPassword = _config["AuthSettings:RootPassword"];

        if (string.IsNullOrEmpty(rootPassword))
        {
            _logger.LogWarning("Root password not configured in appsettings. Please set AuthSettings:RootPassword");
            return;
        }

        // 查找 root 用户
        var rootUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == rootUsername);

        // 生成新的密码哈希
        var (hash, salt) = PasswordHasher.HashPassword(rootPassword);

        if (rootUser == null)
        {
            // Root 用户不存在，创建
            rootUser = new User
            {
                Username = rootUsername,
                PasswordHash = hash,
                PasswordSalt = salt,
                Type = "root",
                Name = "Root Administrator",
                Token = $"root-{Guid.NewGuid()}"
            };

            _context.Users.Add(rootUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Root user created: {Username}", rootUsername);
        }
        else
        {
            // Root 用户已存在，更新密码
            rootUser.PasswordHash = hash;
            rootUser.PasswordSalt = salt;
            rootUser.Type = "root";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Root user password synchronized: {Username}", rootUsername);
        }
    }
}
