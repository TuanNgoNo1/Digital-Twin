param(
    [switch]$SkipCameraDeviceRestart
)

$ErrorActionPreference = "Stop"

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script as Administrator."
}

$root = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main"
$cameraStart = Join-Path $root "Start-CameraSnapshot.ps1"
$plcStart = Join-Path $root "Start-PlcGateway.ps1"
$telemetryStart = Join-Path $root "Start-TelemetryGateway.ps1"

foreach ($required in @($cameraStart, $plcStart, $telemetryStart)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Required startup script is missing: $required"
    }
}

Write-Host "== Stop stale camera and PLC owners =="
Get-Process -Name PlcBridge, Fx3uTelemetryGateway -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

$managedProcesses = Get-CimInstance Win32_Process | Where-Object {
    ($_.Name -eq "python.exe" -and $_.CommandLine -match "browser_camera_worker\.py") -or
    ($_.Name -eq "python.exe" -and $_.CommandLine -match "PiGatewayFxplc.+gateway\.py") -or
    ($_.Name -eq "msedge.exe" -and $_.CommandLine -match "utility-sub-type=video_capture") -or
    ($_.Name -eq "msedge.exe" -and $_.CommandLine -match "edge-profile-(dual|cam1|cam2)")
}
foreach ($process in $managedProcesses) {
    Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Seconds 2

if (-not $SkipCameraDeviceRestart) {
    Write-Host "== Restart the two USB camera devices =="
    $cameraIds = @(
        "USB\VID_4C4A&PID_4A55&MI_00\6&378a238&0&0000",
        "USB\VID_0AC8&PID_3450&MI_00\8&1330c652&0&0000"
    )
    foreach ($cameraId in $cameraIds) {
        & pnputil.exe /restart-device $cameraId
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not restart camera device: $cameraId"
        }
    }
    Start-Sleep -Seconds 3
}

Write-Host "== Start camera worker =="
Start-Process powershell.exe `
    -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $cameraStart) `
    -WorkingDirectory $root `
    -WindowStyle Hidden

Write-Host "== Start COM3 PLC gateway =="
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $plcStart

Write-Host "== Start COM5 telemetry gateway =="
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $telemetryStart

function Wait-HttpJson {
    param(
        [string]$Uri,
        [int]$Seconds = 30
    )

    $deadline = (Get-Date).AddSeconds($Seconds)
    do {
        Start-Sleep -Milliseconds 500
        try {
            return Invoke-RestMethod -Uri $Uri -TimeoutSec 2
        }
        catch {
        }
    } while ((Get-Date) -lt $deadline)

    return $null
}

Write-Host "== Verify services =="
$plc = Wait-HttpJson "http://127.0.0.1:5000/health" 25
$telemetry = Wait-HttpJson "http://127.0.0.1:5002/health" 25
$cam1 = Wait-HttpJson "http://127.0.0.1:5011/health/cam1" 35
$cam2 = Wait-HttpJson "http://127.0.0.1:5012/health/cam2" 35

$snapshot1 = Join-Path $root "camera_www\cam1\snapshot.jpg"
$snapshot2 = Join-Path $root "camera_www\cam2\snapshot.jpg"
$snapshotDeadline = (Get-Date).AddSeconds(35)
do {
    if ((Test-Path -LiteralPath $snapshot1) -and (Test-Path -LiteralPath $snapshot2)) {
        break
    }
    Start-Sleep -Milliseconds 500
} while ((Get-Date) -lt $snapshotDeadline)

$result = [pscustomobject]@{
    PlcGateway5000 = [bool]$plc
    TelemetryGateway5002 = [bool]$telemetry
    CameraWorker5011 = [bool]$cam1
    CameraWorker5012 = [bool]$cam2
    Cam1State = if ($cam1) { $cam1.state } else { "offline" }
    Cam2State = if ($cam2) { $cam2.state } else { "offline" }
    Cam1Snapshot = Test-Path -LiteralPath $snapshot1
    Cam2Snapshot = Test-Path -LiteralPath $snapshot2
    CheckedAt = (Get-Date).ToString("o")
}

$result | Format-List

if (-not $result.PlcGateway5000) {
    Write-Warning "PLC gateway is still offline. Check plc-gateway.err.log and COM3 ownership."
}
if (-not $result.TelemetryGateway5002) {
    Write-Warning "Telemetry gateway is still offline. Check telemetry-gateway.err.log and COM5 ownership."
}
if (-not $result.Cam1Snapshot -or -not $result.Cam2Snapshot) {
    Write-Warning "One or both cameras did not create a fresh snapshot. Check camera-worker.err.log."
}

Write-Host "Recovery finished. Java, Caddy and the backend were not restarted."
