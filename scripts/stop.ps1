$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

Write-Host "=== WKR RUL SYSTEM: STOP ===" -ForegroundColor Cyan

Set-Location $Infra

docker compose down

Write-Host "`nContainers stopped. PostgreSQL volume pgdata was not deleted." -ForegroundColor Green
Write-Host "To delete database volume use: docker compose down -v" -ForegroundColor Yellow