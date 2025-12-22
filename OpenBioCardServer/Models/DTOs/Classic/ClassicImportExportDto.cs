using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic;

public class ClassicImportExportDto
{
    [JsonProperty("user")]
    public ClassicUserExportDto User { get; set; } = new();

    [JsonProperty("profile")]
    public ClassicProfile Profile { get; set; } = new();
}