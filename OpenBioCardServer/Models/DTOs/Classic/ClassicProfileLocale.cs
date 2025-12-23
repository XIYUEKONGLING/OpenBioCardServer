using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic;

/// <summary>
/// 多语言字段 DTO
/// </summary>
public class ClassicProfileLocale
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }
    
    [JsonProperty("pronouns", NullValueHandling = NullValueHandling.Ignore)]
    public string? Pronouns { get; set; }
    
    [JsonProperty("avatar", NullValueHandling = NullValueHandling.Ignore)]
    public string? Avatar { get; set; }
    
    [JsonProperty("bio", NullValueHandling = NullValueHandling.Ignore)]
    public string? Bio { get; set; }
    
    [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
    public string? Location { get; set; }
    
    [JsonProperty("website", NullValueHandling = NullValueHandling.Ignore)]
    public string? Website { get; set; }
    
    [JsonProperty("background", NullValueHandling = NullValueHandling.Ignore)]
    public string? Background { get; set; }
    
    [JsonProperty("currentCompany", NullValueHandling = NullValueHandling.Ignore)]
    public string? CurrentCompany { get; set; }
    
    [JsonProperty("currentCompanyLink", NullValueHandling = NullValueHandling.Ignore)]
    public string? CurrentCompanyLink { get; set; }
    
    [JsonProperty("currentSchool", NullValueHandling = NullValueHandling.Ignore)]
    public string? CurrentSchool { get; set; }
    
    [JsonProperty("currentSchoolLink", NullValueHandling = NullValueHandling.Ignore)]
    public string? CurrentSchoolLink { get; set; }
}