using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.General;

public class ClassicSuccessResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    public ClassicSuccessResponse()
    {
        
    }

    public ClassicSuccessResponse(bool success)
    {
        Success = success;
    }
}