$ErrorActionPreference = "Stop"

$root = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main"
$watchdog = Join-Path $root "ops\server-control\LabServiceWatchdog.ps1"

if (-not (Test-Path -LiteralPath $watchdog)) {
    throw "Watchdog script not found: $watchdog"
}

Start-Process powershell.exe `
    -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $watchdog) `
    -WorkingDirectory $root `
    -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $root "lab-watchdog.out.log") `
    -RedirectStandardError (Join-Path $root "lab-watchdog.err.log")
