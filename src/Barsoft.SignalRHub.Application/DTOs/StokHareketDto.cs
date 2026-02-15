namespace Barsoft.SignalRHub.Application.DTOs;

/// <summary>
/// Stok hareket verisi için Data Transfer Object
/// Client'lara gönderilecek veri yapısı
/// </summary>
public class StokHareketDto
{
    public int Id { get; set; }
    public int StokId { get; set; }
    public string BelgeKodu { get; set; } = string.Empty;
    public DateTime BelgeTarihi { get; set; }
    public decimal Miktar { get; set; }
    public decimal BirimFiyati { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime? ChangeDate { get; set; }

    // Filtreleme için
    public int? MasrafMerkeziId { get; set; }
    public int DepoId { get; set; }
}
