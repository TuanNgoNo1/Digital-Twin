$ErrorActionPreference = "Stop"

$gxWorks = Get-Process GD2 -ErrorAction SilentlyContinue
if ($gxWorks) {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show(
        "Hay Save va dong GX Works truoc, sau do chay lai PLC Gateway Mode.",
        "GX Works van dang mo",
        "OK",
        "Warning"
    ) | Out-Null
    exit 1
}

$gatewayScript = Join-Path $PSScriptRoot "Start-PlcGateway.ps1"
Start-Process powershell.exe `
    -ArgumentList @(
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        $gatewayScript
    ) `
    -WindowStyle Hidden

$deadline = (Get-Date).AddSeconds(15)
$health = $null
do {
    Start-Sleep -Milliseconds 400
    try {
        $health = Invoke-RestMethod `
            -Uri "http://127.0.0.1:5000/health" `
            -TimeoutSec 2
    }
    catch {
        $health = $null
    }
} while (-not $health -and (Get-Date) -lt $deadline)

if (-not $health) {
    throw "PLC gateway did not restart."
}
