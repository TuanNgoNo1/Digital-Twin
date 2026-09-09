$ErrorActionPreference = "Stop"

$workingDirectory = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\gateway\fx3u_telemetry_gateway"
$gateway = Join-Path $workingDirectory "bin\Fx3uTelemetryGateway.exe"

if (-not (Test-Path -LiteralPath $gateway)) {
    throw "Telemetry gateway not found: $gateway"
}

$listener = Get-NetTCPConnection -State Listen -LocalPort 5002 -ErrorAction SilentlyContinue
if ($listener) {
    exit 0
}

$env:FX3U_SERIAL_PORT = "COM5"
$env:FX3U_BAUD_RATE = "9600"
$env:FX3U_HTTP_HOST = "127.0.0.1"
$env:FX3U_HTTP_PORT = "5002"
$env:FX3U_STALE_SECONDS = "3"

Start-Process `
    -FilePath $gateway `
    -WorkingDirectory $workingDirectory `
    -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $PSScriptRoot "telemetry-gateway.out.log") `
    -RedirectStandardError (Join-Path $PSScriptRoot "telemetry-gateway.err.log")

$deadline = (Get-Date).AddSeconds(15)
$health = $null
do {
    Start-Sleep -Milliseconds 400
    try {
        $health = Invoke-RestMethod -Uri "http://127.0.0.1:5002/health" -TimeoutSec 2
    }
    catch {
        $health = $null
    }
} while (-not $health -and (Get-Date) -lt $deadline)

if (-not $health) {
    throw "Telemetry gateway did not start on port 5002."
}
