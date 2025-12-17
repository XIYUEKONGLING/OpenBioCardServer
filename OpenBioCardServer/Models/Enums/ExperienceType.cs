using System.Text.Json.Serialization;

namespace OpenBioCardServer.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))] 
public enum ExperienceType
{
    Work,
    Education,
    Volunteering
}