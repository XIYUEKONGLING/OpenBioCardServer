using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Admin;

public class ClassicCreateUserResponse
{
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
}