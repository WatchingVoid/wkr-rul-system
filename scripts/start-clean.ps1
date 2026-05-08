$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

Write-Host "=== WKR RUL SYSTEM: CLEAN START ===" -ForegroundColor Cyan
Write-Host "WARNING: This script deletes PostgreSQL volume pgdata." -ForegroundColor Red

Set-Location $Infra

Write-Host "`n[1/6] Checking docker compose config..." -ForegroundColor Yellow
docker compose config | Out-Null

Write-Host "`n[2/6] Stopping containers and removing volumes..." -ForegroundColor Yellow
docker compose down -v

Write-Host "`n[3/6] Starting database..." -ForegroundColor Yellow
docker compose up -d db

Write-Host "`n[4/6] Applying Flyway migrations..." -ForegroundColor Yellow
docker compose --profile migrate run --rm flyway

Write-Host "`n[5/6] Starting Adminer, ML-service and Backend..." -ForegroundColor Yellow
docker compose up -d adminer
docker compose up -d --build ml backend

Write-Host "`n[6/6] Current containers:" -ForegroundColor Yellow
docker compose ps

Write-Host "`nChecking database tables:" -ForegroundColor Green
docker compose exec db psql -U wkr -d wkr -c "\dt wkr.*"

Write-Host "`nChecking backend:" -ForegroundColor Green
try {
    Invoke-RestMethod "http://localhost:8000/health"
}
catch {
    Write-Host "Backend is not ready yet. Check logs: docker compose logs --tail 100 backend" -ForegroundColor Red
}

Write-Host "`nChecking ML-service:" -ForegroundColor Green
try {
    Invoke-RestMethod "http://localhost:8001/health"
}
catch {
    Write-Host "ML-service is not ready yet. Check logs: docker compose logs --tail 100 ml" -ForegroundColor Red
}

Write-Host "`n=== CLEAN START COMPLETE ===" -ForegroundColor Cyan