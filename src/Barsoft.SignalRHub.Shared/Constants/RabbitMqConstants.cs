namespace Barsoft.SignalRHub.Shared.Constants;

/// <summary>
/// RabbitMQ konfigürasyon sabitleri
/// Exchange, queue, routing key tanımları
/// </summary>
public static class RabbitMqConstants
{
    /// <summary>
    /// Exchange adı (Topic type)
    /// </summary>
    public const string StokExchangeName = "barsoft.stok.exchange";

    /// <summary>
    /// Queue adı (Durable)
    /// </summary>
    public const string StokQueueName = "barsoft.stok.queue";

    /// <summary>
    /// Routing keys
    /// </summary>
    public static class RoutingKeys
    {
        public const string StokHareketCreated = "stok.hareket.created";
        public const string StokHareketUpdated = "stok.hareket.updated";
        public const string StokHareketDeleted = "stok.hareket.deleted";

        /// <summary>
        /// Tüm stok hareket event'lerini dinlemek için wildcard
        /// </summary>
        public const string StokHareketAll = "stok.hareket.*";
    }

    /// <summary>
    /// Queue ayarları
    /// </summary>
    public static class QueueSettings
    {
        /// <summary>
        /// Queue dayanıklılığı (RabbitMQ restart'ta korunur)
        /// </summary>
        public const bool Durable = true;

        /// <summary>
        /// Bağlantı kesilince queue silinmez
        /// </summary>
        public const bool AutoDelete = false;

        /// <summary>
        /// Sadece bu bağlantıya özel değil
        /// </summary>
        public const bool Exclusive = false;

        /// <summary>
        /// Mesaj TTL (Time To Live) - 1 saat
        /// </summary>
        public const int MessageTtlMs = 3600000; // 1 hour

        /// <summary>
        /// Consumer başına kaç mesaj alacak (prefetch)
        /// </summary>
        public const ushort PrefetchCount = 10;
    }

    /// <summary>
    /// Exchange ayarları
    /// </summary>
    public static class ExchangeSettings
    {
        /// <summary>
        /// Exchange tipi (Topic: routing pattern matching)
        /// </summary>
        public const string Type = "topic";

        /// <summary>
        /// Exchange dayanıklılığı
        /// </summary>
        public const bool Durable = true;

        /// <summary>
        /// Auto-delete kapalı
        /// </summary>
        public const bool AutoDelete = false;
    }
}
