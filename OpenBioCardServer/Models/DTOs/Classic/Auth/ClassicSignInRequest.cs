using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Auth;

public class ClassicSignInRequest
{
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;
}