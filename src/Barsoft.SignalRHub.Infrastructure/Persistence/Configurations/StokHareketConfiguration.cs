using Barsoft.SignalRHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barsoft.SignalRHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// StokHareket entity için EF Core Fluent API configuration
/// Maps to: TBL_STOK_HAREKET (29 columns)
/// </summary>
public class StokHareketConfiguration : IEntityTypeConfiguration<StokHareket>
{
    public void Configure(EntityTypeBuilder<StokHareket> builder)
    {
        // Table mapping
        builder.ToTable("TBL_STOK_HAREKET");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd(); // IDENTITY column

        // Stok bilgileri
        builder.Property(e => e.StokId)
            .HasColumnName("STOKID")
            .IsRequired();

        builder.Property(e => e.IliskiliStokId)
            .HasColumnName("ILISKILISTOKID");

        // Hareket detayları
        builder.Property(e => e.HareketTipId)
            .HasColumnName("HAREKETTIPID")
            .IsRequired();

        builder.Property(e => e.BelgeId)
            .HasColumnName("BELGEID")
            .IsRequired();

        builder.Property(e => e.BelgeKodu)
            .HasColumnName("BELGEKODU")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.BelgeTarihi)
            .HasColumnName("BELGETARIHI")
            .HasColumnType("smalldatetime")
            .IsRequired();

        // Miktar ve birim
        builder.Property(e => e.Miktar)
            .HasColumnName("MIKTAR")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.BirimId)
            .HasColumnName("BIRIMID")
            .IsRequired();

        builder.Property(e => e.BirimCarpan)
            .HasColumnName("BIRIMCARPAN")
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(e => e.BirimFiyati)
            .HasColumnName("BIRIMFIYATI")
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        // Mali bilgiler
        builder.Property(e => e.DepoId)
            .HasColumnName("DEPOID")
            .IsRequired();

        builder.Property(e => e.Kdv)
            .HasColumnName("KDV")
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(e => e.DovizId)
            .HasColumnName("DOVIZID")
            .IsRequired();

        builder.Property(e => e.DovizTutari)
            .HasColumnName("DOVIZTUTARI")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.KdvTutari)
            .HasColumnName("KDVTUTARI")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.IndirimTutari)
            .HasColumnName("INDIRIMTUTARI")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.ArttirimTutari)
            .HasColumnName("ARTTIRIMTUTARI")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.ToplamTutar)
            .HasColumnName("TOPLAMTUTAR")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // İlişkili varlıklar (Nullable)
        builder.Property(e => e.CariId)
            .HasColumnName("CARIID");

        builder.Property(e => e.PlasiyerId)
            .HasColumnName("PLASIYERID");

        builder.Property(e => e.ReyonId)
            .HasColumnName("REYONID");

        builder.Property(e => e.MasrafMerkeziId)
            .HasColumnName("MASRAFMERKEZIID");

        // Detay ve açıklama
        builder.Property(e => e.DetayId)
            .HasColumnName("DETAYID")
            .IsRequired();

        builder.Property(e => e.Aciklama)
            .HasColumnName("ACIKLAMA")
            .HasMaxLength(100)
            .IsRequired();

        // Audit fields - Change Tracking için kritik!
        builder.Property(e => e.CreateDate)
            .HasColumnName("CREATEDATE")
            .HasColumnType("smalldatetime")
            .IsRequired();

        builder.Property(e => e.CreateUserId)
            .HasColumnName("CREATEUSERID")
            .IsRequired();

        builder.Property(e => e.ChangeDate)
            .HasColumnName("CHANGEDATE")
            .HasColumnType("smalldatetime");

        builder.Property(e => e.ChangeUserId)
            .HasColumnName("CHANGEUSERID");

        // Indexes
        // Composite index for incremental polling strategy
        builder.HasIndex(e => new { e.Id, e.ChangeDate })
            .HasDatabaseName("IX_TBL_STOK_HAREKET_ID_CHANGEDATE");

        // Index for CHANGEDATE queries (updates tracking)
        builder.HasIndex(e => e.ChangeDate)
            .HasDatabaseName("IX_TBL_STOK_HAREKET_CHANGEDATE");

        // Index for StokId lookups
        builder.HasIndex(e => e.StokId)
            .HasDatabaseName("IX_TBL_STOK_HAREKET_STOKID");

        // Index for MasrafMerkeziId (multi-tenant filtering)
        builder.HasIndex(e => e.MasrafMerkeziId)
            .HasDatabaseName("IX_TBL_STOK_HAREKET_MASRAFMERKEZIID");
    }
}
