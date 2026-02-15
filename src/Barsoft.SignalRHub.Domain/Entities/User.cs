namespace Barsoft.SignalRHub.Domain.Entities;

/// <summary>
/// TBL_USER_MAIN tablosunun domain entity mapping'i
/// Kullanıcı authentication ve authorization için kullanılır
/// </summary>
public class User
{
    public int Id { get; set; }
    public bool Aktif { get; set; } // bit
    public string UserCode { get; set; } = string.Empty; // varchar(100)
    public string Password { get; set; } = string.Empty; // varchar(20) - NOT: Hash'lenecek!
    public string Description { get; set; } = string.Empty; // varchar(50)
    public bool Admin { get; set; } // bit
    public string CreateUserCode { get; set; } = string.Empty; // varchar(50)
    public DateTime? ChangeDate { get; set; } // smalldatetime
    public int? ChangeUserId { get; set; }

    // Multi-tenant fields
    public int? GrupId { get; set; }
    public bool? IsProgramUpdate { get; set; } // bit
    public string? Telefon { get; set; } // varchar(50)

    // CSV şube/kasa/banka ID'leri (Multi-branch access)
    /// <summary>
    /// Kullanıcının erişebileceği şube ID'leri (CSV format: "1,2,3")
    /// SignalR Group filtreleme için kullanılır
    /// </summary>
    public string? SubeIds { get; set; } // varchar(MAX)
    public string? KasaIds { get; set; } // varchar(MAX)
    public string? BankaIds { get; set; } // varchar(MAX)
}
