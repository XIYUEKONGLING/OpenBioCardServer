using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Auth;

public class ClassicTokenResponse
{
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
}