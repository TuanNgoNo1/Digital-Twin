$ErrorActionPreference = "Stop"

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script as Administrator."
}

$root = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main"
$cameraStart = Join-Path $root "Start-CameraSnapshot.ps1"
$watchdogStart = Join-Path $root "Start-LabServiceWatchdog.ps1"

$watchdogs = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -eq "powershell.exe" -and $_.CommandLine -match "LabServiceWatchdog\.ps1"
}
$watchdogs | ForEach-Object {
    Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
}

$managed = Get-CimInstance Win32_Process | Where-Object {
    ($_.Name -eq "python.exe" -and $_.CommandLine -match "browser_camera_worker\.py") -or
    ($_.Name -eq "msedge.exe" -and $_.CommandLine -match "utility-sub-type=video_capture") -or
    ($_.Name -eq "msedge.exe" -and $_.CommandLine -match "edge-profile-(dual|cam1|cam2)")
}
$managed | Sort-Object { if ($_.Name -eq "msedge.exe") { 0 } else { 1 } } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

Start-Sleep -Seconds 2
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $cameraStart

$checks = @(
    @{ Key = "cam1"; Port = 5011; Snapshot = Join-Path $root "camera_www\cam1\snapshot.jpg" },
    @{ Key = "cam2"; Port = 5012; Snapshot = Join-Path $root "camera_www\cam2\snapshot.jpg" }
)
foreach ($camera in $checks) {
    $health = Invoke-RestMethod `
        -Uri "http://127.0.0.1:$($camera.Port)/health/$($camera.Key)" `
        -TimeoutSec 3
    $age = ((Get-Date) - (Get-Item -LiteralPath $camera.Snapshot).LastWriteTime).TotalSeconds
    Write-Host "$($camera.Key): worker=$($camera.Port), state=$($health.state), snapshotAge=$([math]::Round($age, 1))s"
}

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $watchdogStart
Write-Host "Independent camera workers are running. PLC and Java services were not touched."
