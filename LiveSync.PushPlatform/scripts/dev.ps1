#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

Write-Host "Starting Docker (SQL Server + Redis)..." -ForegroundColor Cyan
docker compose up -d

Write-Host "Building React client..." -ForegroundColor Cyan
Push-Location "$Root\LiveSync.API\client"
if (-not (Test-Path "node_modules")) {
    npm ci
}
npm run build
Pop-Location

$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "Starting API (http://localhost:5252)..." -ForegroundColor Green
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "LiveSync.API" -WorkingDirectory $Root

Start-Sleep -Seconds 2

Write-Host "Starting Worker (metrics http://localhost:5260)..." -ForegroundColor Green
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "LiveSync.Worker" -WorkingDirectory $Root

Write-Host ""
Write-Host "LiveSync dev stack started." -ForegroundColor Green
Write-Host "  App:     http://localhost:5252"
Write-Host "  Scalar:  http://localhost:5252/scalar/v1"
Write-Host "  Metrics: http://localhost:5252/metrics (API), http://localhost:5260/metrics (Worker)"
Write-Host ""
Write-Host "Optional: cd LiveSync.API/client && npm run dev  -> http://localhost:5173"
Write-Host "Troubleshooting: docs/troubleshooting.md"
