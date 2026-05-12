$ErrorActionPreference = "Stop"

Write-Output "=== WKR ANGULAR CLIENT START ==="

$Root = Split-Path -Parent $PSScriptRoot
$Client = Join-Path $Root "client\wkr-client"

if (!(Test-Path $Client)) {
    Write-Output "Angular client folder not found"
    Write-Output $Client
    exit 1
}

Push-Location $Client

Write-Output ""
Write-Output "[1] Node version"
node -v

Write-Output ""
Write-Output "[2] NPM version"
npm -v

Write-Output ""
Write-Output "[3] Installing packages if needed"
if (!(Test-Path ".\node_modules")) {
    npm install
}

Write-Output ""
Write-Output "[4] Starting Angular client"
npm start

Pop-Location