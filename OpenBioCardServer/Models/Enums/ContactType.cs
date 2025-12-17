using System.Text.Json.Serialization;

namespace OpenBioCardServer.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))] 
public enum ContactType
{
    Email,
    Phone,
    WeChat,
    QQ,
    WhatsApp,
    Telegram,
    Discord,
    Line,
    Wecom,
    Other,
}