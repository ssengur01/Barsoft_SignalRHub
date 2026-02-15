using Barsoft.SignalRHub.Application.Interfaces;
using Barsoft.SignalRHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barsoft.SignalRHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// User repository implementation using EF Core
/// Read-only repository for authentication and authorization
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly BarsoftDbContext _context;

    public UserRepository(BarsoftDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get user by user code (login username)
    /// Only returns active users
    /// </summary>
    public async Task<User?> GetByUserCodeAsync(string userCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userCode))
            return null;

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.UserCode == userCode && u.Aktif,
                cancellationToken);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <summary>
    /// Get all active users
    /// </summary>
    public async Task<List<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Aktif)
            .OrderBy(u => u.UserCode)
            .ToListAsync(cancellationToken);
    }
}
