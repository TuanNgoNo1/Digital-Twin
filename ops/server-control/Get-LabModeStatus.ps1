param(
    [switch]$AsJson
)

$ErrorActionPreference = "Stop"

function Get-PlcStudentSessions {
    $rows = @()
    $lines = & query.exe user 2>&1

    foreach ($line in $lines) {
        if ($line -match '^\s*>?\s*plc_student\s+(?:\S+\s+)?(\d+)\s+(Active|Disc)\b') {
            $rows += [pscustomobject]@{
                SessionId = [int]$Matches[1]
                State = $Matches[2]
            }
        }
    }

    @($rows | Sort-Object SessionId -Unique)
}

# Bai 1 mode ownership is tied to COM3. COM5 and COM8 belong to other lessons.
$PlcComPort = "COM3"

function Test-PlcPortAvailable {
    $ports = [System.IO.Ports.SerialPort]::GetPortNames()
    if ($ports -notcontains $script:PlcComPort) {
        return $false
    }

    $port = [System.IO.Ports.SerialPort]::new($script:PlcComPort, 9600)
    try {
        $port.Open()
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($port.IsOpen) {
            $port.Close()
        }
        $port.Dispose()
    }
}

$gatewayHealthy = $false
try {
    $null = Invoke-RestMethod -Uri "http://127.0.0.1:5000/health" -TimeoutSec 2
    $gatewayHealthy = $true
}
catch {
    $gatewayHealthy = $false
}

$sessions = @(Get-PlcStudentSessions)
$gxWorksPids = @(Get-Process -Name GD2 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
$plcPortAvailable = Test-PlcPortAvailable

$mode = if ($gatewayHealthy) {
    "GATEWAY"
}
elseif ($gxWorksPids.Count -gt 0 -or $sessions.Count -gt 0) {
    "GXWORKS"
}
elseif ($plcPortAvailable) {
    "RELEASED"
}
else {
    "BUSY_UNKNOWN"
}

$result = [pscustomobject]@{
    Mode = $mode
    GatewayHealthy = $gatewayHealthy
    PlcComPort = $PlcComPort
    PlcPortAvailable = $plcPortAvailable
    GxWorksPids = $gxWorksPids
    PlcStudentSessions = $sessions
    CheckedAt = (Get-Date).ToString("o")
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 5 -Compress
}
else {
    $result
}
