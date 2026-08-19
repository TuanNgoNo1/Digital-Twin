$ErrorActionPreference = "Stop"

$adminScript = Join-Path $PSScriptRoot "Configure-GXWorksRemoteApp-Admin.ps1"

$process = Start-Process powershell.exe `
    -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$adminScript`"") `
    -Verb RunAs `
    -Wait `
    -PassThru

if ($process.ExitCode -ne 0) {
    throw "RemoteApp configuration failed with exit code $($process.ExitCode)."
}

Write-Host "GX Works2 RemoteApp configuration completed."
