$ErrorActionPreference = "Stop"

Write-Output "=== WKR RUL SYSTEM START COLLECTOR ==="

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

Push-Location $Infra

Write-Output ""
Write-Output "[1] Checking docker compose config"
docker compose config | Out-Null

Write-Output ""
Write-Output "[2] Starting collector"
docker compose up -d --build collector

Write-Output ""
Write-Output "[3] Current containers"
docker compose ps

Write-Output ""
Write-Output "[4] Collector logs"
docker compose logs --tail 80 collector

Pop-Location

Write-Output ""
Write-Output "=== COLLECTOR START COMPLETE ==="