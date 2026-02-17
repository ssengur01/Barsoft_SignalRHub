# Barsoft SignalR Hub - Production Deployment Guide

## 📋 Sunucu Bilgileri
- **IP:** 45.13.190.248
- **Kullanıcı:** kodlatechadmin
- **OS:** Windows Server

---

## 🚀 Hızlı Deployment (Otomatik)

### 1. Sunucuya Bağlan
```
Remote Desktop: 45.13.190.248
Kullanıcı: kodlatechadmin
```

### 2. PowerShell'i Yönetici Olarak Aç

### 3. Deployment Scriptini Çalıştır
```powershell
# GitHub'dan deployment scriptini indir ve çalıştır
cd C:\
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/ssengur01/Barsoft_SignalRHub/main/deployment/server-setup.ps1" -OutFile "deploy.ps1"
.\deploy.ps1
```

**VEYA**

Manuel olarak repository'yi clone edin:
```powershell
cd C:\
git clone https://github.com/ssengur01/Barsoft_SignalRHub.git Barsoft_Deployment
cd Barsoft_Deployment\deployment
.\server-setup.ps1
```

---

## 📦 Kurulum Sonrası

### 1️⃣ Firewall Ayarları
```powershell
# Port 5000 (SignalR Hub API)
New-NetFirewallRule -DisplayName "Barsoft SignalR Hub" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow

# Port 80 (React Frontend - IIS)
New-NetFirewallRule -DisplayName "Barsoft Web App" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow

# Port 15672 (RabbitMQ Management - Opsiyonel)
New-NetFirewallRule -DisplayName "RabbitMQ Management" -Direction Inbound -Protocol TCP -LocalPort 15672 -Action Allow
```

### 2️⃣ SQL Server Connection String Güncelle

**Eğer SQL Server farklı bir instance'da ise:**

`C:\Barsoft_Deployment\publish\DbWatcher\appsettings.json` ve
`C:\Barsoft_Deployment\publish\SignalRHub\appsettings.json` dosyalarını düzenle:

```json
"ConnectionStrings": {
  "BarsoftDb": "Data Source=SUNUCU_ADINIZ;Database=BARSOFT;Integrated Security=true;TrustServerCertificate=true;"
}
```

Sonra servisleri yeniden başlat:
```powershell
Restart-Service BarsoftDbWatcher
Restart-Service BarsoftSignalRHub
```

### 3️⃣ IIS ile React Frontend Deploy

**IIS Kurulu Değilse:**
```powershell
Install-WindowsFeature -Name Web-Server -IncludeManagementTools
```

**Site Oluştur:**
1. IIS Manager'ı aç
2. Sites → Add Website
3. **Site name:** BarsoftWebApp
4. **Physical path:** `C:\Barsoft_Deployment\web\dist`
5. **Binding:** HTTP, Port 80, IP: 45.13.190.248
6. OK

**VEYA PowerShell ile:**
```powershell
Import-Module WebAdministration
New-Website -Name "BarsoftWebApp" `
  -PhysicalPath "C:\Barsoft_Deployment\web\dist" `
  -Port 80 `
  -IPAddress "45.13.190.248"
```

---

## ✅ Test

### API Health Check
```powershell
Invoke-WebRequest -Uri "http://45.13.190.248:5000/health"
```

Beklenen:
```json
{"Status":"Healthy","Timestamp":"...","Environment":"Production"}
```

### Login Test
```powershell
$body = @{
    userCode = "0001"
    password = "password"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://45.13.190.248:5000/api/auth/login" `
  -Method Post `
  -Body $body `
  -ContentType "application/json"
```

Beklenen: JWT token dönmeli

### Web App
Tarayıcıda: `http://45.13.190.248`

---

## 🔧 Servis Yönetimi

### Servisleri Kontrol Et
```powershell
Get-Service -Name "Barsoft*"
```

### Logları Görüntüle
```powershell
# DB Watcher logs
Get-EventLog -LogName Application -Source "BarsoftDbWatcher" -Newest 20

# SignalR Hub logs
Get-EventLog -LogName Application -Source "BarsoftSignalRHub" -Newest 20
```

### Servisleri Yeniden Başlat
```powershell
Restart-Service BarsoftDbWatcher
Restart-Service BarsoftSignalRHub
```

### Servisleri Durdur
```powershell
Stop-Service BarsoftDbWatcher
Stop-Service BarsoftSignalRHub
```

---

## 📊 Monitoring

### RabbitMQ Management UI
`http://45.13.190.248:15672`
- Kullanıcı: admin
- Şifre: admin123

### Docker Container'ları Kontrol
```powershell
docker ps
docker logs barsoft-rabbitmq
```

---

## 🔄 Güncelleme (CI/CD)

GitHub'a yeni kod push edildiğinde:

```powershell
cd C:\Barsoft_Deployment
git pull origin main
dotnet build -c Release
dotnet publish src/Barsoft.SignalRHub.DbWatcher/Barsoft.SignalRHub.DbWatcher.csproj -c Release -o publish/DbWatcher
dotnet publish src/Barsoft.SignalRHub.SignalRHub/Barsoft.SignalRHub.SignalRHub.csproj -c Release -o publish/SignalRHub
cd web
npm run build
Restart-Service BarsoftDbWatcher
Restart-Service BarsoftSignalRHub
```

---

## 🆘 Sorun Giderme

### API 500 hatası veriyor
- SQL Server connection string'i kontrol edin
- Windows Authentication çalışıyor mu?
- Event Viewer'da hata loglarına bakın

### SignalR bağlantısı kurulmuyor
- Firewall port 5000 açık mı?
- CORS ayarları doğru mu? (Program.cs)
- JWT token geçerli mi?

### RabbitMQ çalışmıyor
```powershell
docker restart barsoft-rabbitmq
docker logs barsoft-rabbitmq
```

---

## 📞 Erişim URL'leri

- **Web App:** http://45.13.190.248
- **API:** http://45.13.190.248:5000
- **Health:** http://45.13.190.248:5000/health
- **RabbitMQ:** http://45.13.190.248:15672
- **Swagger:** http://45.13.190.248:5000/openapi (Development)

---

✅ **Deployment tamamlandı!**
