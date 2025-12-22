using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Admin;

public class ClassicCreateUserRequest
{
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
    
    [JsonProperty("newUsername")]
    public string NewUsername { get; set; } = string.Empty;
    
    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;
    
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
}