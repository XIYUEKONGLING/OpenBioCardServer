using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic;

/// <summary>
/// 用于增量更新的 Profile DTO
/// 所有字段均为可空，Null 表示不修改该字段
/// </summary>
public class ClassicProfilePatch
{
    [JsonProperty("username")]
    public string? Username { get; set; }
    
    [JsonProperty("userType")]
    public string? UserType { get; set; }
    
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("pronouns")]
    public string? Pronouns { get; set; }
    
    [JsonProperty("avatar")]
    public string? Avatar { get; set; }
    
    [JsonProperty("bio")]
    public string? Bio { get; set; }
    
    [JsonProperty("location")]
    public string? Location { get; set; }
    
    [JsonProperty("website")]
    public string? Website { get; set; }
    
    [JsonProperty("background")]
    public string? Background { get; set; }
    
    [JsonProperty("currentCompany")]
    public string? CurrentCompany { get; set; }
    
    [JsonProperty("currentCompanyLink")]
    public string? CurrentCompanyLink { get; set; }
    
    [JsonProperty("currentSchool")]
    public string? CurrentSchool { get; set; }
    
    [JsonProperty("currentSchoolLink")]
    public string? CurrentSchoolLink { get; set; }
    
    // Collections...
    [JsonProperty("contacts")]
    public List<ClassicContact>? Contacts { get; set; }
    
    [JsonProperty("socialLinks")]
    public List<ClassicSocialLink>? SocialLinks { get; set; }
    
    [JsonProperty("projects")]
    public List<ClassicProject>? Projects { get; set; }
    
    [JsonProperty("workExperiences")]
    public List<ClassicWorkExperience>? WorkExperiences { get; set; }
    
    [JsonProperty("schoolExperiences")]
    public List<ClassicSchoolExperience>? SchoolExperiences { get; set; }
    
    [JsonProperty("gallery")]
    public List<ClassicGalleryItem>? Gallery { get; set; }
}