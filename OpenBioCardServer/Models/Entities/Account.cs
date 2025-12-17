using System.ComponentModel.DataAnnotations;
using OpenBioCardServer.Models.Enums;

namespace OpenBioCardServer.Models.Entities;

public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(64)]
    public string UserName { get; set; } = string.Empty;
    
    [MaxLength(128)]
    public string EMail { get; set; } = string.Empty;

    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [MaxLength(256)]
    public string PasswordSalt { get; set; } = string.Empty;

    public UserType Type { get; set; } = UserType.User;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ProfileEntity? Profile { get; set; }
    public ICollection<Token> Tokens { get; set; } = new List<Token>();
}