$ErrorActionPreference = "Stop"

$adminScript = Join-Path $PSScriptRoot "Install-RemoteAppTool-Admin.ps1"

$process = Start-Process powershell.exe `
    -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$adminScript`"") `
    -Verb RunAs `
    -Wait `
    -PassThru

Write-Host "Elevated installer process exited with code $($process.ExitCode)."
