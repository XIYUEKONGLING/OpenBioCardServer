namespace OpenBioCardServer.Configuration;

/// <summary>
/// 媒体资源配置
/// </summary>
public class AssetSettings
{
    public const string SectionName = "AssetSettings";
    
    /// <summary>
    /// 最大文件大小（字节）
    /// 默认：5MB
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// 最大文件大小（MB）
    /// </summary>
    public int MaxFileSizeMB
    {
        get => (int)(MaxFileSizeBytes / 1024 / 1024);
        set => MaxFileSizeBytes = value * 1024L * 1024L;
    }

    /// <summary>
    /// 允许的图片 MIME 类型
    /// </summary>
    public List<string> AllowedImageTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/svg+xml"
    };
    
}