param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("GxWorks", "Gateway")]
    [string]$Mode,

    [switch]$LogoffPlcStudent
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$prepareGxWorks = Join-Path $PSScriptRoot "Prepare-GXWorksRemoteMode.ps1"
$logoffStudents = Join-Path $PSScriptRoot "Logoff-PlcStudentSessions-Admin.ps1"
$startGateway = Join-Path $repositoryRoot "Start-PlcGateway.ps1"
$getStatus = Join-Path $PSScriptRoot "Get-LabModeStatus.ps1"

foreach ($requiredFile in @($prepareGxWorks, $logoffStudents, $startGateway, $getStatus)) {
    if (-not (Test-Path -LiteralPath $requiredFile)) {
        throw "Required lab-control file is missing: $requiredFile"
    }
}

if ($Mode -eq "GxWorks") {
    & $prepareGxWorks

    $status = & $getStatus
    if (-not $status.PlcPortAvailable) {
        throw "Serial port $($status.PlcComPort) was not released for GX Works2."
    }

    [pscustomobject]@{
        Success = $true
        RequestedMode = "GXWORKS"
        Status = $status
    }
    exit 0
}

if ($LogoffPlcStudent) {
    & $logoffStudents
}
else {
    $statusBefore = & $getStatus
    if ($statusBefore.PlcStudentSessions.Count -gt 0) {
        throw "A plc_student session still exists. End/logoff that session before Gateway mode."
    }
}

$deadline = (Get-Date).AddSeconds(10)
do {
    $gxWorks = @(Get-Process -Name GD2 -ErrorAction SilentlyContinue)
    if ($gxWorks.Count -eq 0) {
        break
    }
    Start-Sleep -Milliseconds 250
} while ((Get-Date) -lt $deadline)

if ($gxWorks.Count -gt 0) {
    throw "GX Works2 is still running. Save/close it or log off its Windows session first."
}

# Gateway mode uses the Python gateway. PlcBridge must not compete for COM3.
# Both hold the port CONTINUOUSLY, so they genuinely cannot coexist (unlike GX Works2,
# which only holds the port while actually online - verified 2026-08-05).
Get-Process -Name PlcBridge -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

& $startGateway

$gatewayDeadline = (Get-Date).AddSeconds(20)
$gatewayHealthy = $false
do {
    Start-Sleep -Milliseconds 400
    try {
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:5000/health" -TimeoutSec 2
        $gatewayHealthy = $true
    }
    catch {
        $gatewayHealthy = $false
    }
} while (-not $gatewayHealthy -and (Get-Date) -lt $gatewayDeadline)

if (-not $gatewayHealthy) {
    throw "PLC gateway did not become healthy on http://127.0.0.1:5000/health."
}

$statusAfter = & $getStatus
[pscustomobject]@{
    Success = $true
    RequestedMode = "GATEWAY"
    Status = $statusAfter
}
