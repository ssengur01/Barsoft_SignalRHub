namespace Barsoft.SignalRHub.Application.DTOs;

/// <summary>
/// Kullanıcı bilgisi için Data Transfer Object
/// Login response için kullanılır (şifre HARİÇ!)
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<int> SubeIds { get; set; } = new();
    public string? Telefon { get; set; }

    /// <summary>
    /// Kullanıcıya tanımlı şube ID'lerini parse eder
    /// CSV format: "1,2,3" -> [1, 2, 3]
    /// </summary>
    public static List<int> ParseSubeIds(string? subeIdsString)
    {
        if (string.IsNullOrWhiteSpace(subeIdsString))
            return new List<int>();

        return subeIdsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
            .Where(x => x > 0)
            .ToList();
    }
}
