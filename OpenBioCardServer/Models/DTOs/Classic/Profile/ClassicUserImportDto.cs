using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Profile;

public class ClassicUserImportDto
{
    [JsonProperty("user")]
    public ClassicUserExportDto User { get; set; } = new();

    [JsonProperty("profile")]
    public ClassicProfile Profile { get; set; } = new();
}