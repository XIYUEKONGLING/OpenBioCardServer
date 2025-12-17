using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Models;
using OpenBioCardServer.Utilities;

namespace OpenBioCardServer.Services;

public class AdminService
{
    private readonly AppDbContext _context;

    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    public async Task<List<object>> GetAllUsersAsync()
    {
        return await _context.Users
            .Select(u => new 
            { 
                username = u.Username, 
                type = u.Type 
            })
            .Cast<object>()
            .ToListAsync();
    }

    /// <summary>
    /// 创建新用户（管理员操作）
    /// </summary>
    public async Task<(bool success, string? token, string? error)> CreateUserAsync(
        string newUsername, 
        string password, 
        string type)
    {
        // 不允许创建 root 用户
        if (type == "root")
        {
            return (false, null, "Cannot create root user");
        }

        if (type != "user" && type != "admin")
        {
            return (false, null, "Invalid user type");
        }

        if (await _context.Users.AnyAsync(u => u.Username == newUsername))
        {
            return (false, null, "Username already exists");
        }

        var (hash, salt) = PasswordHasher.HashPassword(password);

        var user = new User
        {
            Username = newUsername,
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
    /// 删除用户（管理员操作）
    /// </summary>
    public async Task<(bool success, string? error)> DeleteUserAsync(
        string usernameToDelete, 
        string currentUsername)
    {
        if (usernameToDelete == currentUsername)
        {
            return (false, "Cannot delete yourself");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == usernameToDelete);
        if (user == null)
        {
            return (false, "User not found");
        }

        // 不允许删除 root 用户
        if (user.Type == "root")
        {
            return (false, "Cannot delete root user");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return (true, null);
    }
}
