namespace OpenBioCardServer.Models.DTOs;

public class SignupRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Type { get; set; } = "user"; // user, admin
}