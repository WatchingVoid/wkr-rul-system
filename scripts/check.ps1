$ErrorActionPreference = "Continue"

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

Write-Host "=== WKR RUL SYSTEM: CHECK ===" -ForegroundColor Cyan

Set-Location $Infra

Write-Host "`n[1] Docker containers:" -ForegroundColor Yellow
docker compose ps

Write-Host "`n[2] Backend health:" -ForegroundColor Yellow
try {
    Invoke-RestMethod "http://localhost:8000/health"
}
catch {
    Write-Host "Backend health failed" -ForegroundColor Red
}

Write-Host "`n[3] ML-service health:" -ForegroundColor Yellow
try {
    Invoke-RestMethod "http://localhost:8001/health"
}
catch {
    Write-Host "ML health failed" -ForegroundColor Red
}

Write-Host "`n[4] ML model info:" -ForegroundColor Yellow
try {
    Invoke-RestMethod "http://localhost:8001/model/info"
}
catch {
    Write-Host "ML model info failed" -ForegroundColor Red
}

Write-Host "`n[5] Database tables:" -ForegroundColor Yellow
docker compose exec db psql -U wkr -d wkr -P pager=off -c "\dt wkr.*"

Write-Host "`n[6] Database functions:" -ForegroundColor Yellow
docker compose exec db psql -U wkr -d wkr -P pager=off -c "\df wkr.*"

Write-Host "`n[7] Telemetry count:" -ForegroundColor Yellow
docker compose exec db psql -U wkr -d wkr -P pager=off -c "select count(*) from wkr.telemetry_spindle;"

Write-Host "`n[8] Last RUL predictions:" -ForegroundColor Yellow
docker compose exec db psql -U wkr -d wkr -P pager=off -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, state, model_version from wkr.rul_predictions order by ts desc limit 10;"

Write-Host "`n[9] Last alarm events:" -ForegroundColor Yellow
docker compose exec db psql -U wkr -d wkr -P pager=off -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, alarm_message from wkr.alarm_events order by ts desc limit 10;"

Write-Host "`n[10] PostgreSQL external port:" -ForegroundColor Yellow
docker compose port db 5432

Write-Host "`n=== CHECK COMPLETE ===" -ForegroundColor Cyan