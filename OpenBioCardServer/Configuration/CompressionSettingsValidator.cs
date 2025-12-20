using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace OpenBioCardServer.Configuration;

public class CompressionSettingsValidator : IValidateOptions<CompressionSettings>
{
    public ValidateOptionsResult Validate(string? name, CompressionSettings options)
    {
        if (options.Enabled)
        {
            if (!Enum.TryParse<CompressionLevel>(options.Level, true, out _))
            {
                return ValidateOptionsResult.Fail($"Invalid Compression Level: {options.Level}. Valid values are: Optimal, Fastest, NoCompression, SmallestSize.");
            }
        }
        return ValidateOptionsResult.Success;
    }
}