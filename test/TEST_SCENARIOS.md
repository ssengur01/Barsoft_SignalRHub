# Barsoft SignalR Hub - Test Scenarios

Bu dokümanda sistemin tüm özelliklerini test etmek için senaryolar bulunmaktadır.

## Prerequisites

Tüm test senaryoları için önce servisleri başlatın:

```bash
# 1. RabbitMQ
cd docker && docker-compose up -d rabbitmq

# 2. DB Watcher Service
cd src/Barsoft.SignalRHub.DbWatcher && dotnet run

# 3. SignalR Hub API
cd src/Barsoft.SignalRHub.SignalRHub && dotnet run
```

**Test URL'leri:**
- SignalR Hub API: `https://localhost:5001`
- RabbitMQ Management: `http://localhost:15672` (admin/admin123)
- Health Check: `https://localhost:5001/health`

---

## 1. Authentication & Authorization Tests

### 1.1 Valid Login
**Amaç:** JWT token başarılı alınabilmeli

**Adımlar:**
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userCode": "0001", "password": "password"}'
```

**Beklenen Sonuç:**
- HTTP 200 OK
- Response içinde `token`, `user`, `expiresAt` alanları
- `user.subeIds` array dolu

### 1.2 Invalid Credentials
**Amaç:** Hatalı şifrede login başarısız olmalı

**Adımlar:**
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userCode": "0001", "password": "wrongpassword"}'
```

**Beklenen Sonuç:**
- HTTP 401 Unauthorized
- Error message: "Invalid credentials"

### 1.3 Inactive User
**Amaç:** Pasif kullanıcı login yapamamalı

**Adımlar:**
1. Database'de user kaydı oluştur: `AKTIF = 0`
2. Login dene

**Beklenen Sonuç:**
- HTTP 401 Unauthorized
- Error message: "User account is inactive"

### 1.4 SignalR Hub Without Token
**Amaç:** Token olmadan SignalR'a bağlanma başarısız olmalı

**Adımlar:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:5001/hubs/stokhareket")
    .build();

await connection.start(); // Fail
```

**Beklenen Sonuç:**
- Connection failed: 401 Unauthorized

---

## 2. SignalR Connection Tests

### 2.1 Successful Connection
**Amaç:** Token ile SignalR bağlantısı başarılı olmalı

**Adımlar:**
1. Login yap, JWT token al
2. SignalR client'ta token kullan
3. Connect

**Beklenen Sonuç:**
- Connection state: Connected
- ConnectionId alınmalı
- `OnConnectedAsync` tetiklenmeli (server log)
- User gruplarına join edilmeli (server log)

### 2.2 Group Membership Verification
**Amaç:** User doğru gruplara join edilmeli

**Adımlar:**
1. User login: SubeIds = [1, 2, 3]
2. SignalR connect
3. Hub method çağır: `GetMyGroups()`

**Beklenen Sonuç:**
```json
{
  "userCode": "0001",
  "subeIds": [1, 2, 3],
  "groups": ["sube_1", "sube_2", "sube_3"],
  "connectionId": "xxx"
}
```

### 2.3 Ping Test
**Amaç:** Hub method çağrısı çalışmalı

**Adımlar:**
```javascript
const result = await connection.invoke("Ping");
console.log(result);
```

**Beklenen Sonuç:**
- Response: `"Pong from server at 2026-02-15T..."`

### 2.4 Reconnection After Disconnect
**Amaç:** Network kesintisinde automatic reconnection

**Adımlar:**
1. Client connect et
2. SignalR Hub service'i durdur
3. 5 saniye bekle
4. Service'i tekrar başlat

**Beklenen Sonuç:**
- Client state: Reconnecting (sarı)
- Automatic reconnect (0, 2, 5, 10s intervals)
- Connection restored
- Event'ler yeniden akmaya başlar

---

## 3. Real-time Event Broadcasting Tests

### 3.1 StokHareketCreated Event
**Amaç:** Yeni kayıt eklendiğinde event gelmeli

**Adımlar:**
1. Client connect (User SubeIds: [1])
2. Database'de INSERT:
```sql
INSERT INTO TBL_STOK_HAREKET (
    STOKID, BELGEKODU, BELGETARIHI, MIKTAR, TOPLAMTUTAR,
    CREATEDATE, CREATEUSERID, MASRAFMERKEZIID,
    BIRIMID, BIRIMCARPAN, BIRIMFIYATI, DEPOID, KDV, DOVIZID,
    DOVIZTUTARI, KDVTUTARI, INDIRIMTUTARI, ARTIRIMTUTARI,
    DETAYID, ACIKLAMA, HAREKETTIPID
)
VALUES (
    100, 'TEST-CREATED', GETDATE(), 10.0, 1000.0,
    GETDATE(), 1, 1, -- MasrafMerkeziId = 1
    1, 1.0, 100.0, 1, 18.0, 1,
    1000.0, 180.0, 0.0, 0.0,
    1, 'Created Event Test', 1
);
```

**Beklenen Sonuç:**
- DB Watcher: Change detected log
- RabbitMQ: Message published (routing: stok.hareket.created)
- Consumer: Message received log
- SignalR Client: `StokHareketCreated` event received
- Event log'da yeşil renk ile gösterilmeli

**Timing:**
- Max 10 saniye içinde (DB Watcher adaptive polling)

### 3.2 StokHareketUpdated Event
**Amaç:** Kayıt güncellendiğinde event gelmeli

**Adımlar:**
1. Client connect
2. Mevcut kaydı UPDATE:
```sql
UPDATE TBL_STOK_HAREKET
SET MIKTAR = 20.0,
    TOPLAMTUTAR = 2000.0,
    CHANGEDATE = GETDATE(),
    CHANGEUSERID = 2
