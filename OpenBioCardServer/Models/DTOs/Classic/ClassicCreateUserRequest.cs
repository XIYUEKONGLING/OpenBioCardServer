using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic;

public class ClassicCreateUserRequest
{
    [JsonProperty("username")]
    public string UserName { get; set; } = string.Empty;
    
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
    
    [JsonProperty("newUsername")]
    public string NewUserName { get; set; } = string.Empty;
    
    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;
    
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
}