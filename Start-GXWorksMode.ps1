param(
    [switch]$ReleaseOnly
)

$ErrorActionPreference = "Stop"

$gatewayProcesses = Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -eq "python.exe" -and
        $_.CommandLine -match "PiGatewayFxplc.+gateway\.py"
    }

foreach ($process in $gatewayProcesses) {
    Stop-Process -Id $process.ProcessId -ErrorAction SilentlyContinue
}

Get-Process -Name PlcBridge -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

$deadline = (Get-Date).AddSeconds(10)
do {
    Start-Sleep -Milliseconds 250
    $listener = Get-NetTCPConnection `
        -State Listen `
        -LocalPort 5000 `
        -ErrorAction SilentlyContinue
} while ($listener -and (Get-Date) -lt $deadline)

if ($listener) {
    throw "PLC gateway did not release COM3/port 5000."
}

if ($ReleaseOnly) {
    Write-Host "COM3 released for the remote GX Works2 session."
    exit 0
}

$gxWorks = "C:\Program Files (x86)\MELSOFT\GPPW2\GD2.EXE"
$project = "C:\Users\Server-Lab602\Desktop\server-to-plc.gxw"

if (-not (Test-Path -LiteralPath $gxWorks)) {
    throw "GX Works executable not found: $gxWorks"
}

if (-not (Get-Process GD2 -ErrorAction SilentlyContinue)) {
    $arguments = @()
    if (Test-Path -LiteralPath $project) {
        $arguments += $project
    }

    Start-Process `
        -FilePath $gxWorks `
        -ArgumentList $arguments
}
