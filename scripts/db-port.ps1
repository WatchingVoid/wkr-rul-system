$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

Set-Location $Infra

Write-Host "PostgreSQL port for pgAdmin4:" -ForegroundColor Cyan
docker compose port db 5432

Write-Host "`nUse in pgAdmin4:" -ForegroundColor Yellow
Write-Host "Host: 127.0.0.1"
Write-Host "Port: use port from output above"
Write-Host "Database: wkr"
Write-Host "Username: wkr"
Write-Host "Password: wkr"