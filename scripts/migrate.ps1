$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

Write-Host "=== WKR RUL SYSTEM: MIGRATE DATABASE ===" -ForegroundColor Cyan

Set-Location $Infra

Write-Host "`n[1/4] Checking docker compose config..." -ForegroundColor Yellow
docker compose config | Out-Null

Write-Host "`n[2/4] Starting database..." -ForegroundColor Yellow
docker compose up -d db

Write-Host "`n[3/4] Running Flyway migrations..." -ForegroundColor Yellow
docker compose --profile migrate run --rm flyway

Write-Host "`n[4/4] Checking migration history..." -ForegroundColor Yellow

Write-Host "`nTrying public.flyway_schema_history..." -ForegroundColor DarkGray
docker compose exec db psql -U wkr -d wkr -c "select installed_rank, version, description, success from public.flyway_schema_history order by installed_rank;" 2>$null

Write-Host "`nDatabase tables:" -ForegroundColor Green
docker compose exec db psql -U wkr -d wkr -c "\dt wkr.*"

Write-Host "`n=== MIGRATION COMPLETE ===" -ForegroundColor Cyan