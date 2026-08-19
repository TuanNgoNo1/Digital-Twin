@echo off
setlocal
set "FX3U_SERIAL_PORT=COM5"
set "FX3U_BAUD_RATE=9600"
set "FX3U_HTTP_HOST=127.0.0.1"
set "FX3U_HTTP_PORT=5002"
set "FX3U_STALE_SECONDS=3"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0gateway\fx3u_telemetry_gateway\Build-Fx3uTelemetryGateway.ps1"
if errorlevel 1 (
    echo BUILD FAILED
    pause
    exit /b 1
)

"%~dp0gateway\fx3u_telemetry_gateway\bin\Fx3uTelemetryGateway.exe"
endlocal
