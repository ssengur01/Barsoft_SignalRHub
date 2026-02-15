# Barsoft SignalR Hub - Real-time Veri Senkronizasyon Sistemi

[![CI/CD Pipeline](https://github.com/ssengur01/Barsoft_SignalRHub/actions/workflows/ci.yml/badge.svg)](https://github.com/ssengur01/Barsoft_SignalRHub/actions)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--time-blue)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Message%20Broker-orange)](https://www.rabbitmq.com/)

## 📋 Proje Hakkında

Barsoft SignalR Hub, SQL Server veritabanındaki stok hareketlerini **event-driven architecture** ile gerçek zamanlı olarak web ve desktop client'lara ileten production-ready bir sistemdir.

### ✨ Temel Özellikler

- 🔄 **Event-Driven Architecture** - RabbitMQ ile loose-coupling
- 📡 **Real-time Communication** - SignalR WebSocket bağlantısı
- 🔐 **JWT Authentication** - Güvenli kullanıcı doğrulama
- 🏢 **Multi-tenant** - Şube bazlı veri izolasyonu
- 📊 **Adaptive Polling** - Akıllı veritabanı izleme (1-10 saniye dinamik)
- 🐳 **Docker Support** - Container orchestration
- 🚀 **CI/CD Pipeline** - GitHub Actions ile otomatik deployment

---

## 🏗️ Mimari Tasarım

### Event Flow

```
┌─────────────────┐
│   SQL Server    │ (Read-Only, Windows Auth)
│   TBL_STOK_     │
│   HAREKET       │
└────────┬────────┘
         │ Adaptive Polling (ID + CHANGEDATE tracking)
         ↓
┌─────────────────┐
│   DB Watcher    │ (Background Worker Service)
│   Service       │ - Change Detection
└────────┬────────┘ - Event Creation
         │ Publish Domain Events
         ↓
┌─────────────────┐
│   RabbitMQ      │ (Message Broker)
│   Exchange:     │ barsoft.stok.exchange (Topic)
│   Queue:        │ barsoft.stok.queue (Durable)
└────────┬────────┘
         │ Subscribe/Consume
         ↓
┌─────────────────┐
│ SignalR Hub     │ (Web API + Hosted Service)
│ Service         │ - JWT Authorization
└────────┬────────┘ - Group-based Filtering
         │ WebSocket (HTTPS + JWT)
         ↓
┌─────────────────┐
│   Clients       │ (Web / Desktop)
│   (React, WPF)  │
└─────────────────┘
```

---

## 📁 Solution Yapısı

```
Barsoft.SignalRHub/
├── src/
│   ├── Barsoft.SignalRHub.Domain/              # Domain entities, events, value objects
│   │   ├── Entities/
│   │   │   ├── StokHareket.cs                  (29 columns - TBL_STOK_HAREKET)
│   │   │   └── User.cs                         (15 columns - TBL_USER_MAIN)
│   │   ├── Events/
│   │   │   ├── StokHareketCreatedEvent.cs
│   │   │   └── StokHareketUpdatedEvent.cs
│   │   └── ValueObjects/
│   │       └── ChangeTrackingInfo.cs
│   │
│   ├── Barsoft.SignalRHub.Application/         # Application logic, interfaces, DTOs
│   │   ├── DTOs/
│   │   │   ├── StokHareketDto.cs
│   │   │   ├── UserDto.cs
│   │   │   └── LoginRequestDto.cs
│   │   └── Interfaces/
│   │       ├── IStokHareketRepository.cs
│   │       ├── IUserRepository.cs
│   │       ├── IMessagePublisher.cs
│   │       └── IJwtTokenService.cs
│   │
│   ├── Barsoft.SignalRHub.Infrastructure/      # EF Core, RabbitMQ, JWT implementation
│   │   ├── Persistence/                        (EF Core DbContext - Read-only)
│   │   ├── Messaging/                          (RabbitMQ Publisher/Consumer)
│   │   └── Security/                           (JWT Token Service)
│   │
│   ├── Barsoft.SignalRHub.DbWatcher/           # Background Worker Service
│   │   └── ChangeDetectionStrategy.cs          (Adaptive polling: 1-10s)
│   │
│   ├── Barsoft.SignalRHub.SignalRHub/          # Web API + SignalR Hub
│   │   ├── Hubs/StokHareketHub.cs             ([Authorize] JWT)
│   │   ├── Controllers/AuthController.cs
│   │   └── BackgroundServices/
│   │       └── RabbitMqConsumerService.cs
│   │
│   └── Barsoft.SignalRHub.Shared/              # Constants, extensions
│       └── Constants/RabbitMqConstants.cs
│
├── tests/                                       # Unit & Integration tests
│
├── docker/
│   ├── docker-compose.yml                       # RabbitMQ + Services
│   ├── Dockerfile.DbWatcher
│   └── Dockerfile.SignalRHub
│
└── .github/workflows/
    └── ci.yml                                   # CI/CD Pipeline
```

---

## 🚀 Hızlı Başlangıç

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- SQL Server (Windows Authentication)

### 1. Projeyi Klonlayın

```bash
git clone https://github.com/ssengur01/Barsoft_SignalRHub.git
cd Barsoft_SignalRHub
```

### 2. RabbitMQ'yu Başlatın

```bash
cd docker
docker-compose up -d rabbitmq
```

RabbitMQ Management Console: http://localhost:15672 (admin/admin123)

### 3. Solution'ı Build Edin

```bash
dotnet restore
dotnet build
```

### 4. Servisleri Çalıştırın

**DB Watcher Service:**
```bash
cd src/Barsoft.SignalRHub.DbWatcher
dotnet run
```

**SignalR Hub Service:**
```bash
cd src/Barsoft.SignalRHub.SignalRHub
dotnet run
```

### 5. Docker ile Tüm Sistemi Başlatın

```bash
cd docker
docker-compose up --build
```

- SignalR Hub API: http://localhost:5000
- RabbitMQ Management: http://localhost:15672

---

## 🔧 Konfigürasyon

### Database Connection

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "BarsoftDb": "Data Source=MSI;Database=BARSOFT;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

### RabbitMQ Settings

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "admin",
    "Password": "admin123"
  }
}
```

### JWT Settings

```json
{
  "JWT": {
    "Secret": "your-super-secret-key-min-32-chars",
    "Issuer": "BarsoftSignalRHub",
    "Audience": "BarsoftClients",
    "ExpirationMinutes": 480
  }
}
```

---

## 🔐 Authentication & Authorization

### Login Endpoint

```http
POST /api/auth/login
Content-Type: application/json

