# SignalR Client Demo

Bu HTML dosyası, Barsoft SignalR Hub sistemini test etmek için interaktif bir client demo'sudur.

## Kullanım

### 1. Servisleri Başlat

**RabbitMQ:**
```bash
cd docker
docker-compose up -d rabbitmq
```

**DB Watcher Service:**
```bash
cd src/Barsoft.SignalRHub.DbWatcher
dotnet run
```

**SignalR Hub API:**
```bash
cd src/Barsoft.SignalRHub.SignalRHub
dotnet run
```

### 2. Demo Sayfasını Aç

Tarayıcıda açın:
```
file:///path/to/test/client-demo/signalr-demo.html
```

veya basit HTTP server ile:
```bash
cd test/client-demo
python -m http.server 8080
# http://localhost:8080/signalr-demo.html
```

### 3. Login

- **API URL:** `https://localhost:5001` (varsayılan)
- **User Code:** Test kullanıcı kodu (örn: `0001`)
- **Password:** Kullanıcı şifresi

"Login & Connect" butonuna tıklayın.

### 4. Real-time Events İzle

Login başarılı olduğunda:
- SignalR connection otomatik kurulur
- Kullanıcının grup üyelikleri gösterilir
- Database'de yapılan değişiklikler real-time olarak görüntülenir

## Özellikler

### Connection Management
- JWT authentication ile güvenli bağlantı
- Automatic reconnection (0, 2, 5, 10 saniye)
- Connection state tracking
- Connection ID gösterimi

### Multi-Tenant Filtering
- Kullanıcının `subeIds` claim'ine göre grup üyeliği
- Sadece yetkili şubelerin event'lerini alma
- Group membership debug bilgisi

### Event Handling
- **StokHareketCreated:** Yeni stok hareketleri
- **StokHareketUpdated:** Güncellenen stok hareketleri
- **StokHareketReceived:** Generic handler (her iki tip için)

### Real-time Log
- Timestamp ile detaylı event log
- Color-coded event types (Created: Yeşil, Updated: Sarı, Error: Kırmızı)
- JSON formatting ile event detail görüntüleme
- Son 100 event tutma (memory optimization)

### Control Panel
- **Ping Server:** Connection test (server timestamp döner)
- **Get My Groups:** Kullanıcının grup üyeliklerini göster
- **Clear Log:** Event log'u temizle
- **Disconnect:** SignalR connection'ı kapat

### Statistics
- Created Events sayacı
- Updated Events sayacı
- Total Events sayacı

## Test Senaryoları

### Scenario 1: Single User - Single Branch
1. User: `0001`, SubeIds: `[1]`
2. Database'de `MASRAFMERKEZIID = 1` olan kayıt ekle/güncelle
3. Event real-time görünmeli

### Scenario 2: Single User - Multiple Branches
1. User: `0002`, SubeIds: `[1, 2, 3]`
2. Database'de `MASRAFMERKEZIID = 2` olan kayıt ekle
3. Event görünmeli (user sube 2'ye erişebilir)
4. Database'de `MASRAFMERKEZIID = 5` olan kayıt ekle
5. Event görünmemeli (user sube 5'e erişemez)

### Scenario 3: Multiple Users - Branch Isolation
1. İki browser/tab aç
2. Tab 1: User `0001` (SubeIds: `[1]`)
3. Tab 2: User `0002` (SubeIds: `[2]`)
4. Database'de `MASRAFMERKEZIID = 1` ekle → Sadece Tab 1 görmeli
5. Database'de `MASRAFMERKEZIID = 2` ekle → Sadece Tab 2 görmeli

### Scenario 4: Admin User - All Branches
1. User: `admin` (SubeIds: `[1, 2, 3, 4, 5]`)
2. Herhangi bir şube için event eklense de görünmeli

### Scenario 5: Reconnection Test
1. Client'ı connect et
2. SignalR Hub service'i durdur (`Ctrl+C`)
3. Client "Reconnecting..." state'e geçmeli
4. Service'i tekrar başlat
5. Client otomatik reconnect olmalı
6. Event'ler yeniden akmaya başlamalı

## Browser Console

Detaylı debug için tarayıcı console'unu açın:
```
F12 → Console sekmesi
```

SignalR internal log'ları göreceksiniz:
- Connection negotiation
- WebSocket upgrade
- Message sending/receiving
- Reconnection attempts

## Troubleshooting

### CORS Hatası
Eğer "CORS policy" hatası alırsanız:
- `appsettings.Development.json` kontrol edin
- CORS policy "AllowAll" aktif mi kontrol edin
- Browser'ı incognito/private mode'da açmayı deneyin

### Self-Signed Certificate Hatası
HTTPS development sertifikası için:
```bash
dotnet dev-certs https --trust
```

### Connection Refused
- SignalR Hub API çalışıyor mu: `https://localhost:5001/health`
- Firewall bloğu var mı kontrol edin

### No Events Received
- DB Watcher Service çalışıyor mu kontrol edin
- RabbitMQ çalışıyor mu: `http://localhost:15672`
- Database'de değişiklik yapıldı mı kontrol edin
- User'ın doğru grup üyeliği var mı kontrol edin (Get My Groups)

## Advanced Testing

### Manuel Database Insert
```sql
INSERT INTO TBL_STOK_HAREKET (
    STOKID, BELGEKODU, BELGETARIHI, MIKTAR, TOPLAMTUTAR,
    CREATEDATE, CREATEUSERID, MASRAFMERKEZIID,
    BIRIMID, BIRIMCARPAN, BIRIMFIYATI, DEPOID, KDV, DOVIZID,
    DOVIZTUTARI, KDVTUTARI, INDIRIMTUTARI, ARTIRIMTUTARI,
    DETAYID, ACIKLAMA, HAREKETTIPID
)
VALUES (
    100, 'TEST-001', GETDATE(), 10.0, 1000.0,
    GETDATE(), 1, 1, -- MasrafMerkeziId = 1 (sube_1 group)
    1, 1.0, 100.0, 1, 18.0, 1,
    1000.0, 180.0, 0.0, 0.0,
    1, 'Test Stock Movement', 1
);
```

### Performance Test
Çoklu event'leri hızlıca eklemek için SQL script:
```sql
DECLARE @i INT = 0;
WHILE @i < 100
BEGIN
    INSERT INTO TBL_STOK_HAREKET (...) VALUES (...);
    SET @i = @i + 1;
    WAITFOR DELAY '00:00:00.100'; -- 100ms interval
END
```

Client demo'da event'lerin real-time akışını göreceksiniz!
