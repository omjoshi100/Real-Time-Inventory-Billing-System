# ============================================================================
# REAL-TIME INVENTORY & BILLING SYSTEM - BUILD & PUBLISH SCRIPT
# ============================================================================
param (
    [string]$Configuration = "Release",
    [string]$OutputDir = "publish"
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " BUILDING & PACKAGING REAL-TIME INVENTORY & BILLING SYSTEM " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# Clean previous output
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}

New-Item -ItemType Directory -Path "$OutputDir\API" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir\WPF_Cashier_POS" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir\WinForms_Admin" -Force | Out-Null

Write-Host "`n[1/4] Publishing ASP.NET Core Web API..." -ForegroundColor Yellow
dotnet publish src/RealTimeInventoryBilling.API/RealTimeInventoryBilling.API.csproj -c $Configuration -o "$OutputDir\API" --no-self-contained

Write-Host "`n[2/4] Publishing WPF Cashier POS Terminal (MVVM)..." -ForegroundColor Yellow
dotnet publish src/RealTimeInventoryBilling.WPF/RealTimeInventoryBilling.WPF.csproj -c $Configuration -o "$OutputDir\WPF_Cashier_POS" --no-self-contained

Write-Host "`n[3/4] Publishing WinForms Admin Screen..." -ForegroundColor Yellow
dotnet publish src/RealTimeInventoryBilling.WinForms/RealTimeInventoryBilling.WinForms.csproj -c $Configuration -o "$OutputDir\WinForms_Admin" --no-self-contained

Write-Host "`n[4/4] Copying Database Scripts & Setup Tools..." -ForegroundColor Yellow
Copy-Item -Path "database" -Destination "$OutputDir\database" -Recurse -Force
Copy-Item -Path "scripts\start_demo.ps1" -Destination "$OutputDir\start_demo.ps1" -Force

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " BUILD & PACKAGING COMPLETE!" -ForegroundColor Green
Write-Host " Output available in: $OutputDir" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
