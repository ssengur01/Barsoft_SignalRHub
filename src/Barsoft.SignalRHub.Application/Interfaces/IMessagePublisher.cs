namespace Barsoft.SignalRHub.Application.Interfaces;

/// <summary>
/// RabbitMQ message publishing abstraction
/// DB Watcher servisinin domain event'lerini publish etmesi için kullanılır
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Exchange'e mesaj publish eder
    /// </summary>
    /// <typeparam name="TEvent">Event tipi (StokHareketCreatedEvent, etc.)</typeparam>
    /// <param name="exchangeName">Exchange adı</param>
    /// <param name="routingKey">Routing key (stok.hareket.created, etc.)</param>
    /// <param name="event">Publish edilecek event</param>
    /// <param name="cancellationToken">İptal token</param>
    Task PublishAsync<TEvent>(
        string exchangeName,
        string routingKey,
        TEvent @event,
        CancellationToken cancellationToken = default) where TEvent : class;

    /// <summary>
    /// Connection sağlık kontrolü
    /// </summary>
    Task<bool> IsHealthyAsync();
}
