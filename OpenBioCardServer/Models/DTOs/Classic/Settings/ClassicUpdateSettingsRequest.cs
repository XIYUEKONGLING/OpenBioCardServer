using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Settings;

public class ClassicUpdateSettingsRequest
{
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
    
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonProperty("logo")]
    public string Logo { get; set; } = string.Empty;
}