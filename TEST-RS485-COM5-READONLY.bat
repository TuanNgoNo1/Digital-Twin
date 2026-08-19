@echo off
cd /d "%~dp0"
if not exist "%~dp0gateway\modbus_rtu_gateway\bin\ModbusRtuGateway.exe" (
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0gateway\modbus_rtu_gateway\Build-ModbusRtuGateway.ps1"
  if errorlevel 1 goto :done
)
"%~dp0gateway\modbus_rtu_gateway\bin\ModbusRtuGateway.exe" probe D500 10

:done
echo.
pause
