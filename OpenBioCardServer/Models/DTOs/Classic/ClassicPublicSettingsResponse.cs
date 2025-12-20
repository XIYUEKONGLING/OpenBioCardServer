using System.Text.Json.Serialization;

namespace OpenBioCardServer.Models.DTOs.Classic;

public class ClassicPublicSettingsResponse
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("logo")]
    public string Logo { get; set; } = string.Empty;
}