using Microsoft.Extensions.Options;

namespace OpenBioCardServer.Configuration;

public class DatabaseSettingsValidator : IValidateOptions<DatabaseSettings>
{
    public ValidateOptionsResult Validate(string? name, DatabaseSettings settings)
    {
        if (settings == null)
            return ValidateOptionsResult.Fail("DatabaseSettings cannot be null");
        
        if (string.IsNullOrWhiteSpace(settings.Type))
            return ValidateOptionsResult.Fail("DatabaseSettings.Type is required");
        
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            return ValidateOptionsResult.Fail("DatabaseSettings.ConnectionString is required");
        
        var validTypes = new[] { "SQLite", "PgSQL", "MySQL" };
        if (!validTypes.Contains(settings.Type, StringComparer.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail(
                $"DatabaseSettings.Type must be one of: {string.Join(", ", validTypes)}. Current value: {settings.Type}");
        
        return ValidateOptionsResult.Success;
    }
}