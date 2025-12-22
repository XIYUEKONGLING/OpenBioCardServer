using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.Admin;

public class ClassicUserListResponse
{
    [JsonProperty("users")]
    public List<ClassicUserInfo> Users { get; set; } = new();
}