namespace Barsoft.SignalRHub.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ connection configuration
/// Binds to "RabbitMQ" section in appsettings.json
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// RabbitMQ server hostname or IP
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ server port (default: 5672)
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ username
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ password
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ virtual host (default: /)
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Connection timeout in seconds (default: 30)
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Automatic recovery enabled (reconnect on connection loss)
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Network recovery interval in seconds (default: 10)
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Connection retry count on startup (default: 5)
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// Retry delay in seconds (default: 5)
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Validates the settings
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("RabbitMQ Host is required");

        if (Port <= 0 || Port > 65535)
            throw new InvalidOperationException("RabbitMQ Port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(Username))
            throw new InvalidOperationException("RabbitMQ Username is required");

        if (string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("RabbitMQ Password is required");

        if (string.IsNullOrWhiteSpace(VirtualHost))
            throw new InvalidOperationException("RabbitMQ VirtualHost is required");
    }
}
