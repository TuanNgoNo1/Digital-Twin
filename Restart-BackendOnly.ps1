$ErrorActionPreference = "Stop"

$backendScript = Join-Path $PSScriptRoot "Start-BackendLoopback.ps1"
$jarPath = "C:\Users\Server-Lab602\PTnew\pdtwin-backend-0.0.1-SNAPSHOT.jar"

if (-not (Test-Path -LiteralPath $jarPath)) {
    throw "Backend JAR not found: $jarPath"
}

if (-not (Test-Path -LiteralPath $backendScript)) {
    throw "Backend start script not found: $backendScript"
}

$backendPid = $null
foreach ($line in (netstat -ano -p tcp)) {
    if ($line -match "^\s*TCP\s+127\.0\.0\.1:8080\s+\S+\s+LISTENING\s+(\d+)\s*$") {
        $backendPid = [int]$Matches[1]
        break
    }
}

if ($backendPid) {
    $backendProcess = Get-Process -Id $backendPid -ErrorAction Stop
    if ($backendProcess.ProcessName -ne "java") {
        throw "Port 127.0.0.1:8080 is owned by $($backendProcess.ProcessName) PID $backendPid, not Java. Nothing was stopped."
    }

    Write-Host "Stopping old Java backend PID $backendPid..."
    Stop-Process -Id $backendPid -Force
    Wait-Process -Id $backendPid -Timeout 15 -ErrorAction SilentlyContinue
}
else {
    Write-Host "No old Java backend is listening on 127.0.0.1:8080."
}

Write-Host "Starting backend from:"
Write-Host "  $jarPath"
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $backendScript

$deadline = (Get-Date).AddSeconds(60)
$newPid = $null
do {
    Start-Sleep -Seconds 2
    foreach ($line in (netstat -ano -p tcp)) {
        if ($line -match "^\s*TCP\s+127\.0\.0\.1:8080\s+\S+\s+LISTENING\s+(\d+)\s*$") {
            $newPid = [int]$Matches[1]
            break
        }
    }
} while (-not $newPid -and (Get-Date) -lt $deadline)

if (-not $newPid) {
    throw "Backend did not start within 60 seconds. Check backend.err.log."
}

$response = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:8080/" -TimeoutSec 15
if ($response.StatusCode -ne 200) {
    throw "Backend started as PID $newPid but returned HTTP $($response.StatusCode)."
}

Write-Host ""
Write-Host "BACKEND RESTART SUCCESS"
Write-Host "  PID: $newPid"
Write-Host "  HTTP: 200"
Write-Host "  Public URL: http://103.238.69.131:8080/"
