namespace Barsoft.SignalRHub.Infrastructure.Security;

/// <summary>
/// JWT configuration settings
/// Binds to "JWT" section in appsettings.json
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JWT";

    /// <summary>
    /// Secret key for signing JWT tokens (min 32 characters)
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (iss claim)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience (aud claim)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes (default: 480 = 8 hours)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 480;

    /// <summary>
    /// Validates the settings
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret) || Secret.Length < 32)
            throw new InvalidOperationException("JWT Secret must be at least 32 characters long");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("JWT Issuer is required");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("JWT Audience is required");

        if (ExpirationMinutes <= 0)
            throw new InvalidOperationException("JWT ExpirationMinutes must be greater than 0");
    }
}
