param(
    [string]$LogFile = "C:\ProgramData\PDTwin\LabControl\controller.log"
)

$ErrorActionPreference = "Stop"

$modeScript = Join-Path $PSScriptRoot "Invoke-LabMode.ps1"
$watchdogMutex = [System.Threading.Mutex]::new($false, "Global\PDTwinLabSessionWatchdog")
$ownsWatchdog = $false

function Write-WatchdogLog {
    param([string]$Message)

    $safeMessage = $Message -replace '[\r\n]+', ' '
    Add-Content -LiteralPath $LogFile -Value ("{0} watchdog {1}" -f (Get-Date).ToString("o"), $safeMessage)
}

function Get-PlcStudentSessionIds {
    $ids = @()
    foreach ($line in (& query.exe user 2>&1)) {
        if ($line -match '^\s*>?\s*plc_student\s+(?:\S+\s+)?(\d+)\s+(Active|Disc)\b') {
            $ids += [int]$Matches[1]
        }
    }
    @($ids | Sort-Object -Unique)
}

try {
    try {
        $ownsWatchdog = $watchdogMutex.WaitOne(0, $false)
    }
    catch [System.Threading.AbandonedMutexException] {
        $ownsWatchdog = $true
    }

    if (-not $ownsWatchdog) {
        exit 0
    }

    $hadStudentSession = $false
    Write-WatchdogLog "started"

    while ($true) {
        try {
            $sessionIds = @(Get-PlcStudentSessionIds)
            if ($sessionIds.Count -gt 0) {
                $hadStudentSession = $true
            }
            elseif ($hadStudentSession) {
                $modeMutex = [System.Threading.Mutex]::new($false, "Global\PDTwinLabModeSwitch")
                $ownsMode = $false
                try {
                    try {
                        $ownsMode = $modeMutex.WaitOne(0, $false)
                    }
                    catch [System.Threading.AbandonedMutexException] {
                        $ownsMode = $true
                    }

                    if ($ownsMode) {
                        $gxDeadline = (Get-Date).AddSeconds(10)
                        do {
                            $gxWorks = @(Get-Process -Name GD2 -ErrorAction SilentlyContinue)
                            if ($gxWorks.Count -eq 0) {
                                break
                            }
                            Start-Sleep -Milliseconds 500
                        } while ((Get-Date) -lt $gxDeadline)

                        if ($gxWorks.Count -gt 0) {
                            throw "GX Works2 remains after plc_student logoff."
                        }

                        Write-WatchdogLog "plc_student logged off; returning to Gateway mode"
                        $null = & $modeScript -Mode Gateway
                        $hadStudentSession = $false
                        Write-WatchdogLog "Gateway mode restored"
                    }
                }
                finally {
                    if ($ownsMode) {
                        $modeMutex.ReleaseMutex()
                    }
                    $modeMutex.Dispose()
                }
            }
        }
        catch {
            Write-WatchdogLog ("retry_failed error={0}" -f $_.Exception.Message)
        }

        Start-Sleep -Seconds 2
    }
}
finally {
    if ($ownsWatchdog) {
        Write-WatchdogLog "stopped"
        $watchdogMutex.ReleaseMutex()
    }
    $watchdogMutex.Dispose()
}
