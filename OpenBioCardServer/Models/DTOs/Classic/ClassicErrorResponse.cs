using Newtonsoft.Json;

namespace OpenBioCardServer.Models.DTOs.Classic;

public class ClassicErrorResponse
{
    [JsonProperty("error")]
    public string Error { get; set; } = string.Empty;

    public ClassicErrorResponse()
    {
        
    }

    public ClassicErrorResponse(string error)
    {
        Error = error;
    }
}