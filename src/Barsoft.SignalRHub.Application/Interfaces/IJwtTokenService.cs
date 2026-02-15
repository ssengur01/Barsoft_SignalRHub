using Barsoft.SignalRHub.Domain.Entities;

namespace Barsoft.SignalRHub.Application.Interfaces;

/// <summary>
/// JWT token oluşturma ve doğrulama servisi
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Kullanıcı için JWT token oluşturur
    /// Claims: sub (userId), userCode, userName, subeIds, isAdmin
    /// </summary>
    string GenerateToken(User user);

    /// <summary>
    /// Token'dan kullanıcı ID'sini çıkarır
    /// </summary>
    int? GetUserIdFromToken(string token);

    /// <summary>
    /// Token'ı doğrular
    /// </summary>
    bool ValidateToken(string token);
}
