@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0gateway\modbus_rtu_gateway\Build-ModbusRtuGateway.ps1"
if errorlevel 1 goto :done
"%~dp0gateway\modbus_rtu_gateway\bin\ModbusRtuGateway.exe" loopback

:done
echo.
pause
