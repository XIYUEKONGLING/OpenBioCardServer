using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models;
using OpenBioCardServer.Utilities;

namespace OpenBioCardServer.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    public async Task<(bool success, string? token, string? error)> SignupAsync(string username, string password, string type)
    {
        // 不允许注册 root 类型用户
        if (type == "root")
        {
            return (false, null, "Cannot register root user");
        }

        // 验证用户类型
        if (type != "user" && type != "admin")
        {
            return (false, null, "Invalid user type");
        }

        // 检查用户名是否已存在
        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            return (false, null, "Username already exists");
        }

        var (hash, salt) = PasswordHasher.HashPassword(password);

        var user = new User
        {
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Type = type,
            Token = Guid.NewGuid().ToString()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (true, user.Token, null);
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<(bool success, string? token, string? error)> SigninAsync(string username, string password)
    {
        // 从数据库查询用户（包括 root）
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return (false, null, "Invalid username or password");
        }

        // 验证密码
        if (!PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            return (false, null, "Invalid username or password");
        }

        return (true, user.Token, null);
    }

    /// <summary>
    /// 验证 Token 并获取用户
    /// </summary>
    public async Task<User?> ValidateTokenAsync(string token)
    {
        // 查询所有用户（包括 root）
        return await _context.Users.FirstOrDefaultAsync(u => u.Token == token);
    }

    /// <summary>
    /// 删除用户账号
    /// </summary>
    public async Task<bool> DeleteAccountAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return false;
        }

        // 不允许删除 root 用户
        if (user.Type == "root")
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}
