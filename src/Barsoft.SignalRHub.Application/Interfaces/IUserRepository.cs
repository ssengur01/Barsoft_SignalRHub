using Barsoft.SignalRHub.Domain.Entities;

namespace Barsoft.SignalRHub.Application.Interfaces;

/// <summary>
/// Kullanıcı verilerini okumak için repository interface
/// Authentication ve authorization için kullanılır
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Kullanıcı koduna göre kullanıcı getirir
    /// Login işlemi için kullanılır
    /// </summary>
    Task<User?> GetByUserCodeAsync(string userCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// ID'ye göre kullanıcı getirir
    /// </summary>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktif kullanıcıları listeler
    /// </summary>
    Task<List<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
}
