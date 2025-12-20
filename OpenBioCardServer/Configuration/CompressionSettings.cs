using System.IO.Compression;

namespace OpenBioCardServer.Configuration;

public class CompressionSettings
{
    public const string SectionName = "CompressionSettings";
    
    public bool Enabled { get; set; } = true;
    public bool EnableForHttps { get; set; } = true;
    public string Level { get; set; } = "Fastest";

    public CompressionLevel GetCompressionLevel()
    {
        return Enum.TryParse<CompressionLevel>(Level, true, out var result) 
            ? result 
            : CompressionLevel.Fastest;
    }
}