WHERE ID = 100;
```

**Beklenen Sonuç:**
- DB Watcher: Change detected (CHANGEDATE tracking)
- RabbitMQ: Message published (routing: stok.hareket.updated)
- SignalR Client: `StokHareketUpdated` event received
- Event log'da sarı renk ile gösterilmeli

### 3.3 Event Latency Test
**Amaç:** End-to-end latency ölçümü

**Adımlar:**
1. Client connect
2. Database INSERT
3. Client'ta event timestamp - DB insert timestamp karşılaştır

**Beklenen Sonuç:**
- Adaptive polling (1-10s) + Processing (~100ms)
- **Best case:** ~1 saniye (DB Watcher hızlı polling)
- **Worst case:** ~10 saniye (DB Watcher yavaş polling)

---

## 4. Multi-Tenant Filtering Tests

### 4.1 Branch-Based Event Filtering
**Amaç:** User sadece yetkili şubelerin event'lerini almalı

**Adımlar:**
1. User login: SubeIds = [1, 2]
2. Client connect
3. Database INSERT (MasrafMerkeziId = 1):
```sql
INSERT INTO TBL_STOK_HAREKET (..., MASRAFMERKEZIID) VALUES (..., 1);
```
4. Database INSERT (MasrafMerkeziId = 3):
```sql
INSERT INTO TBL_STOK_HAREKET (..., MASRAFMERKEZIID) VALUES (..., 3);
```

**Beklenen Sonuç:**
- Event 1 (MasrafMerkeziId=1): **Client almalı** ✅
- Event 2 (MasrafMerkeziId=3): **Client almamalı** ❌

**Verification:**
- Server log: "Broadcasted to group: sube_1"
- Server log: "Broadcasted to group: sube_3" (ama client almaz)

### 4.2 Multiple Users - Isolation Test
**Amaç:** Farklı kullanıcılar sadece kendi şubelerinin event'lerini almalı

**Adımlar:**
1. **Browser Tab 1:**
   - User A login (SubeIds: [1])
   - Connect

2. **Browser Tab 2:**
   - User B login (SubeIds: [2])
   - Connect

3. Database INSERT (MasrafMerkeziId = 1)
4. Database INSERT (MasrafMerkeziId = 2)

**Beklenen Sonuç:**
- Tab 1 (User A): Sadece Event 1 görünür
- Tab 2 (User B): Sadece Event 2 görünür

### 4.3 Admin User - All Events
**Amaç:** Admin kullanıcı tüm şubelerin event'lerini almalı

**Adımlar:**
1. User login: SubeIds = [1, 2, 3, 4, 5] (Admin)
2. Connect
3. Farklı şubeler için event'ler oluştur

**Beklenen Sonuç:**
- Tüm event'ler client'a ulaşmalı
- Group membership: ["sube_1", "sube_2", "sube_3", "sube_4", "sube_5"]

### 4.4 No Branch Access User
**Amaç:** SubeIds boş kullanıcı hiç event almamalı

**Adımlar:**
1. User login: SubeIds = [] (boş)
2. Connect
3. Database INSERT (herhangi bir MasrafMerkeziId)

**Beklenen Sonuç:**
- Server log: "User has no branch access"
- Client hiç event almaz
- Group membership: []

---

## 5. DB Watcher Adaptive Polling Tests

### 5.1 Fast Polling (Data Active)
**Amaç:** Sürekli veri varsa polling hızlanmalı

**Adımlar:**
1. DB Watcher başlat
2. Her 2 saniyede bir INSERT yap (10 kez)
3. DB Watcher log'una bak

**Beklenen Sonuç:**
- Interval: 1000ms (MinInterval)
- "Detected N changes" log'ları
- Consecutive empty queries: 0

### 5.2 Slow Polling (No Data)
**Amaç:** Veri yoksa polling yavaşlamalı

**Adımlar:**
1. DB Watcher başlat
2. 30 saniye bekle (hiç INSERT yapma)
3. DB Watcher log'una bak

**Beklenen Sonuç:**
- Interval: 10000ms (MaxInterval)
- "No changes detected" log'ları (her 10 saniyede)
- Consecutive empty queries: 3+

### 5.3 Batch Processing
**Amaç:** Çok sayıda değişiklik batch'ler halinde işlenmeli

**Adımlar:**
1. DB Watcher durdur
2. Database'e 200 kayıt INSERT et
3. DB Watcher başlat

**Beklenen Sonuç:**
- İlk sorgu: 100 kayıt (BatchSize limit)
- İkinci sorgu: 100 kayıt
- Her batch için event publish
- Interval hızlı kalır (1000ms)

---

## 6. RabbitMQ Integration Tests

### 6.1 Message Publishing
**Amaç:** DB Watcher RabbitMQ'ya publish etmeli

**Adımlar:**
1. RabbitMQ Management aç: http://localhost:15672
2. Queues → `barsoft.stok.queue` seç
3. Database INSERT yap
4. Queue'da message görün

**Beklenen Sonuç:**
- Queue message count: +1
- Message rate: ~1/sec (adaptive'e göre)

### 6.2 Message Consumption
**Amaç:** SignalR Hub Consumer RabbitMQ'dan consume etmeli

**Adımlar:**
1. Consumer başlat
2. RabbitMQ Management'ta consumer sayısına bak
3. Database INSERT
4. Queue message count değişimini izle

**Beklenen Sonuç:**
- Consumer count: 1 (RabbitMqConsumerService)
- Message delivered: +1
- Ack: Success

### 6.3 Queue Persistence
**Amaç:** RabbitMQ restart'ta message'lar korunmalı

**Adımlar:**
1. DB Watcher çalışırken 10 event oluştur
2. Consumer'ı durdur (SignalR Hub)
3. Queue'da 10 message bekler
4. Consumer'ı başlat
5. Queue boşalmalı

**Beklenen Sonuç:**
- Messages unacked → acked
- Queue length: 0
- Tüm event'ler client'a ulaşır

---

## 7. Error Handling & Recovery Tests

### 7.1 Database Connection Lost
**Amaç:** DB connection kesildiğinde recovery

**Adımlar:**
1. DB Watcher çalışırken SQL Server'ı durdur
2. Log'ları izle
3. SQL Server'ı başlat

**Beklenen Sonuç:**
- Error log: "Error processing changes"
- Retry mechanism (10 saniye delay)
- Connection restored

### 7.2 RabbitMQ Connection Lost
**Amaç:** RabbitMQ kesintisinde recovery

**Adımlar:**
1. DB Watcher & Consumer çalışırken RabbitMQ'yu durdur
2. Log'ları izle
3. RabbitMQ'yu başlat

**Beklenen Sonuç:**
- **DB Watcher:** "Connection shutdown" → Automatic recovery
- **Consumer:** "Connection shutdown" → Automatic recovery
- Connection restored

### 7.3 Malformed Event Data
**Amaç:** Bozuk JSON'da graceful error handling

**Adımlar:**
1. RabbitMQ Management → Publish message
2. Bozuk JSON gönder:
```json
{ "invalid": "data", "missing": "fields
```

**Beklenen Sonuç:**
- Consumer log: "Failed to deserialize"
- Message: BasicNack (not requeued)
- System devam eder

---

## 8. Performance Tests

### 8.1 High Volume Event Test
**Amaç:** Yüksek event trafiğinde performans

**Adımlar:**
1. 5 client connect
2. Database'e 1000 kayıt INSERT (loop)
3. Her client'ta event count izle

**Beklenen Sonuç:**
- Tüm client'lar tüm event'leri alır
- SignalR Hub CPU: < 20%
- Memory: Stable (no leak)
- Event latency: < 2 saniye

### 8.2 Long Running Stability
**Amaç:** 24 saat kesintisiz çalışma

**Adımlar:**
1. Tüm servisleri başlat
2. Her 10 saniyede bir random INSERT (script)
3. 24 saat çalıştır
4. Memory & CPU izle

**Beklenen Sonuç:**
- No memory leak
- No connection drops
- Event delivery: 100%

---

## 9. Security Tests

### 9.1 Token Expiration
**Amaç:** Expired token ile connection başarısız olmalı

**Adımlar:**
1. `appsettings.json`: ExpirationMinutes = 1
2. Login, token al
3. 2 dakika bekle
4. SignalR connect dene

**Beklenen Sonuç:**
- Connection failed: 401 Unauthorized

### 9.2 Invalid Token
**Amaç:** Manipüle edilmiş token reddedilmeli

**Adımlar:**
1. Valid token al
2. Token'ı değiştir (son karakter)
3. SignalR connect dene

**Beklenen Sonuç:**
- Connection failed: 401 Unauthorized

### 9.3 Cross-Tenant Data Leakage
**Amaç:** User başka şubenin event'ini görmemeli

**Adımlar:**
1. User A: SubeIds = [1]
2. User B: SubeIds = [2]
3. Event: MasrafMerkeziId = 2
4. User A client'ına bak

**Beklenen Sonuç:**
- User A event'i görmez ✅
- User B event'i görür ✅

---

## Test Automation

### Integration Test Script
```bash
#!/bin/bash

# Start services
docker-compose up -d rabbitmq
cd src/Barsoft.SignalRHub.DbWatcher && dotnet run &
cd src/Barsoft.SignalRHub.SignalRHub && dotnet run &

# Wait for startup
sleep 10

# Run tests
dotnet test

# Cleanup
pkill -f "dotnet run"
docker-compose down
```

### Load Test (k6)
```javascript
import http from 'k6/http';
import { check } from 'k6';

export default function () {
  const payload = JSON.stringify({
    userCode: '0001',
    password: 'password',
  });

  const res = http.post('https://localhost:5001/api/auth/login', payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(res, {
    'status is 200': (r) => r.status === 200,
    'has token': (r) => JSON.parse(r.body).token !== undefined,
  });
}
```

---

## Test Checklist

- [ ] Authentication & Authorization (1.1-1.4)
- [ ] SignalR Connection (2.1-2.4)
- [ ] Event Broadcasting (3.1-3.3)
- [ ] Multi-Tenant Filtering (4.1-4.4)
- [ ] Adaptive Polling (5.1-5.3)
- [ ] RabbitMQ Integration (6.1-6.3)
- [ ] Error Handling (7.1-7.3)
- [ ] Performance (8.1-8.2)
- [ ] Security (9.1-9.3)

**Tüm testler geçerse: Production-ready! 🚀**
