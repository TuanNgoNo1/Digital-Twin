$ErrorActionPreference = "Stop"

$adminScript = Join-Path $PSScriptRoot "Install-LabSessionController-Admin.ps1"
$process = Start-Process powershell.exe `
    -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$adminScript`"") `
    -Verb RunAs `
    -Wait `
    -PassThru

if ($process.ExitCode -ne 0) {
    throw "Lab Session Controller installation failed with exit code $($process.ExitCode)."
}

Write-Host "Lab Session Controller installation completed."
