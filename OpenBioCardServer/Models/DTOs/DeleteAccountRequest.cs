namespace OpenBioCardServer.Models.DTOs;

public class DeleteAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}