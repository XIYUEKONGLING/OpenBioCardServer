using System.ComponentModel.DataAnnotations;

namespace OpenBioCardServer.Models.Entities;

/// <summary>
/// 用户账户实体 - 存储身份验证和账户信息
/// </summary>
public class UserAccount
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [MaxLength(256)]
    public string PasswordSalt { get; set; } = string.Empty;
    
    [MaxLength(256)]
    public string Token { get; set; } = Guid.NewGuid().ToString();
    
    [MaxLength(16)]
    public string Type { get; set; } = "user"; // user, admin, root
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性 - 一对一关系
    public UserProfile Profile { get; set; } = null!;
}