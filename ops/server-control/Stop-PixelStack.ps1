$ErrorActionPreference = "Continue"

$names = @("PLC", "PlcBridge", "node", "turnserver")

foreach ($name in $names) {
    Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Stopping $($_.ProcessName) PID $($_.Id)"
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Pixel Streaming stack stop requested."

