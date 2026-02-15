using Barsoft.SignalRHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barsoft.SignalRHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// User entity için EF Core Fluent API configuration
/// Maps to: TBL_USER_MAIN (15 columns)
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table mapping
        builder.ToTable("TBL_USER_MAIN");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd();

        // Authentication fields
        builder.Property(e => e.UserCode)
            .HasColumnName("USERCODE")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Password)
            .HasColumnName("PASSWORD")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(50)
            .IsRequired();

        // Authorization fields
        builder.Property(e => e.Admin)
            .HasColumnName("ADMIN")
            .IsRequired();

        builder.Property(e => e.Aktif)
            .HasColumnName("AKTIF")
            .IsRequired();

        // Audit fields
        builder.Property(e => e.CreateUserCode)
            .HasColumnName("CREATEUSERCODE")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ChangeDate)
            .HasColumnName("CHANGEDATE")
            .HasColumnType("smalldatetime");

        builder.Property(e => e.ChangeUserId)
            .HasColumnName("CHANGEUSERID");

        // Multi-tenant fields
        builder.Property(e => e.GrupId)
            .HasColumnName("GRUPID");

        builder.Property(e => e.IsProgramUpdate)
            .HasColumnName("ISPROGRAMUPDATE");

        builder.Property(e => e.Telefon)
            .HasColumnName("TELEFON")
            .HasMaxLength(50);

        // CSV access control lists (Multi-branch support)
        builder.Property(e => e.SubeIds)
            .HasColumnName("SUBEIDS")
            .HasColumnType("varchar(MAX)");

        builder.Property(e => e.KasaIds)
            .HasColumnName("KASAIDS")
            .HasColumnType("varchar(MAX)");

        builder.Property(e => e.BankaIds)
            .HasColumnName("BANKAIDS")
            .HasColumnType("varchar(MAX)");

        // Unique constraint on UserCode (login username)
        builder.HasIndex(e => e.UserCode)
            .IsUnique()
            .HasDatabaseName("UX_TBL_USER_MAIN_USERCODE");

        // Index for active user queries
        builder.HasIndex(e => e.Aktif)
            .HasDatabaseName("IX_TBL_USER_MAIN_AKTIF");

        // Composite index for authentication queries
        builder.HasIndex(e => new { e.UserCode, e.Aktif })
            .HasDatabaseName("IX_TBL_USER_MAIN_USERCODE_AKTIF");
    }
}
