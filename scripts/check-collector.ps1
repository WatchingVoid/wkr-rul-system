$ErrorActionPreference = "Stop"

Write-Output "=== WKR RUL SYSTEM: COLLECTOR CHECK ==="

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

Set-Location $Infra

Write-Output ""
Write-Output "[1] Docker containers:"
docker compose ps

Write-Output ""
Write-Output "[2] Collector logs:"
docker compose logs --tail 80 collector

Write-Output ""
Write-Output "[3] Telemetry count:"
docker compose exec db psql -U wkr -d wkr -c "select count(*) from wkr.telemetry_spindle;"

Write-Output ""
Write-Output "[4] Last telemetry rows:"
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, spindle_rpm, spindle_current_a, spindle_power_kw, feed_mm_min, cut_flag, machine_state, spindle_state, stop_required, stop_reason, control_action, program from wkr.telemetry_spindle order by id desc limit 15;"

Write-Output ""
Write-Output "[5] Last RUL predictions:"
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, state, model_version from wkr.rul_predictions order by ts desc limit 15;"

Write-Output ""
Write-Output "[6] Last alarm events:"
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, alarm_message from wkr.alarm_events order by ts desc limit 15;"

Write-Output ""
Write-Output "[7] Last machine events:"
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, event_code, event_level, event_message, machine_state, spindle_state, stop_reason, control_action from wkr.machine_events order by ts desc limit 15;"

Write-Output ""
Write-Output "[8] Backend dashboard current:"
try {
    Invoke-RestMethod "http://localhost:8000/api/dashboard/current?machineId=HAAS_VF2_NGC_01&toolId=T12"
}
catch {
    Write-Output "Backend dashboard request failed"
    Write-Output $_.Exception.Message
}

Write-Output ""
Write-Output "=== COLLECTOR CHECK COMPLETE ==="