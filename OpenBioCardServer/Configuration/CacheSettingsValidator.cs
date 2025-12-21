using Microsoft.Extensions.Options;

namespace OpenBioCardServer.Configuration;

public class CacheSettingsValidator : IValidateOptions<CacheSettings>
{
    public ValidateOptionsResult Validate(string? name, CacheSettings options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.CacheSizeLimit <= 0)
            failures.Add("CacheSizeLimit must be greater than 0.");

        if (options.ExpirationMinutes <= 0)
            failures.Add("ExpirationMinutes must be greater than 0.");

        if (options.SlidingExpirationMinutes <= 0)
            failures.Add("SlidingExpirationMinutes must be greater than 0.");
        
        if (options.CompactionPercentage < 0 || options.CompactionPercentage > 1)
            failures.Add("CompactionPercentage must be between 0 and 1.");

        if (options.FactorySoftTimeoutMilliseconds <= 0)
            failures.Add("FactorySoftTimeoutMilliseconds must be greater than 0.");

        if (options.EnableFailSafe)
        {
            if (options.FailSafeMaxDurationMinutes <= 0)
                failures.Add("FailSafeMaxDurationMinutes must be greater than 0 when FailSafe is enabled.");
            
            if (options.FailSafeThrottleDurationSeconds <= 0)
                failures.Add("FailSafeThrottleDurationSeconds must be greater than 0 when FailSafe is enabled.");
        }

        if (options.DistributedCacheCircuitBreakerDurationSeconds <= 0)
            failures.Add("DistributedCacheCircuitBreakerDurationSeconds must be greater than 0.");

        if (options.UseRedis)
        {
            if (string.IsNullOrWhiteSpace(options.RedisConnectionString))
                failures.Add("RedisConnectionString cannot be empty when UseRedis is true.");

            if (string.IsNullOrWhiteSpace(options.InstanceName))
                failures.Add("InstanceName cannot be empty when UseRedis is true.");
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures) 
            : ValidateOptionsResult.Success;
    }
}
