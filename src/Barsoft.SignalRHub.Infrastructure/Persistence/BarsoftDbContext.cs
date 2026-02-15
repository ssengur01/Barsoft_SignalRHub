using Barsoft.SignalRHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Barsoft.SignalRHub.Infrastructure.Persistence;

/// <summary>
/// Read-only DbContext for Barsoft SQL Server database
/// Uses Windows Authentication (Integrated Security)
/// No write operations allowed - throws exception on SaveChanges
/// </summary>
public class BarsoftDbContext : DbContext
{
    public BarsoftDbContext(DbContextOptions<BarsoftDbContext> options)
        : base(options)
    {
        // Change tracker optimizations for read-only scenarios
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    // DbSets
    public DbSet<StokHareket> StokHareketler => Set<StokHareket>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Additional global configurations
        ConfigureConventions(modelBuilder);
    }

    /// <summary>
    /// Global conventions for the model
    /// </summary>
    private void ConfigureConventions(ModelBuilder modelBuilder)
    {
        // Disable cascade delete globally (read-only DB, no FK actions needed)
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Default string column type to varchar instead of nvarchar (SQL Server optimization)
        // Already handled in individual configurations, but good practice
    }

    /// <summary>
    /// Override SaveChanges to prevent write operations
    /// This is a read-only database context
    /// </summary>
    public override int SaveChanges()
    {
        throw new InvalidOperationException(
            "BarsoftDbContext is read-only. Write operations are not allowed. " +
            "Data changes are tracked via polling mechanism in DB Watcher Service.");
    }

    /// <summary>
    /// Override SaveChangesAsync to prevent write operations
    /// This is a read-only database context
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "BarsoftDbContext is read-only. Write operations are not allowed. " +
            "Data changes are tracked via polling mechanism in DB Watcher Service.");
    }

    /// <summary>
    /// Optimized query for incremental polling
    /// Fetches new or changed StokHareket records since last check
    /// </summary>
    /// <param name="lastProcessedId">Last processed record ID</param>
    /// <param name="lastProcessedDate">Last processed CHANGEDATE</param>
    /// <param name="batchSize">Maximum records to fetch (default: 100)</param>
    /// <returns>List of new/changed records</returns>
    public async Task<List<StokHareket>> GetNewOrChangedStokHareketlerAsync(
        int lastProcessedId,
        DateTime lastProcessedDate,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        return await StokHareketler
            .Where(sh => sh.Id > lastProcessedId ||
                        (sh.ChangeDate != null && sh.ChangeDate > lastProcessedDate))
            .OrderBy(sh => sh.Id)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get user by UserCode for authentication
    /// </summary>
    /// <param name="userCode">User code (login username)</param>
    /// <returns>User entity or null</returns>
    public async Task<User?> GetUserByUserCodeAsync(
        string userCode,
        CancellationToken cancellationToken = default)
    {
        return await Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserCode == userCode && u.Aktif, cancellationToken);
    }
}
