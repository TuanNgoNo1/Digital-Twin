$ErrorActionPreference = "Stop"

$camStack = "D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Start-CamStack.ps1"
$watchdog = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\Start-LabServiceWatchdog.ps1"

if (-not (Test-Path -LiteralPath $camStack)) {
    throw "Missing Start-CamStack script: $camStack"
}

Start-Process powershell.exe `
    -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $camStack) `
    -WindowStyle Hidden

if (Test-Path -LiteralPath $watchdog) {
    Start-Process powershell.exe `
        -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $watchdog) `
        -WindowStyle Hidden
}

Write-Host "Requested start of web stack:"
Write-Host "  camera snapshot"
Write-Host "  Spring Boot backend on 127.0.0.1:8080"
Write-Host "  PLC HTTP gateway on 127.0.0.1:5000"
Write-Host "  Telemetry gateway on 127.0.0.1:5002 (COM5)"
Write-Host "  Caddy on :80 and 10.170.43.240:8080"
Write-Host "  camera/PLC watchdog every 10 seconds"


