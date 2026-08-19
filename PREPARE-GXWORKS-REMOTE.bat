@echo off
setlocal

net session >nul 2>&1
if not "%errorlevel%"=="0" (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0ops\server-control\Prepare-GXWorksRemoteMode.ps1"

if not "%errorlevel%"=="0" (
    echo.
    echo KHONG THE CHUYEN SANG CHE DO GX WORKS2.
    echo Hay chup lai noi dung cua so nay de kiem tra.
) else (
    echo.
    echo DA SAN SANG CHO GX WORKS2 QUA GUACAMOLE.
    echo Mo: http://103.238.69.131:8080/gxworks2/
)

echo.
pause
