using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Auth;

public class ClassicDeleteRequest
{
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
}