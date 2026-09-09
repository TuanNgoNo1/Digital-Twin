$ErrorActionPreference = "Stop"

$root = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main"
$startScript = Join-Path $root "Start-CameraSnapshot.ps1"
$snapshot = Join-Path $root "camera_www\snapshot.jpg"

Write-Host "Stopping old camera worker/Edge capture processes..."
$targets = Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -like "*browser_camera_worker.py*" -or
    $_.CommandLine -like "*camera_runtime*edge-profile*"
}
foreach ($target in $targets) {
    try {
        Stop-Process -Id $target.ProcessId -Force -ErrorAction Stop
        Write-Host ("Stopped {0} PID {1}" -f $target.Name, $target.ProcessId)
    } catch {
        Write-Warning ("Could not stop PID {0}: {1}" -f $target.ProcessId, $_.Exception.Message)
    }
}

$deadline = (Get-Date).AddSeconds(10)
do {
    $listener = Get-NetTCPConnection -State Listen -LocalPort 5010 -ErrorAction SilentlyContinue
    if (-not $listener) { break }
    Start-Sleep -Milliseconds 300
} while ((Get-Date) -lt $deadline)

Write-Host "Starting camera worker..."
powershell -NoProfile -ExecutionPolicy Bypass -File $startScript

$deadline = (Get-Date).AddSeconds(30)
do {
    Start-Sleep -Milliseconds 500
    try {
        $health = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:5010/health" -TimeoutSec 2
        $state = $health.Content | ConvertFrom-Json
        if ($state.state -eq "online" -and (Test-Path -LiteralPath $snapshot)) {
            Write-Host ("Camera online: {0} {1}x{2}" -f $state.camera, $state.width, $state.height)
            Write-Host "Open: http://103.238.69.131:8080/cam/"
            exit 0
        }
    } catch {}
} while ((Get-Date) -lt $deadline)

throw "Camera did not become online within 30 seconds. Check camera-worker.err.log."
