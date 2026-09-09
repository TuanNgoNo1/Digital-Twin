$ErrorActionPreference = "Stop"

$python = "C:\Users\Server-Lab602\AppData\Local\Programs\Python\Python311\python.exe"
$worker = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\camera_runtime\browser_camera_worker.py"
$logRoot = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main"

if (-not (Test-Path -LiteralPath $python)) {
    throw "Python not found: $python"
}
if (-not (Test-Path -LiteralPath $worker)) {
    throw "Browser camera worker not found: $worker"
}

$cameras = @(
    @{
        Key = "cam2"
        Port = 5012
        Profile = Join-Path $logRoot "camera_runtime\edge-profile-cam2"
        OutLog = Join-Path $logRoot "camera-worker-cam2.out.log"
        ErrLog = Join-Path $logRoot "camera-worker-cam2.err.log"
    },
    @{
        Key = "cam1"
        Port = 5011
        Profile = Join-Path $logRoot "camera_runtime\edge-profile-cam1"
        OutLog = Join-Path $logRoot "camera-worker-cam1.out.log"
        ErrLog = Join-Path $logRoot "camera-worker-cam1.err.log"
    }
)

foreach ($camera in $cameras) {
    $listener = Get-NetTCPConnection -State Listen -LocalPort $camera.Port -ErrorAction SilentlyContinue
    if ($listener) {
        continue
    }

    Start-Process `
        -FilePath $python `
        -ArgumentList @(
            $worker,
            "--camera", $camera.Key,
            "--port", $camera.Port,
            "--profile", $camera.Profile
        ) `
        -WorkingDirectory (Split-Path -Parent $worker) `
        -WindowStyle Hidden `
        -RedirectStandardOutput $camera.OutLog `
        -RedirectStandardError $camera.ErrLog

    $cameraDeadline = (Get-Date).AddSeconds(12)
    do {
        Start-Sleep -Milliseconds 500
        try {
            $status = Invoke-RestMethod `
                -Uri "http://127.0.0.1:$($camera.Port)/health/$($camera.Key)" `
                -TimeoutSec 2
        }
        catch {
            $status = $null
        }
    } while ($status.state -ne "online" -and (Get-Date) -lt $cameraDeadline)
}

$deadline = (Get-Date).AddSeconds(25)
$snapshots = @(
    (Join-Path $logRoot "camera_www\cam1\snapshot.jpg"),
    (Join-Path $logRoot "camera_www\cam2\snapshot.jpg")
)
do {
    Start-Sleep -Milliseconds 500
    $freshSnapshots = @($snapshots | Where-Object {
        (Test-Path -LiteralPath $_) -and
        (((Get-Date) - (Get-Item -LiteralPath $_).LastWriteTime).TotalSeconds -le 5)
    })
} while ($freshSnapshots.Count -lt $snapshots.Count -and (Get-Date) -lt $deadline)

if ($freshSnapshots.Count -lt $snapshots.Count) {
    throw "One or both camera workers did not create a fresh snapshot within 25 seconds."
}