{
  "userCode": "0001",
  "password": "your-password"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 1,
    "userCode": "0001",
    "description": "Admin",
    "isAdmin": true,
    "subeIds": [1, 2, 3]
  },
  "expiresAt": "2025-12-15T12:00:00Z"
}
```

### SignalR Connection

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:5001/hubs/stokhareket", {
        accessTokenFactory: () => "your-jwt-token"
    })
    .build();

connection.on("StokHareketReceived", (data) => {
    console.log("New stock movement:", data);
});

await connection.start();
```

---

## 📊 Veritabanı Şeması

### TBL_STOK_HAREKET (29 Kolon)

| Kolon | Tip | Açıklama |
|-------|-----|----------|
| ID | int | Primary key |
| STOKID | int | Stok ID |
| BELGEKODU | varchar(20) | Belge kodu |
| MIKTAR | decimal | Hareket miktarı |
| CREATEDATE | smalldatetime | **Change tracking için kritik** |
| CHANGEDATE | smalldatetime | **Change tracking için kritik** |
| ... | ... | ... (toplam 29 kolon) |

### TBL_USER_MAIN (15 Kolon)

| Kolon | Tip | Açıklama |
|-------|-----|----------|
| ID | int | Primary key |
| USERCODE | varchar(100) | Kullanıcı kodu (unique) |
| PASSWORD | varchar(20) | Hash'lenmiş şifre |
| SUBEIDS | varchar(MAX) | CSV format: "1,2,3" |
| AKTIF | bit | Aktif kullanıcı kontrolü |

---

## 🎯 DB Watcher Stratejisi

### Adaptive Incremental Polling

```csharp
// Sürekli 5 saniye polling YOK!
// Dinamik interval: 1-10 saniye

SELECT TOP 100 *
FROM TBL_STOK_HAREKET
WHERE ID > @LastProcessedId
   OR (CHANGEDATE IS NOT NULL AND CHANGEDATE > @LastProcessedDate)
ORDER BY ID ASC
```

**Avantajlar:**
- ✅ Sadece yeni/değişen kayıtlar çekiliyor
- ✅ Batch size kontrollü (100 kayıt)
- ✅ Veri yoksa interval artıyor (10sn)
- ✅ Veri varsa interval azalıyor (1sn)
- ✅ DB yükü minimal

