using Barsoft.SignalRHub.Application.Interfaces;
using Barsoft.SignalRHub.Shared.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Barsoft.SignalRHub.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ message publisher implementation
/// Manages connection, channel, and publishes domain events
/// Thread-safe and handles automatic recovery
/// </summary>
public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly object _connectionLock = new();
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed;

    public RabbitMqPublisher(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqPublisher> logger)
    {
        _settings = settings.Value;
        _settings.Validate();
        _logger = logger;

        InitializeConnection();
    }

    /// <summary>
    /// Publishes an event to RabbitMQ exchange
    /// </summary>
    public async Task PublishAsync<TEvent>(
        string exchangeName,
        string routingKey,
        TEvent @event,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqPublisher));

        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        try
        {
            // Ensure connection and channel are healthy
            EnsureConnection();

            // Serialize event to JSON
            var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var body = Encoding.UTF8.GetBytes(message);

            // Set message properties
            var properties = _channel!.CreateBasicProperties();
            properties.Persistent = true; // Durable messages
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";
            properties.DeliveryMode = 2; // Persistent
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = typeof(TEvent).Name;

            // Publish message
            _channel.BasicPublish(
                exchange: exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published message to exchange: {Exchange}, routing key: {RoutingKey}, event type: {EventType}",
                exchangeName, routingKey, typeof(TEvent).Name);

            await Task.CompletedTask; // Make method async-compatible
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish message to exchange: {Exchange}, routing key: {RoutingKey}",
                exchangeName, routingKey);
            throw;
        }
    }

    /// <summary>
    /// Checks if RabbitMQ connection is healthy
    /// </summary>
    public Task<bool> IsHealthyAsync()
    {
        try
        {
            return Task.FromResult(_connection?.IsOpen == true && _channel?.IsOpen == true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Initializes RabbitMQ connection with retry logic
    /// </summary>
    private void InitializeConnection()
    {
        lock (_connectionLock)
        {
            var retryCount = 0;

            while (retryCount < _settings.RetryCount)
            {
                try
                {
                    _logger.LogInformation(
                        "Connecting to RabbitMQ: {Host}:{Port} (attempt {Attempt}/{MaxAttempts})",
                        _settings.Host, _settings.Port, retryCount + 1, _settings.RetryCount);

                    var factory = new ConnectionFactory
                    {
                        HostName = _settings.Host,
                        Port = _settings.Port,
                        UserName = _settings.Username,
                        Password = _settings.Password,
                        VirtualHost = _settings.VirtualHost,
                        AutomaticRecoveryEnabled = _settings.AutomaticRecoveryEnabled,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(_settings.NetworkRecoveryIntervalSeconds),
                        RequestedConnectionTimeout = TimeSpan.FromSeconds(_settings.ConnectionTimeoutSeconds),
                        DispatchConsumersAsync = true
                    };

                    _connection = factory.CreateConnection("Barsoft.DbWatcher");
                    _channel = _connection.CreateModel();

                    // Declare exchange
                    _channel.ExchangeDeclare(
                        exchange: RabbitMqConstants.StokExchangeName,
                        type: RabbitMqConstants.ExchangeSettings.Type,
                        durable: RabbitMqConstants.ExchangeSettings.Durable,
                        autoDelete: RabbitMqConstants.ExchangeSettings.AutoDelete);

                    // Declare queue
                    _channel.QueueDeclare(
                        queue: RabbitMqConstants.StokQueueName,
                        durable: RabbitMqConstants.QueueSettings.Durable,
                        exclusive: RabbitMqConstants.QueueSettings.Exclusive,
                        autoDelete: RabbitMqConstants.QueueSettings.AutoDelete,
                        arguments: new Dictionary<string, object>
                        {
                            { "x-message-ttl", RabbitMqConstants.QueueSettings.MessageTtlMs }
                        });

                    // Bind queue to exchange with routing patterns
                    _channel.QueueBind(
                        queue: RabbitMqConstants.StokQueueName,
                        exchange: RabbitMqConstants.StokExchangeName,
                        routingKey: RabbitMqConstants.RoutingKeys.StokHareketAll);

                    _logger.LogInformation("Successfully connected to RabbitMQ: {Host}:{Port}",
                        _settings.Host, _settings.Port);

                    // Setup connection event handlers
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    return;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex,
                        "Failed to connect to RabbitMQ (attempt {Attempt}/{MaxAttempts})",
                        retryCount, _settings.RetryCount);

                    if (retryCount >= _settings.RetryCount)
                    {
                        _logger.LogError("Failed to connect to RabbitMQ after {MaxAttempts} attempts",
                            _settings.RetryCount);
                        throw;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(_settings.RetryDelaySeconds));
                }
            }
        }
    }

    /// <summary>
    /// Ensures connection and channel are open, reconnects if needed
    /// </summary>
    private void EnsureConnection()
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            return;

        _logger.LogWarning("RabbitMQ connection lost, attempting to reconnect...");
        InitializeConnection();
    }

    #region Event Handlers

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", e.ReplyText);
    }

    private void OnCallbackException(object? sender, RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "RabbitMQ callback exception");
    }

    private void OnConnectionBlocked(object? sender, RabbitMQ.Client.Events.ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection blocked: {Reason}", e.Reason);
    }

    #endregion

    /// <summary>
    /// Disposes connection and channel
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing RabbitMQ publisher...");

        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ publisher");
        }
        finally
        {
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
