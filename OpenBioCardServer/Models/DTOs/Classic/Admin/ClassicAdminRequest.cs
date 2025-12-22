using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Admin;

public class ClassicAdminRequest
{
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
}