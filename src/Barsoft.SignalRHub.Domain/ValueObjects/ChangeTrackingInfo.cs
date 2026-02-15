namespace Barsoft.SignalRHub.Domain.ValueObjects;

/// <summary>
/// DB Watcher servisinin son işlediği kayıt bilgisini tutar
/// Incremental polling stratejisi için kullanılır
/// </summary>
public class ChangeTrackingInfo
{
    /// <summary>
    /// Son işlenen kaydın ID'si
    /// Sonraki sorgu: WHERE ID > LastProcessedId
    /// </summary>
    public int LastProcessedId { get; set; }

    /// <summary>
    /// Son işlenen kaydın değişiklik tarihi
    /// Sonraki sorgu: WHERE CHANGEDATE > LastProcessedDate
    /// </summary>
    public DateTime LastProcessedDate { get; set; }

    /// <summary>
    /// Polling interval'ı (milisaniye)
    /// Adaptive: 1000-10000ms arası dinamik ayarlanır
    /// </summary>
    public int CurrentIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Son sorgu zamanı
    /// </summary>
    public DateTime LastQueryTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Son sorguda bulunan kayıt sayısı
    /// Adaptive interval için kullanılır
    /// </summary>
    public int LastQueryRecordCount { get; set; }
}
