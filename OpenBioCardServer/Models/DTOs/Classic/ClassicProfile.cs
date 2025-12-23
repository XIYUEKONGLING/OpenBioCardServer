using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic;

public class ClassicProfile
{
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonProperty("userType")]
    public string UserType { get; set; } = "personal";
    
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty("pronouns")]
    public string Pronouns { get; set; } = string.Empty;
    
    [JsonProperty("avatar")]
    public string Avatar { get; set; } = string.Empty;
    
    [JsonProperty("bio")]
    public string Bio { get; set; } = string.Empty;
    
    [JsonProperty("location")]
    public string Location { get; set; } = string.Empty;
    
    [JsonProperty("website")]
    public string Website { get; set; } = string.Empty;
    
    [JsonProperty("background")]
    public string Background { get; set; } = string.Empty;
    
    [JsonProperty("currentCompany")]
    public string CurrentCompany { get; set; } = string.Empty;
    
    [JsonProperty("currentCompanyLink")]
    public string CurrentCompanyLink { get; set; } = string.Empty;
    
    [JsonProperty("currentSchool")]
    public string CurrentSchool { get; set; } = string.Empty;
    
    [JsonProperty("currentSchoolLink")]
    public string CurrentSchoolLink { get; set; } = string.Empty;
    
    [JsonProperty("contacts")]
    public List<ClassicContact> Contacts { get; set; } = new();
    
    [JsonProperty("socialLinks")]
    public List<ClassicSocialLink> SocialLinks { get; set; } = new();
    
    [JsonProperty("projects")]
    public List<ClassicProject> Projects { get; set; } = new();
    
    [JsonProperty("workExperiences")]
    public List<ClassicWorkExperience> WorkExperiences { get; set; } = new();
    
    [JsonProperty("schoolExperiences")]
    public List<ClassicSchoolExperience> SchoolExperiences { get; set; } = new();
    
    [JsonProperty("gallery")]
    public List<ClassicGalleryItem> Gallery { get; set; } = new();
    
    [JsonProperty("locales")]
    public Dictionary<string, ClassicProfileLocale> Locales { get; set; } = new();
}
