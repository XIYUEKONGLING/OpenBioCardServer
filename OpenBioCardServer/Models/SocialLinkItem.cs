namespace OpenBioCardServer.Models;

public class SocialLinkItem {
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public object? GithubData { get; set; } 
}