namespace OpenBioCardServer.Configuration;

public class AuthSettings
{
    public const string SectionName = "AuthSettings";

    public string RootUsername { get; set; } = string.Empty;
    public string RootPassword { get; set; } = string.Empty;
}