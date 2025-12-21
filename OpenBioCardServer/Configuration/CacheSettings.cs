namespace OpenBioCardServer.Configuration;

public class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public bool Enabled { get; set; } = true;
    
    // Redis
    public bool UseRedis { get; set; } = false;
    public string? RedisConnectionString { get; set; }
    public string InstanceName { get; set; } = "OpenBioCard:";
    
    // Memory
    public long? CacheSizeLimit { get; set; }
    public double CompactionPercentage { get; set; } = 0.2;

    public int ExpirationMinutes { get; set; } = 30;
    public int SlidingExpirationMinutes { get; set; } = 5;
    
    // 工厂软超时 (毫秒)
    public int FactorySoftTimeoutMilliseconds { get; set; } = 500;

    // 故障兜底 (Fail-Safe) 配置
    public bool EnableFailSafe { get; set; } = true;
    public int FailSafeMaxDurationMinutes { get; set; } = 120;
    
    // 故障节流时间 (秒)
    public int FailSafeThrottleDurationSeconds { get; set; } = 30;

    // 分布式缓存熔断时间 (秒)
    public int DistributedCacheCircuitBreakerDurationSeconds { get; set; } = 2;
}