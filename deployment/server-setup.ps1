# ============================================
# Barsoft SignalR Hub - Server Deployment Script
# IP: 45.13.190.248
# ============================================

Write-Host "=== Barsoft SignalR Hub Deployment ===" -ForegroundColor Green

# 1. Clone Repository
Write-Host "`n[1/7] Cloning repository from GitHub..." -ForegroundColor Cyan
$deployPath = "C:\Barsoft_Deployment"
if (Test-Path $deployPath) {
    Write-Host "Deployment directory exists. Pulling latest changes..." -ForegroundColor Yellow
    cd $deployPath
    git pull origin main
} else {
    git clone https://github.com/ssengur01/Barsoft_SignalRHub.git $deployPath
    cd $deployPath
}

# 2. Configure Environment Variables
Write-Host "`n[2/7] Creating environment files..." -ForegroundColor Cyan

# React Frontend .env
$reactEnv = @"
VITE_API_BASE_URL=http://45.13.190.248:5000
VITE_SIGNALR_HUB_URL=http://45.13.190.248:5000/hubs/stokhareket
"@
$reactEnv | Out-File -FilePath "web\.env.local" -Encoding UTF8

Write-Host "✓ React .env.local created" -ForegroundColor Green

# 3. Build & Run Docker Services
Write-Host "`n[3/7] Starting RabbitMQ with Docker..." -ForegroundColor Cyan
cd docker
docker-compose up -d rabbitmq
Write-Host "✓ RabbitMQ started on port 5672, 15672" -ForegroundColor Green
cd ..

# 4. Build .NET Projects
Write-Host "`n[4/7] Building .NET projects..." -ForegroundColor Cyan
dotnet restore
dotnet build -c Release

Write-Host "✓ .NET projects built" -ForegroundColor Green

# 5. Publish Projects
Write-Host "`n[5/7] Publishing .NET projects..." -ForegroundColor Cyan

dotnet publish src/Barsoft.SignalRHub.DbWatcher/Barsoft.SignalRHub.DbWatcher.csproj `
    -c Release -o publish/DbWatcher

dotnet publish src/Barsoft.SignalRHub.SignalRHub/Barsoft.SignalRHub.SignalRHub.csproj `
    -c Release -o publish/SignalRHub

Write-Host "✓ Projects published to ./publish/" -ForegroundColor Green

# 6. Build React Frontend
Write-Host "`n[6/7] Building React frontend..." -ForegroundColor Cyan
cd web
npm install
npm run build
Write-Host "✓ React app built to ./web/dist/" -ForegroundColor Green
cd ..

# 7. Install as Windows Services
Write-Host "`n[7/7] Installing Windows Services..." -ForegroundColor Cyan

# DB Watcher Service
$dbWatcherPath = "$deployPath\publish\DbWatcher\Barsoft.SignalRHub.DbWatcher.exe"
sc.exe create "BarsoftDbWatcher" `
    binPath= $dbWatcherPath `
    start= auto `
    DisplayName= "Barsoft DB Watcher Service"

# SignalR Hub Service
$signalRHubPath = "$deployPath\publish\SignalRHub\Barsoft.SignalRHub.SignalRHub.exe"
sc.exe create "BarsoftSignalRHub" `
    binPath= "$signalRHubPath --urls http://*:5000" `
    start= auto `
    DisplayName= "Barsoft SignalR Hub Service"

Write-Host "✓ Windows Services created" -ForegroundColor Green

# Start Services
Write-Host "`nStarting services..." -ForegroundColor Cyan
sc.exe start BarsoftDbWatcher
sc.exe start BarsoftSignalRHub

Write-Host "`n=== Deployment Complete! ===" -ForegroundColor Green
Write-Host "`nServices:" -ForegroundColor White
Write-Host "  - RabbitMQ Management: http://45.13.190.248:15672 (admin/admin123)" -ForegroundColor Yellow
Write-Host "  - SignalR Hub API: http://45.13.190.248:5000" -ForegroundColor Yellow
Write-Host "  - Health Check: http://45.13.190.248:5000/health" -ForegroundColor Yellow
Write-Host "`nNext Steps:" -ForegroundColor White
Write-Host "  1. Configure Firewall (ports 5000, 80)" -ForegroundColor Yellow
Write-Host "  2. Setup IIS for React frontend (./web/dist)" -ForegroundColor Yellow
Write-Host "  3. Update SQL Server connection string" -ForegroundColor Yellow
Write-Host "  4. Check Windows Services are running" -ForegroundColor Yellow
