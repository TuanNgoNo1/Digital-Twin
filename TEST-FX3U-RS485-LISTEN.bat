@echo off
cd /d "%~dp0"
set "MODBUS_SERIAL_PORT=COM5"
set "MODBUS_BAUD_RATE=9600"
set "MODBUS_TIMEOUT_MS=250"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0gateway\modbus_rtu_gateway\Build-ModbusRtuGateway.ps1"
if errorlevel 1 goto :done

"%~dp0gateway\modbus_rtu_gateway\bin\ModbusRtuGateway.exe" listen 60

:done
echo.
pause
