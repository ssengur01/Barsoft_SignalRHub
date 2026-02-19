using Barsoft.SignalRHub.Infrastructure.Messaging;
using Barsoft.SignalRHub.Shared.Constants;
using Barsoft.SignalRHub.SignalRHub.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Barsoft.SignalRHub.SignalRHub.BackgroundServices;

/// <summary>
/// Background service that consumes messages from RabbitMQ
/// and broadcasts them to SignalR clients
/// Implements multi-tenant filtering via SignalR Groups
/// </summary>
public class RabbitMqConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IHubContext<StokHareketHub, IStokHareketHubClient> _hubContext;
    private readonly RabbitMqSettings _settings;
    private IConnection? _connection;
    private IModel? _channel;
    private EventingBasicConsumer? _consumer;

    public RabbitMqConsumerService(
        ILogger<RabbitMqConsumerService> logger,
        IHubContext<StokHareketHub, IStokHareketHubClient> hubContext,
        IOptions<RabbitMqSettings> settings)
    {
        _logger = logger;
        _hubContext = hubContext;
        _settings = settings.Value;
        _settings.Validate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Service starting...");

        // Wait for RabbitMQ to be ready
        await Task.Delay(3000, stoppingToken);

        try
        {
            InitializeRabbitMQ();
            StartConsuming(stoppingToken);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ Consumer Service stopping gracefully...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in RabbitMQ Consumer Service");
            throw;
        }
    }

    /// <summary>
    /// Initializes RabbitMQ connection and channel
    /// </summary>
    private void InitializeRabbitMQ()
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
                    DispatchConsumersAsync = false // Using synchronous EventingBasicConsumer
                };

                _connection = factory.CreateConnection("Barsoft.SignalRHub.Consumer");
                _channel = _connection.CreateModel();

                // Set QoS (prefetch count)
                _channel.BasicQos(
                    prefetchSize: 0,
                    prefetchCount: RabbitMqConstants.QueueSettings.PrefetchCount,
                    global: false);

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

    /// <summary>
    /// Starts consuming messages from RabbitMQ queue
    /// </summary>
    private void StartConsuming(CancellationToken stoppingToken)
    {
        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel is not initialized");

        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += (sender, eventArgs) =>
        {
            _logger.LogWarning(">>> EVENT HANDLER INVOKED! DeliveryTag: {DeliveryTag}, RoutingKey: {RoutingKey}",
                eventArgs.DeliveryTag, eventArgs.RoutingKey);

            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(">>> Cancellation requested, skipping message");
                return;
            }

            try
            {
                _logger.LogWarning(">>> About to call HandleMessageAsync...");
                // Use GetAwaiter().GetResult() to call async method synchronously
                HandleMessageAsync(eventArgs, stoppingToken).GetAwaiter().GetResult();
                _logger.LogWarning(">>> HandleMessageAsync completed successfully!");

                // Acknowledge message
                _logger.LogWarning(">>> About to ACK message...");
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                _logger.LogWarning(">>> Message ACK'd successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> EXCEPTION IN EVENT HANDLER! Error processing message. DeliveryTag: {DeliveryTag}", eventArgs.DeliveryTag);
                _logger.LogError(">>> Exception details: {Message}", ex.Message);
                _logger.LogError(">>> Stack trace: {StackTrace}", ex.StackTrace);

                // Negative acknowledge - requeue if transient error
                _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                _logger.LogWarning(">>> Message NACK'd");
            }
        };

        _channel.BasicConsume(
            queue: RabbitMqConstants.StokQueueName,
            autoAck: false,
            consumer: _consumer);

        _logger.LogInformation(
            "Started consuming from queue: {QueueName}, Prefetch: {PrefetchCount}",
            RabbitMqConstants.StokQueueName,
            RabbitMqConstants.QueueSettings.PrefetchCount);
    }

    /// <summary>
    /// Handles incoming message from RabbitMQ
    /// Deserializes and broadcasts to SignalR clients
    /// </summary>
    private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var body = eventArgs.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var routingKey = eventArgs.RoutingKey;

        _logger.LogDebug("Received message: RoutingKey={RoutingKey}, Size={Size} bytes",
            routingKey, body.Length);

        // Deserialize based on routing key
        if (routingKey == RabbitMqConstants.RoutingKeys.StokHareketCreated)
        {
            await HandleStokHareketCreatedAsync(message, cancellationToken);
        }
        else if (routingKey == RabbitMqConstants.RoutingKeys.StokHareketUpdated)
        {
            await HandleStokHareketUpdatedAsync(message, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
        }
    }

    /// <summary>
    /// Handles StokHareketCreated event
    /// </summary>
    private async Task HandleStokHareketCreatedAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<Dictionary<string, object>>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (@event == null)
            {
                _logger.LogWarning("Failed to deserialize StokHareketCreated event");
                return;
            }

            _logger.LogInformation("Broadcasting StokHareketCreated event: ID={Id}",
                @event.TryGetValue("id", out var id) ? id : "unknown");

            // Broadcast to specific group if MasrafMerkeziId exists
            if (@event.TryGetValue("masrafMerkeziId", out var masrafMerkeziIdObj) &&
                masrafMerkeziIdObj != null)
            {
                var masrafMerkeziId = masrafMerkeziIdObj.ToString();
                if (!string.IsNullOrEmpty(masrafMerkeziId) && masrafMerkeziId != "0")
                {
                    var groupName = $"sube_{masrafMerkeziId}";
                    await _hubContext.Clients.Group(groupName).StokHareketCreated(@event);
                    await _hubContext.Clients.Group(groupName).StokHareketReceived(@event);

                    _logger.LogDebug("Broadcasted to group: {GroupName}", groupName);
                    return;
                }
            }

            // Broadcast to all clients if no specific group
            await _hubContext.Clients.All.StokHareketCreated(@event);
            await _hubContext.Clients.All.StokHareketReceived(@event);

            _logger.LogDebug("Broadcasted to all clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling StokHareketCreated event");
            throw;
        }
    }

    /// <summary>
    /// Handles StokHareketUpdated event
    /// </summary>
    private async Task HandleStokHareketUpdatedAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<Dictionary<string, object>>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (@event == null)
            {
                _logger.LogWarning("Failed to deserialize StokHareketUpdated event");
                return;
            }

            _logger.LogInformation("Broadcasting StokHareketUpdated event: ID={Id}",
                @event.TryGetValue("id", out var id) ? id : "unknown");

            // Broadcast to specific group if MasrafMerkeziId exists
            if (@event.TryGetValue("masrafMerkeziId", out var masrafMerkeziIdObj) &&
                masrafMerkeziIdObj != null)
            {
                var masrafMerkeziId = masrafMerkeziIdObj.ToString();
                if (!string.IsNullOrEmpty(masrafMerkeziId) && masrafMerkeziId != "0")
                {
                    var groupName = $"sube_{masrafMerkeziId}";
                    await _hubContext.Clients.Group(groupName).StokHareketUpdated(@event);
                    await _hubContext.Clients.Group(groupName).StokHareketReceived(@event);

                    _logger.LogDebug("Broadcasted to group: {GroupName}", groupName);
                    return;
                }
            }

            // Broadcast to all clients if no specific group
            await _hubContext.Clients.All.StokHareketUpdated(@event);
            await _hubContext.Clients.All.StokHareketReceived(@event);

            _logger.LogDebug("Broadcasted to all clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling StokHareketUpdated event");
            throw;
        }
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ Consumer Service...");

        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping RabbitMQ Consumer Service");
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("RabbitMQ Consumer Service stopped");
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
