using System.ComponentModel.DataAnnotations;
using OpenBioCardServer.Models.Enums;

namespace OpenBioCardServer.Models.Entities;

public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(64)]
    public string AccountName { get; set; } = string.Empty; // Username
    
    [MaxLength(128)]
    public string EMail { get; set; } = string.Empty;

    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [MaxLength(256)]
    public string PasswordSalt { get; set; } = string.Empty;

    public AccountRole Role { get; set; } = AccountRole.User;
    public AccountType Type { get; set; } = AccountType.Personal;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<ProfileEntity> Profiles { get; set; } = new List<ProfileEntity>();
    public ICollection<Token> Tokens { get; set; } = new List<Token>();
}