using Microsoft.Extensions.Options;

namespace OpenBioCardServer.Configuration;

public class AuthSettingsValidator : IValidateOptions<AuthSettings>
{
    public ValidateOptionsResult Validate(string? name, AuthSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.RootUsername))
            failures.Add("RootUsername is required.");

        if (string.IsNullOrWhiteSpace(options.RootPassword))
            failures.Add("RootPassword is required.");
            
        if (!string.IsNullOrEmpty(options.RootPassword) && options.RootPassword.Length < 6)
            failures.Add("RootPassword must be at least 6 characters long.");

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures) 
            : ValidateOptionsResult.Success;
    }
}