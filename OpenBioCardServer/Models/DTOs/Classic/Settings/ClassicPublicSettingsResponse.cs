using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Settings;

public class ClassicPublicSettingsResponse
{
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonProperty("logo")]
    public string Logo { get; set; } = string.Empty;
}