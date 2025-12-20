using Microsoft.Extensions.Options;

namespace OpenBioCardServer.Configuration;

public class CorsSettingsValidator : IValidateOptions<CorsSettings>
{
    public ValidateOptionsResult Validate(string? name, CorsSettings options)
    {
        if (options.AllowedOrigins == null)
        {
            return ValidateOptionsResult.Fail("AllowedOrigins cannot be null.");
        }
        return ValidateOptionsResult.Success;
    }
}