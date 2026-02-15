namespace Barsoft.SignalRHub.Application.DTOs;

/// <summary>
/// Login response için DTO
/// JWT token ve kullanıcı bilgisi içerir
/// </summary>
public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}
