namespace Barsoft.SignalRHub.Domain.Entities;

/// <summary>
/// TBL_STOK_HAREKET tablosunun domain entity mapping'i
/// Stok hareketlerini (giriş/çıkış) temsil eder
/// </summary>
public class StokHareket
{
    // Primary Key
    public int Id { get; set; }

    // Stok bilgileri
    public int StokId { get; set; }
    public int? IliskiliStokId { get; set; }

    // Hareket detayları
    public int HareketTipId { get; set; }
    public int BelgeId { get; set; }
    public string BelgeKodu { get; set; } = string.Empty; // varchar(20)
    public DateTime BelgeTarihi { get; set; } // smalldatetime

    // Miktar ve birim
    public decimal Miktar { get; set; }
    public int BirimId { get; set; }
    public decimal BirimCarpan { get; set; }
    public decimal BirimFiyati { get; set; }

    // Mali bilgiler
    public int DepoId { get; set; }
    public decimal Kdv { get; set; }
    public int DovizId { get; set; }
    public decimal DovizTutari { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal IndirimTutari { get; set; }
    public decimal ArttirimTutari { get; set; }
    public decimal ToplamTutar { get; set; }

    // İlişkili varlıklar
    public int? CariId { get; set; }
    public int? PlasiyerId { get; set; }
    public int? ReyonId { get; set; }
    public int? MasrafMerkeziId { get; set; }

    // Detay ve açıklama
    public int DetayId { get; set; }
    public string Aciklama { get; set; } = string.Empty; // varchar(100)

    // Audit fields (Change Tracking için kritik!)
    public DateTime CreateDate { get; set; } // smalldatetime
    public int CreateUserId { get; set; }
    public DateTime? ChangeDate { get; set; } // smalldatetime
    public int? ChangeUserId { get; set; }
}
