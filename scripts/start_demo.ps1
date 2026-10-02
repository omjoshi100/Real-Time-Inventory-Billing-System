# ============================================================================
# REAL-TIME INVENTORY & BILLING SYSTEM - ONE-CLICK DEMO LAUNCHER
# ============================================================================

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " LAUNCHING REAL-TIME INVENTORY & BILLING SYSTEM DEMO      " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Starting 3 tiers concurrently:                          " -ForegroundColor Gray
Write-Host "   1. ASP.NET Core Web API (Port 5000 + SignalR Hub)      " -ForegroundColor Yellow
Write-Host "   2. WPF Cashier Terminal (POS - MVVM)                   " -ForegroundColor Yellow
Write-Host "   3. WinForms Admin Screen (Live Sales Ticker & Alerts)  " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# 1. Start API Backend in new process
Write-Host "`n[1/3] Starting ASP.NET Core Web API..." -ForegroundColor Green
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/RealTimeInventoryBilling.API/RealTimeInventoryBilling.API.csproj --urls=http://localhost:5000" -PassThru -WindowStyle Minimized

Write-Host "Waiting 4 seconds for API and database initialization..." -ForegroundColor Gray
Start-Sleep -Seconds 4

# Verify API is alive
try {
    $res = Invoke-RestMethod -Uri "http://localhost:5000/api/products" -Method Get -TimeoutSec 3
    Write-Host "API is ONLINE! Found $($res.Count) catalog products." -ForegroundColor Green
} catch {
    Write-Host "Warning: API starting up, proceeding to launch clients..." -ForegroundColor Yellow
}

# 2. Start WPF Client
Write-Host "`n[2/3] Launching WPF Desktop POS Client (MVVM)..." -ForegroundColor Green
$wpfProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/RealTimeInventoryBilling.WPF/RealTimeInventoryBilling.WPF.csproj" -PassThru

# 3. Start WinForms Admin Client
Write-Host "`n[3/3] Launching WinForms Admin Screen (Real-Time Monitor)..." -ForegroundColor Green
$winFormsProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/RealTimeInventoryBilling.WinForms/RealTimeInventoryBilling.WinForms.csproj" -PassThru

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " ALL SYSTEMS RUNNING!" -ForegroundColor Green
Write-Host " Demo Instructions:" -ForegroundColor White
Write-Host "   1. WinForms Admin: Login as 'admin' / 'Admin@123'" -ForegroundColor White
Write-Host "   2. WPF Cashier: Login as 'cashier1' / 'Cashier@123'" -ForegroundColor White
Write-Host "   3. In WPF, scan or add 'BEV-002' or 'SNK-002' and click 'COMPLETE SALE'" -ForegroundColor White
Write-Host "   4. Watch the WinForms Live Sales Ticker and today's revenue update INSTANTLY!" -ForegroundColor Yellow
Write-Host "   5. Watch both screens display the SignalR Low Stock Alert banner!" -ForegroundColor Red
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Press Ctrl+C or close this window when done testing." -ForegroundColor Gray
