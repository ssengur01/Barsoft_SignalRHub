using Barsoft.SignalRHub.Application.Interfaces;
using Barsoft.SignalRHub.Domain.Entities;
using Barsoft.SignalRHub.Domain.Events;
using Barsoft.SignalRHub.Domain.ValueObjects;
using Barsoft.SignalRHub.Infrastructure.Persistence;
using Barsoft.SignalRHub.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Barsoft.SignalRHub.DbWatcher.Workers;

/// <summary>
/// Background service that monitors database for changes
/// Uses adaptive incremental polling strategy (1-10 seconds)
/// Publishes domain events to RabbitMQ
/// </summary>
public class ChangeDetectionWorker : BackgroundService
{
    private readonly ILogger<ChangeDetectionWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ChangeTrackingInfo _trackingInfo;

    // Adaptive polling configuration
    private const int MinIntervalMs = 1000;  // 1 second when data found
    private const int MaxIntervalMs = 10000; // 10 seconds when no data
    private const int BatchSize = 100;       // Max records per query
    private const int IdleThreshold = 3;     // Consecutive empty queries before slowing down

    private int _consecutiveEmptyQueries = 0;

    public ChangeDetectionWorker(
        ILogger<ChangeDetectionWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _trackingInfo = new ChangeTrackingInfo
        {
            LastProcessedId = 0,
            LastProcessedDate = new DateTime(1900, 1, 1), // SQL Server safe minimum date
            CurrentIntervalMs = MinIntervalMs,
            LastQueryTime = DateTime.UtcNow
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChangeDetectionWorker starting...");

        // Wait for dependencies to initialize
        await Task.Delay(2000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessChangesAsync(stoppingToken);

                // Adaptive delay based on data availability
                await Task.Delay(_trackingInfo.CurrentIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ChangeDetectionWorker stopping gracefully...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in change detection loop");
                // Slow down on errors
                await Task.Delay(MaxIntervalMs, stoppingToken);
            }
        }

        _logger.LogInformation("ChangeDetectionWorker stopped");
    }

    /// <summary>
    /// Processes database changes and publishes events
    /// </summary>
    private async Task ProcessChangesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BarsoftDbContext>();
        var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        try
        {
            // Query new or changed records
            var changes = await GetNewOrChangedRecordsAsync(dbContext, cancellationToken);

            if (changes.Count == 0)
            {
                _consecutiveEmptyQueries++;
                AdjustPollingInterval(hasData: false);

                if (_consecutiveEmptyQueries % 10 == 0)
                {
                    _logger.LogDebug(
                        "No changes detected. Last ID: {LastId}, Last Date: {LastDate}, Interval: {Interval}ms",
                        _trackingInfo.LastProcessedId,
                        _trackingInfo.LastProcessedDate,
                        _trackingInfo.CurrentIntervalMs);
                }

                return;
            }

            _logger.LogInformation(
                "Detected {Count} changes (IDs: {MinId}-{MaxId})",
                changes.Count,
                changes.Min(c => c.Id),
                changes.Max(c => c.Id));

            // Process each change
            foreach (var change in changes)
            {
                await PublishChangeEventAsync(messagePublisher, change, cancellationToken);

                // Update tracking info
                if (change.Id > _trackingInfo.LastProcessedId)
                {
                    _trackingInfo.LastProcessedId = change.Id;
                }

                if (change.ChangeDate.HasValue && change.ChangeDate > _trackingInfo.LastProcessedDate)
                {
                    _trackingInfo.LastProcessedDate = change.ChangeDate.Value;
                }
            }

            _trackingInfo.LastQueryRecordCount = changes.Count;
            _trackingInfo.LastQueryTime = DateTime.UtcNow;
            _consecutiveEmptyQueries = 0;

            AdjustPollingInterval(hasData: true);

            _logger.LogInformation(
                "Processed {Count} changes. New tracking: ID={LastId}, Date={LastDate}",
                changes.Count,
                _trackingInfo.LastProcessedId,
                _trackingInfo.LastProcessedDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing changes");
            throw;
        }
    }

    /// <summary>
    /// Queries database for new or changed records using incremental strategy
    /// </summary>
    private async Task<List<StokHareket>> GetNewOrChangedRecordsAsync(
        BarsoftDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.StokHareketler
            .AsNoTracking()
            .Where(sh =>
                sh.Id > _trackingInfo.LastProcessedId ||
                (sh.ChangeDate != null && sh.ChangeDate > _trackingInfo.LastProcessedDate))
            .OrderBy(sh => sh.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Publishes domain event to RabbitMQ
    /// Determines if record is new (created) or updated
    /// </summary>
    private async Task PublishChangeEventAsync(
        IMessagePublisher publisher,
        StokHareket record,
        CancellationToken cancellationToken)
    {
        try
        {
            // Determine if this is a new record or update
            bool isNewRecord = record.Id > _trackingInfo.LastProcessedId &&
                              (record.ChangeDate == null || record.ChangeDate == record.CreateDate);

            if (isNewRecord)
            {
                // Publish Created event
                var createdEvent = new StokHareketCreatedEvent
                {
                    Id = record.Id,
                    StokId = record.StokId,
                    BelgeKodu = record.BelgeKodu,
                    BelgeTarihi = record.BelgeTarihi,
                    Miktar = record.Miktar,
                    BirimFiyati = record.BirimFiyati,
                    ToplamTutar = record.ToplamTutar,
                    KdvTutari = record.KdvTutari,
                    Aciklama = record.Aciklama,
                    DepoId = record.DepoId,
                    CreateUserId = record.CreateUserId,
                    CreateDate = record.CreateDate,
                    ChangeDate = record.ChangeDate,
                    MasrafMerkeziId = record.MasrafMerkeziId,
                    EventTimestamp = DateTime.UtcNow,
                    Version = "1.0"
                };

                await publisher.PublishAsync(
                    RabbitMqConstants.StokExchangeName,
                    RabbitMqConstants.RoutingKeys.StokHareketCreated,
                    createdEvent,
                    cancellationToken);

                _logger.LogDebug("Published Created event for ID: {Id}", record.Id);
            }
            else
            {
                // Publish Updated event
                var updatedEvent = new StokHareketUpdatedEvent
                {
                    Id = record.Id,
                    StokId = record.StokId,
                    BelgeKodu = record.BelgeKodu,
                    BelgeTarihi = record.BelgeTarihi,
                    Miktar = record.Miktar,
                    BirimFiyati = record.BirimFiyati,
                    ToplamTutar = record.ToplamTutar,
                    KdvTutari = record.KdvTutari,
                    Aciklama = record.Aciklama,
                    DepoId = record.DepoId,
                    CreateUserId = record.CreateUserId,
                    CreateDate = record.CreateDate,
                    ChangeUserId = record.ChangeUserId,
                    ChangeDate = record.ChangeDate ?? DateTime.UtcNow,
                    MasrafMerkeziId = record.MasrafMerkeziId,
                    EventTimestamp = DateTime.UtcNow,
                    Version = "1.0"
                };

                await publisher.PublishAsync(
                    RabbitMqConstants.StokExchangeName,
                    RabbitMqConstants.RoutingKeys.StokHareketUpdated,
                    updatedEvent,
                    cancellationToken);

                _logger.LogDebug("Published Updated event for ID: {Id}", record.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event for record ID: {Id}", record.Id);
            throw;
        }
    }

    /// <summary>
    /// Adjusts polling interval based on data availability (adaptive strategy)
    /// </summary>
    private void AdjustPollingInterval(bool hasData)
    {
        if (hasData)
        {
            // Data found: speed up polling
            _trackingInfo.CurrentIntervalMs = MinIntervalMs;
        }
        else if (_consecutiveEmptyQueries >= IdleThreshold)
        {
            // No data for multiple queries: slow down polling
            _trackingInfo.CurrentIntervalMs = MaxIntervalMs;
        }
        else
        {
            // Gradual slowdown
            _trackingInfo.CurrentIntervalMs = Math.Min(
                _trackingInfo.CurrentIntervalMs + 1000,
                MaxIntervalMs);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ChangeDetectionWorker stopping. Final stats: Processed up to ID={LastId}, Date={LastDate}",
            _trackingInfo.LastProcessedId,
            _trackingInfo.LastProcessedDate);

        await base.StopAsync(cancellationToken);
    }
}
