$ErrorActionPreference = "Continue"

$ports = @(80, 8080, 8081, 5000, 5002, 8090, 8888, 8889, 9090, 19303, 3389, 1883)

function Get-ListeningTcpRows {
    $rows = @()
    $netstatLines = netstat -ano -p tcp | Select-String "LISTENING"
    foreach ($line in $netstatLines) {
        $text = $line.ToString().Trim()
        if ($text -notmatch "^TCP\s+(\S+)\s+\S+\s+LISTENING\s+(\d+)$") {
            continue
        }

        $localAddress = $Matches[1]
        $pidText = $Matches[2]
        if ($localAddress -notmatch ":(\d+)$") {
            continue
        }

        $rows += [pscustomobject]@{
            Address = $localAddress
            Port = [int]$Matches[1]
            PID = [int]$pidText
        }
    }

    $rows
}

Write-Host "== Listening ports =="
$portRows = @()
$listenersByPort = Get-ListeningTcpRows | Group-Object Port -AsHashTable -AsString
foreach ($port in $ports) {
    $listeners = $listenersByPort[[string]$port]
    if ($listeners) {
        foreach ($listener in $listeners) {
            $process = Get-Process -Id $listener.PID -ErrorAction SilentlyContinue
            $portRows += [pscustomobject]@{
                Port = $port
                Address = $listener.Address
                PID = $listener.PID
                Process = $process.ProcessName
                Path = $process.Path
            }
        }
    }
    else {
        $portRows += [pscustomobject]@{
            Port = $port
            Address = "-"
            PID = "-"
            Process = "not listening"
            Path = ""
        }
    }
}
$portRows | Format-Table -AutoSize

Write-Host ""
Write-Host "== Key Windows services =="
$serviceNames = @(
    "Tailscale",
    "RustDesk",
    "StarDeskService",
    "UltraViewService",
    "mosquitto",
    "MELSOFT Mediative Server",
    "WSLService",
    "TermService"
)

$serviceRows = @()
foreach ($name in $serviceNames) {
    $serviceRows += Get-Service -Name $name -ErrorAction SilentlyContinue |
        Select-Object Name, DisplayName, Status, StartType
}
$serviceRows | Format-Table -AutoSize

Write-Host ""
Write-Host "== Main app processes =="
$processNames = @(
    "caddy",
    "java",
    "python",
    "ffmpeg",
    "Fx3uTelemetryGateway",
    "node",
    "turnserver",
    "PlcBridge",
    "PLC",
    "GD2",
    "Docker Desktop",
    "com.docker.backend"
)

$processRows = @()
foreach ($name in $processNames) {
    $processRows += Get-Process -Name $name -ErrorAction SilentlyContinue |
        Select-Object Id, ProcessName, Path, StartTime
}
$processRows | Sort-Object ProcessName, Id | Format-Table -AutoSize

