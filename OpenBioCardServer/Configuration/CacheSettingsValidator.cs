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
        {
            failures.Add("CacheSizeLimit must be greater than 0.");
        }

        if (options.ExpirationMinutes <= 0)
        {
            failures.Add("ExpirationMinutes must be greater than 0.");
        }

        if (options.SlidingExpirationMinutes <= 0)
        {
            failures.Add("SlidingExpirationMinutes must be greater than 0.");
        }
        
        if (options.CompactionPercentage < 0 || options.CompactionPercentage > 1)
        {
            failures.Add("CompactionPercentage must be between 0 and 1.");
        }

        if (options.UseRedis && string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            failures.Add("RedisConnectionString cannot be empty when UseRedis is true.");
        }

        if (options.UseRedis && string.IsNullOrWhiteSpace(options.InstanceName))
        {
            failures.Add("InstanceName cannot be empty when UseRedis is true.");
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures) 
            : ValidateOptionsResult.Success;
    }
}