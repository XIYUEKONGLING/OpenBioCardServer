namespace OpenBioCardServer.Models.DTOs;

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewUsername { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Type { get; set; } = "user";
}
