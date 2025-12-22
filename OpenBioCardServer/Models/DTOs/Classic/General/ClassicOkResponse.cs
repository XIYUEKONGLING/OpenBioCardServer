using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic.General;

public class ClassicOkResponse
{
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    public ClassicOkResponse()
    {
        
    }

    public ClassicOkResponse(string message)
    {
        Message = message;
    }
}