$ErrorActionPreference = "Continue"

Write-Output "=== WKR RUL SYSTEM CHECK ==="

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

Push-Location $Infra

Write-Output ""
Write-Output "[1] Docker containers"
docker compose ps

Write-Output ""
Write-Output "[2] Backend health"
try {
    Invoke-RestMethod "http://localhost:8000/health"
} catch {
    Write-Output "Backend is not available"
    Write-Output $_.Exception.Message
}

Write-Output ""
Write-Output "[3] ML health"
try {
    Invoke-RestMethod "http://localhost:8001/health"
} catch {
    Write-Output "ML service is not available"
    Write-Output $_.Exception.Message
}

Write-Output ""
Write-Output "[4] Database tables"
docker compose exec db psql -U wkr -d wkr -c "\dt wkr.*"

Write-Output ""
Write-Output "[5] Telemetry count"
docker compose exec db psql -U wkr -d wkr -c "select count(*) from wkr.telemetry_spindle;"

Write-Output ""
Write-Output "[6] Last telemetry"
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, spindle_rpm, spindle_current_a, spindle_power_kw, feed_mm_min, cut_flag, machine_state, spindle_state, stop_required, stop_reason, control_action, program from wkr.telemetry_spindle order by id desc limit 10;"

Write-Output ""
Write-Output "[7] Last RUL predictions"
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, state, model_version from wkr.rul_predictions order by ts desc limit 10;"

Write-Output ""
Write-Output "[8] Last alarm events"
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, alarm_message from wkr.alarm_events order by ts desc limit 10;"

Write-Output ""
Write-Output "[9] Last machine events"
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, event_code, event_level, event_message, machine_state, spindle_state, stop_reason, control_action from wkr.machine_events order by ts desc limit 10;"

Write-Output ""
Write-Output "[10] Dashboard current API"
try {
    Invoke-RestMethod "http://localhost:8000/api/dashboard/current?machineId=HAAS_VF2_NGC_01&toolId=T12"
} catch {
    Write-Output "Dashboard current API failed"
    Write-Output $_.Exception.Message
}

Write-Output ""
Write-Output "[11] PostgreSQL external port"
docker compose port db 5432

Pop-Location

Write-Output ""
Write-Output "=== CHECK COMPLETE ==="