@echo off
cd /d "%~dp0"
echo Restarting only the Spring Boot backend...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Restart-BackendOnly.ps1"
if errorlevel 1 (
    echo.
    echo BACKEND RESTART FAILED
    echo Check backend.err.log in this folder.
)
pause
