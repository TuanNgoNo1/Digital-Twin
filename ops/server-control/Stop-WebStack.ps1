$ErrorActionPreference = "Continue"

$ports = @(80, 8080, 5000)
$protectedProcesses = @("System", "Idle", "svchost", "services", "lsass", "wininit")

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

Write-Host "Stopping web stack listeners on ports: $($ports -join ', ')"
$listenersByPort = Get-ListeningTcpRows | Group-Object Port -AsHashTable -AsString
foreach ($port in $ports) {
    $listeners = $listenersByPort[[string]$port]
    foreach ($listener in $listeners) {
        $process = Get-Process -Id $listener.PID -ErrorAction SilentlyContinue
        if (-not $process) {
            continue
        }

        if ($protectedProcesses -contains $process.ProcessName) {
            Write-Host "Skip protected process $($process.ProcessName) PID $($process.Id) on port $port"
            continue
        }

        Write-Host "Stopping $($process.ProcessName) PID $($process.Id) on port $port"
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

Get-Process -Name ffmpeg -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Stopping ffmpeg PID $($_.Id)"
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "Web stack stop requested."
