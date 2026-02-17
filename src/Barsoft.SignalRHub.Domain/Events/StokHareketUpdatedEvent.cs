namespace Barsoft.SignalRHub.Domain.Events;

/// <summary>
/// Mevcut stok hareketi güncellendiğinde DB Watcher tarafından publish edilir
/// RabbitMQ routing key: stok.hareket.updated
/// </summary>
public class StokHareketUpdatedEvent
{
    public int Id { get; set; }
    public int StokId { get; set; }
    public string BelgeKodu { get; set; } = string.Empty;
    public DateTime BelgeTarihi { get; set; }
    public decimal Miktar { get; set; }
    public decimal BirimFiyati { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public string? Aciklama { get; set; }
    public int DepoId { get; set; }
    public int CreateUserId { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? ChangeDate { get; set; }
    public int? ChangeUserId { get; set; }

    /// <summary>
    /// SignalR Group filtreleme için
    /// </summary>
    public int? MasrafMerkeziId { get; set; }

    /// <summary>
    /// Event oluşturulma zamanı (UTC)
    /// </summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Event versiyonu (şema değişiklikleri için)
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Hangi alanların değiştiğini belirtir (opsiyonel)
    /// Örnek: "Miktar,ToplamTutar"
    /// </summary>
    public string? ChangedFields { get; set; }
}
