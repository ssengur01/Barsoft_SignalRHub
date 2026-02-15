namespace Barsoft.SignalRHub.Application.DTOs;

/// <summary>
/// Login request için DTO
/// </summary>
public class LoginRequestDto
{
    public string UserCode { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
