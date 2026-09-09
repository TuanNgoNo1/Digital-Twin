@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0ops\server-control\Start-FullLabAfterReboot.ps1"
pause

