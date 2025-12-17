using System.Text.Json.Serialization;

namespace OpenBioCardServer.Models.Enums;

/// <summary>
/// 社交媒体平台类型
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))] 
public enum SocialPlatform
{
    Other = 0,
    Website,
    GitHub,
    HuggingFace,
    Twitter,
    Facebook,
    Instagram,
    Threads,
    Weibo,
    Xiaohongshu,
    YouTube,
    Bilibili,
    Steam,
    Spotify,
    QQMusic,
    NetEaseMusic,
    KuGouMusic,
}