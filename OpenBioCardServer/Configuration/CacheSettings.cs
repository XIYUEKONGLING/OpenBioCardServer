namespace OpenBioCardServer.Configuration;

public class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public bool Enabled { get; set; } = true;
    public bool UseRedis { get; set; } = false;
    public string? RedisConnectionString { get; set; }
    public string InstanceName { get; set; } = "OpenBioCard:";
    public long? CacheSizeLimit { get; set; }
    public int ExpirationMinutes { get; set; } = 30;
    public int SlidingExpirationMinutes { get; set; } = 5;
    public double CompactionPercentage { get; set; } = 0.2;
}