---

## 📡 RabbitMQ Mesajlaşma

### Exchange & Queue

- **Exchange:** `barsoft.stok.exchange` (Topic)
- **Queue:** `barsoft.stok.queue` (Durable)
- **Routing Keys:**
  - `stok.hareket.created`
  - `stok.hareket.updated`

### Event Yapısı

```json
{
  "id": 19882,
  "stokId": 13915,
  "belgeKodu": "MERKEZ_20251208",
  "miktar": -2.0,
  "toplamTutar": -300.0,
  "createDate": "2025-12-08T15:53:00",
  "masrafMerkeziId": 1,
  "eventTimestamp": "2025-12-08T15:53:01Z",
  "version": "1.0"
}
```

---

## 🧪 Testing

```bash
# Unit tests
dotnet test

# Integration tests (Docker gerekli)
docker-compose -f docker/docker-compose.yml up -d
dotnet test --filter Category=Integration
```

---

## 🚢 Deployment

### Docker Compose ile Production

```bash
cd docker
docker-compose up -d
```

### Manuel Deployment

1. Publish projeler:
```bash
dotnet publish -c Release -o ./publish/dbwatcher src/Barsoft.SignalRHub.DbWatcher
dotnet publish -c Release -o ./publish/signalrhub src/Barsoft.SignalRHub.SignalRHub
```

2. Sistemik servis olarak çalıştır (Windows):
```bash
sc create BarsoftDbWatcher binPath="C:\path\to\Barsoft.SignalRHub.DbWatcher.exe"
sc create BarsoftSignalRHub binPath="C:\path\to\Barsoft.SignalRHub.SignalRHub.exe"
```

---

## 🔄 CI/CD Pipeline

GitHub Actions otomatik olarak:
- ✅ Kodu build eder
- ✅ Testleri çalıştırır
- ✅ Docker image'larını oluşturur
- ✅ Artifact'leri publish eder

**Branch Stratejisi:**
- `main` → Production
- `develop` → Development
- `feature/*` → Yeni özellikler

---

## 📈 Monitoring & Logs

### RabbitMQ Monitoring
- http://localhost:15672 - Management console
- Queue size, message rate, consumers

### Application Logs
```bash
# DbWatcher logs
docker logs barsoft-dbwatcher

# SignalRHub logs
docker logs barsoft-signalrhub
```

---

## 🛡️ Güvenlik

- ✅ JWT token authentication
- ✅ HTTPS zorunlu (production)
- ✅ Password hashing (BCrypt - FAZ 3'te eklenecek)
- ✅ SignalR Group bazlı authorization
- ✅ Cross-user veri sızıntısı koruması
- ✅ SQL Injection koruması (Parameterized queries)

---

## 🧩 FAZ İlerlemesi

| Faz | Durum | Açıklama |
|-----|-------|----------|
| **FAZ 1** | ✅ **TAMAMLANDI** | Mimari tasarım, Solution yapısı, Docker, CI/CD |
| **FAZ 2** | ⏳ Bekliyor | Entity configurations + EF Core DbContext |
| **FAZ 3** | ⏳ Bekliyor | JWT authentication + Login API |
| **FAZ 4** | ⏳ Bekliyor | DB Watcher Service + RabbitMQ Producer |
| **FAZ 5** | ⏳ Bekliyor | SignalR Hub + RabbitMQ Consumer |
| **FAZ 6** | ⏳ Bekliyor | Client demo + User filtering |
| **FAZ 7** | ⏳ Bekliyor | Full CI/CD pipeline + Deploy docs |

---

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Commit edin (`git commit -m 'Add some AmazingFeature'`)
4. Push edin (`git push origin feature/AmazingFeature`)
5. Pull Request açın

---

## 📄 Lisans

Bu proje Barsoft için geliştirilmiştir.

---

## 📞 İletişim

- **Proje Sahibi:** Barsoft
- **Repository:** https://github.com/ssengur01/Barsoft_SignalRHub
- **Issues:** https://github.com/ssengur01/Barsoft_SignalRHub/issues

---

## 🙏 Teşekkürler

- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [RabbitMQ](https://www.rabbitmq.com/documentation.html)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

---

**Made with ❤️ using .NET 9, SignalR, and RabbitMQ**
