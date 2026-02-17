using Barsoft.SignalRHub.Domain.Entities;

namespace Barsoft.SignalRHub.Application.Interfaces;

/// <summary>
/// Stok hareket verilerini okumak için repository interface
/// READ-ONLY: Veritabanına yazma işlemi yapılmaz
/// </summary>
public interface IStokHareketRepository
{
    /// <summary>
    /// Son işlenen kayıttan sonraki yeni/değişen kayıtları getirir
    /// Incremental polling stratejisi için kullanılır
    /// </summary>
    /// <param name="lastProcessedId">Son işlenen kayıt ID</param>
    /// <param name="lastProcessedDate">Son işlenen kayıt tarihi</param>
    /// <param name="batchSize">Tek seferde getirilecek kayıt sayısı</param>
    /// <param name="cancellationToken">İptal token</param>
    Task<List<StokHareket>> GetChangedRecordsAsync(
        int lastProcessedId,
        DateTime lastProcessedDate,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ID'ye göre tekil kayıt getirir
    /// </summary>
    Task<StokHareket?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir tarih aralığındaki kayıtları getirir
    /// </summary>
    Task<List<StokHareket>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// En son N adet kaydı getirir (ID'ye göre azalan sırada)
    /// Dashboard'da son hareketleri göstermek için kullanılır
    /// </summary>
    /// <param name="count">Getirilecek kayıt sayısı</param>
    /// <param name="subeIds">Şube ID filtreleme (multi-tenant için)</param>
    /// <param name="cancellationToken">İptal token</param>
    Task<List<StokHareket>> GetRecentAsync(
        int count = 10,
        IEnumerable<int>? subeIds = null,
        CancellationToken cancellationToken = default);
}
