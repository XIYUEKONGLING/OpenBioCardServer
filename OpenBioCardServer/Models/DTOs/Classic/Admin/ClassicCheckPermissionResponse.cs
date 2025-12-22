using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Admin;

public class ClassicCheckPermissionResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
}