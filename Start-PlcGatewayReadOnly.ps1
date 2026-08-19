$ErrorActionPreference = "Stop"

$workingDirectory = "D:\MIGRATION_2026-06-29\Windows_Readable\PiGatewayFxplc"
$python = Join-Path $workingDirectory ".venv-win\Scripts\python.exe"
$gateway = Join-Path $workingDirectory "gateway.py"

if (-not (Test-Path -LiteralPath $python)) {
    throw "Gateway Python not found: $python"
}

if (-not (Test-Path -LiteralPath $gateway)) {
    throw "Gateway script not found: $gateway"
}

$listener = Get-NetTCPConnection -State Listen -LocalPort 5000 -ErrorAction SilentlyContinue
if ($listener) {
    exit 0
}

# Keep this aligned with Start-PlcGateway.ps1.
# Bai 1 uses COM3. COM5 and COM8 are reserved for other practical lessons.
$env:FXPLC_SERIAL_PORT = "COM3"
$env:FXPLC_HTTP_HOST = "127.0.0.1"
$env:FXPLC_HTTP_PORT = "5000"
$env:FXPLC_ALLOW_WRITES = "0"
$env:FXPLC_PULSE_SECONDS = "0.1"

Start-Process `
    -FilePath $python `
    -ArgumentList @($gateway) `
    -WorkingDirectory $workingDirectory `
    -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $PSScriptRoot "plc-gateway.out.log") `
    -RedirectStandardError (Join-Path $PSScriptRoot "plc-gateway.err.log")
