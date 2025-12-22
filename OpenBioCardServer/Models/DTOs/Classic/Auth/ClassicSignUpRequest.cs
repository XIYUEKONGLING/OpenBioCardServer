using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Auth;

public class ClassicSignUpRequest
{
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;
    
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty; // "user" or "admin"
}