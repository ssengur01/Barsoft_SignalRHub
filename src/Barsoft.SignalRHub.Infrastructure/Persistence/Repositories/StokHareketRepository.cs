using Barsoft.SignalRHub.Application.Interfaces;
using Barsoft.SignalRHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barsoft.SignalRHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// Stok hareket repository implementation
/// READ-ONLY: No write operations
/// </summary>
public class StokHareketRepository : IStokHareketRepository
{
    private readonly BarsoftDbContext _context;

    public StokHareketRepository(BarsoftDbContext context)
    {
        _context = context;
    }

    public async Task<List<StokHareket>> GetChangedRecordsAsync(
        int lastProcessedId,
        DateTime lastProcessedDate,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.StokHareketler
            .Where(x => x.Id > lastProcessedId ||
                       (x.ChangeDate != null && x.ChangeDate > lastProcessedDate))
            .OrderBy(x => x.Id)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<StokHareket?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.StokHareketler
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<StokHareket>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.StokHareketler
            .Where(x => x.BelgeTarihi >= startDate && x.BelgeTarihi <= endDate)
            .OrderByDescending(x => x.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StokHareket>> GetRecentAsync(
        int count = 10,
        IEnumerable<int>? subeIds = null,
        CancellationToken cancellationToken = default)
    {
        // Get all records first, then filter in memory to avoid EF Core OPENJSON translation issues
        var allRecords = await _context.StokHareketler
            .OrderByDescending(x => x.Id)
            .Take(count * 10) // Get more than needed to ensure we have enough after filtering
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Apply filtering in memory if subeIds provided
        if (subeIds != null && subeIds.Any())
        {
            var subeIdsList = subeIds.ToHashSet();
            allRecords = allRecords
                .Where(x => subeIdsList.Contains(x.DepoId))
                .Take(count)
                .ToList();
        }
        else
        {
            allRecords = allRecords.Take(count).ToList();
        }

        return allRecords;
    }
}
