$ErrorActionPreference = "Stop"

$adminScript = Join-Path $PSScriptRoot "Install-DockerNoReboot-Admin.ps1"
Start-Process -FilePath "powershell.exe" `
    -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$adminScript`"" `
    -Verb RunAs `
    -Wait

Write-Host "Installer finished or was cancelled. Log:"
Write-Host (Join-Path $PSScriptRoot "install-docker-no-reboot.log")

