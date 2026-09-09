$ErrorActionPreference = "Continue"

$root = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main"
$cameraStart = Join-Path $root "Start-CameraSnapshot.ps1"
$plcStart = Join-Path $root "Start-PlcGateway.ps1"
$telemetryStart = Join-Path $root "Start-TelemetryGateway.ps1"
$cameras = @(
    @{ Key = "cam1"; Port = 5011; Snapshot = Join-Path $root "camera_www\cam1\snapshot.jpg" },
    @{ Key = "cam2"; Port = 5012; Snapshot = Join-Path $root "camera_www\cam2\snapshot.jpg" }
)
$mutex = [Threading.Mutex]::new($false, "Local\DigitalTwinLabServiceWatchdog")
$ownsMutex = $false

try {
    try {
        $ownsMutex = $mutex.WaitOne(0, $false)
    }
    catch [Threading.AbandonedMutexException] {
        $ownsMutex = $true
    }
    if (-not $ownsMutex) {
        exit 0
    }

    Write-Output "$(Get-Date -Format o) lab watchdog started"
    Start-Sleep -Seconds 20

    function Test-Http {
        param([string]$Uri)
        try {
            $null = Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec 2
            return $true
        }
        catch {
            return $false
        }
    }

    function Test-FreshFile {
        param([string]$Path, [int]$MaxAgeSeconds = 5)
        if (-not (Test-Path -LiteralPath $Path)) {
            return $false
        }
        return ((Get-Date) - (Get-Item -LiteralPath $Path).LastWriteTime).TotalSeconds -le $MaxAgeSeconds
    }

    function Stop-ListenerProcess {
        param([int]$Port)
        $rows = Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue
        foreach ($row in $rows) {
            & taskkill.exe /PID $row.OwningProcess /T /F 2>$null | Out-Null
        }
    }

    function Restart-Camera {
        param([hashtable]$Camera)
        Write-Output "$(Get-Date -Format o) $($Camera.Key) stale; restarting only $($Camera.Key)"
        Stop-ListenerProcess $Camera.Port
        Start-Sleep -Seconds 2
        Start-Process powershell.exe `
            -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $cameraStart) `
            -WorkingDirectory $root `
            -WindowStyle Hidden
    }

    function Restart-Telemetry {
        Write-Output "$(Get-Date -Format o) COM5 telemetry stale; restarting gateway"
        Stop-ListenerProcess 5002
        Start-Sleep -Seconds 2
        Start-Process powershell.exe `
            -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $telemetryStart) `
            -WorkingDirectory $root `
            -WindowStyle Hidden
    }

    while ($true) {
        foreach ($camera in $cameras) {
            $healthUri = "http://127.0.0.1:$($camera.Port)/health/$($camera.Key)"
            if (-not (Test-FreshFile $camera.Snapshot) -or -not (Test-Http $healthUri)) {
                Restart-Camera $camera
                Start-Sleep -Seconds 15
            }
        }

        $telemetryHealthy = $false
        try {
            $health = Invoke-RestMethod -Uri "http://127.0.0.1:5002/health" -TimeoutSec 2
            $lastFrame = [datetime]::Parse($health.lastFrameAt).ToUniversalTime()
            $telemetryHealthy = (([datetime]::UtcNow - $lastFrame).TotalSeconds -le 5)
        }
        catch {
            $telemetryHealthy = $false
        }
        if (-not $telemetryHealthy) {
            Restart-Telemetry
            Start-Sleep -Seconds 10
        }

        if (-not (Test-Http "http://127.0.0.1:5000/health")) {
            $gxWorksActive = [bool](Get-Process -Name GD2 -ErrorAction SilentlyContinue)
            $plcBridgeActive = [bool](Get-Process -Name PlcBridge -ErrorAction SilentlyContinue)
            if (-not $gxWorksActive -and -not $plcBridgeActive) {
                Write-Output "$(Get-Date -Format o) COM3 gateway offline; restarting gateway"
                Start-Process powershell.exe `
                    -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $plcStart) `
                    -WorkingDirectory $root `
                    -WindowStyle Hidden
                Start-Sleep -Seconds 10
            }
        }

        Start-Sleep -Seconds 10
    }
}
finally {
    if ($ownsMutex) {
        $mutex.ReleaseMutex()
    }
    $mutex.Dispose()
}
