$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Infra = Join-Path $Root "infra"

$BackendUrl = "http://localhost:8000"
$TelemetryUrl = "$BackendUrl/api/telemetry"

$MachineId = "HAAS_VF2_NGC_01"
$ToolId = "T12"
$Diameter = 10.0

function Send-TelemetryFrame {
    param(
        [string]$Program,
        [int]$Rpm,
        [double]$CurrentA,
        [double]$PowerKw
    )

    $body = @{
        ts = (Get-Date).ToUniversalTime().ToString("o")
        machineId = $MachineId
        toolId = $ToolId
        spindleRpm = $Rpm
        spindleCurrentA = [single]$CurrentA
        spindlePowerKw = [single]$PowerKw
        feedMmMin = 1200
        program = $Program
        cutFlag = $true
        toolDiameterMm = [single]$Diameter
        spindleTorqueNm = $null
    } | ConvertTo-Json

    Invoke-RestMethod `
        -Method Post `
        -Uri $TelemetryUrl `
        -Body $body `
        -ContentType "application/json" | Out-Null
}

function Show-LastPrediction {
    Set-Location $Infra

    docker compose exec db psql -U wkr -d wkr -P pager=off -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, state, model_version from wkr.rul_predictions order by ts desc limit 5;"
}

function Show-LastAlarms {
    Set-Location $Infra

    docker compose exec db psql -U wkr -d wkr -P pager=off -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, alarm_message from wkr.alarm_events order by ts desc limit 10;"
}

Write-Host "=== WKR RUL SYSTEM: TOOL LIFECYCLE TEST ===" -ForegroundColor Cyan

Set-Location $Infra

Write-Host "`n[1/8] Checking services..." -ForegroundColor Yellow
docker compose ps

Write-Host "`nChecking backend health..." -ForegroundColor Yellow
Invoke-RestMethod "$BackendUrl/health" | Out-Host

Write-Host "`nChecking ML health..." -ForegroundColor Yellow
Invoke-RestMethod "http://localhost:8001/health" | Out-Host

Write-Host "`n[2/8] Cleaning old telemetry, predictions and alarms..." -ForegroundColor Yellow
docker compose exec db psql -U wkr -d wkr -c "truncate table wkr.alarm_events, wkr.rul_predictions, wkr.telemetry_spindle restart identity;"

Write-Host "`n[3/8] Sending NORMAL tool state telemetry..." -ForegroundColor Green

for ($i = 0; $i -lt 70; $i++) {
    $rpm = 8200 + (Get-Random -Minimum -20 -Maximum 20)
    $current = 10.0 + (Get-Random -Minimum -10 -Maximum 10) / 100.0
    $power = 2.1 + (Get-Random -Minimum -10 -Maximum 10) / 100.0

    Send-TelemetryFrame `
        -Program "OP10_NORMAL_TEST" `
        -Rpm $rpm `
        -CurrentA $current `
        -PowerKw $power

    Start-Sleep -Milliseconds 60
}

Write-Host "`nWaiting for RulWorker..." -ForegroundColor DarkGray
Start-Sleep -Seconds 8

Write-Host "`nLast predictions after NORMAL stage:" -ForegroundColor Green
Show-LastPrediction

Write-Host "`n[4/8] Sending WARNING-like telemetry..." -ForegroundColor Yellow

for ($i = 0; $i -lt 90; $i++) {
    $wear = $i / 89.0

    $rpm = 8200 + (Get-Random -Minimum -40 -Maximum 40)
    $current = 17.5 + 4.5 * $wear + (Get-Random -Minimum -20 -Maximum 20) / 100.0
    $power = 5.0 + 1.6 * $wear + (Get-Random -Minimum -15 -Maximum 15) / 100.0

    Send-TelemetryFrame `
        -Program "OP10_WARNING_TEST" `
        -Rpm $rpm `
        -CurrentA $current `
        -PowerKw $power

    Start-Sleep -Milliseconds 60
}

Write-Host "`nWaiting for RulWorker..." -ForegroundColor DarkGray
Start-Sleep -Seconds 8

Write-Host "`nLast predictions after WARNING stage:" -ForegroundColor Yellow
Show-LastPrediction

Write-Host "`nLast alarms after WARNING stage:" -ForegroundColor Yellow
Show-LastAlarms

Write-Host "`n[5/8] Sending CRITICAL tool state telemetry..." -ForegroundColor Red

for ($i = 0; $i -lt 110; $i++) {
    $wear = $i / 109.0

    $rpm = 8200 + (Get-Random -Minimum -80 -Maximum 80)
    $current = 22.0 + 4.0 * $wear + (Get-Random -Minimum -30 -Maximum 30) / 100.0
    $power = 7.0 + 1.5 * $wear + (Get-Random -Minimum -20 -Maximum 20) / 100.0

    Send-TelemetryFrame `
        -Program "OP10_CRITICAL_TEST" `
        -Rpm $rpm `
        -CurrentA $current `
        -PowerKw $power

    Start-Sleep -Milliseconds 60
}

Write-Host "`nWaiting for RulWorker..." -ForegroundColor DarkGray
Start-Sleep -Seconds 10

Write-Host "`n[6/8] Last predictions after CRITICAL stage:" -ForegroundColor Red
Show-LastPrediction

Write-Host "`n[7/8] Last alarms:" -ForegroundColor Red
Show-LastAlarms

Write-Host "`n[8/8] API checks:" -ForegroundColor Cyan

Write-Host "`nLast RUL via Backend API:" -ForegroundColor Yellow
Invoke-RestMethod "$BackendUrl/api/rul/last?machineId=$MachineId&toolId=$ToolId" | Out-Host

Write-Host "`nLast alarm via Backend API:" -ForegroundColor Yellow
Invoke-RestMethod "$BackendUrl/api/alarms/last?machineId=$MachineId&toolId=$ToolId" | Out-Host

Write-Host "`n=== TOOL LIFECYCLE TEST COMPLETE ===" -ForegroundColor Cyan