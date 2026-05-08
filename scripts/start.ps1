$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

Write-Host "=== WKR RUL SYSTEM: START ===" -ForegroundColor Cyan
Write-Host "Project root: $Root"
Write-Host "Infra path:   $Infra"

Set-Location $Infra

Write-Host "`n[1/4] Checking docker compose config..." -ForegroundColor Yellow
docker compose config | Out-Null

Write-Host "`n[2/4] Starting database and Adminer..." -ForegroundColor Yellow
docker compose up -d db adminer

Write-Host "`n[3/4] Starting ML-service and Backend..." -ForegroundColor Yellow
docker compose up -d ml backend

Write-Host "`n[4/4] Current containers:" -ForegroundColor Yellow
docker compose ps

Write-Host "`nBackend health:" -ForegroundColor Green
try {
    Invoke-RestMethod "http://localhost:8000/health"
}
catch {
    Write-Host "Backend is not ready yet. Check logs: docker compose logs --tail 100 backend" -ForegroundColor Red
}

Write-Host "`nML health:" -ForegroundColor Green
try {
    Invoke-RestMethod "http://localhost:8001/health"
}
catch {
    Write-Host "ML-service is not ready yet. Check logs: docker compose logs --tail 100 ml" -ForegroundColor Red
}

Write-Host "`n=== START COMPLETE ===" -ForegroundColor Cyan
Write-Host "Backend: http://localhost:8000/swagger"
Write-Host "ML:      http://localhost:8001/model/info"
Write-Host "Adminer: http://localhost:8080"