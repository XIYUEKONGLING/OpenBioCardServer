namespace OpenBioCardServer.Configuration;

public class RateLimitSettings
{
    public const string SectionName = "RateLimitSettings";

    public string PolicyName { get; set; } = "login";
    public int PermitLimit { get; set; } = 10;
    public int WindowMinutes { get; set; } = 1;
    public int QueueLimit { get; set; } = 0;
}