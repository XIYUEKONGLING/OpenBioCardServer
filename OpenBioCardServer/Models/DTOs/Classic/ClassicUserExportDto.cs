using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic;

public class ClassicUserExportDto
{
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
}