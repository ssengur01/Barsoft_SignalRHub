namespace Barsoft.SignalRHub.Infrastructure.Security;

/// <summary>
/// Password hashing and verification using BCrypt
/// </summary>
public class PasswordHasher
{
    /// <summary>
    /// Hashes a plain text password using BCrypt
    /// Work factor: 12 (good balance between security and performance)
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verifies a plain text password against a BCrypt hash
    /// Returns true if password matches
    /// </summary>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            // If hash is invalid format, return false
            return false;
        }
    }

    /// <summary>
    /// Checks if a password needs rehashing (e.g., work factor changed)
    /// </summary>
    public static bool NeedsRehash(string hashedPassword, int workFactor = 12)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return true;

        try
        {
            return BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, workFactor);
        }
        catch
        {
            return true;
        }
    }
}
