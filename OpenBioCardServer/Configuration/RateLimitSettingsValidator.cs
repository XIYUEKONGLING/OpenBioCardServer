using Microsoft.Extensions.Options;

namespace OpenBioCardServer.Configuration;

public class RateLimitSettingsValidator : IValidateOptions<RateLimitSettings>
{
    public ValidateOptionsResult Validate(string? name, RateLimitSettings options)
    {
        var failures = new List<string>();
        
        if (string.IsNullOrWhiteSpace(options.PolicyName))
            failures.Add("PolicyName cannot be empty.");
            
        if (options.PermitLimit <= 0)
            failures.Add("PermitLimit must be greater than 0.");
            
        if (options.WindowMinutes <= 0)
            failures.Add("WindowMinutes must be greater than 0.");

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures) 
            : ValidateOptionsResult.Success;
    }
}