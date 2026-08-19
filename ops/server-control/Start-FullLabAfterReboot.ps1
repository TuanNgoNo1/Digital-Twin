$ErrorActionPreference = "Stop"

$serverControl = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\server-control"
$guacamole = "D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\guacamole"
$autoPlcToolsDirectory = "C:\Users\Server-Lab602\Desktop\AutoPLCTools\publish_PRVHost"
$autoPlcToolsExe = Join-Path $autoPlcToolsDirectory "AutoPLCTools.exe"

Write-Host "== 1/5 Start current web/student stack =="
& (Join-Path $serverControl "Start-WebStack.ps1")

Write-Host ""
Write-Host "Waiting 8 seconds for Caddy/backend/gateway..."
Start-Sleep -Seconds 8

Write-Host ""
Write-Host "== 2/5 Start current Pixel Streaming stack =="
& (Join-Path $serverControl "Start-PixelStack.ps1")

Write-Host ""
Write-Host "Waiting 10 seconds for Pixel Streaming..."
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "== 3/5 Start Bai 2 Guacamole stack =="
& (Join-Path $guacamole "Post-Reboot-Start.ps1")

Write-Host ""
Write-Host "Waiting 5 seconds for routes..."
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "== 4/5 Start AutoPLCTools PRVHost =="
if (-not (Test-Path -LiteralPath $autoPlcToolsExe)) {
    throw "Missing AutoPLCTools executable: $autoPlcToolsExe"
}

if (Get-Process -Name "AutoPLCTools" -ErrorAction SilentlyContinue) {
    Write-Host "AutoPLCTools is already running; skip duplicate start."
}
else {
    Start-Process `
        -FilePath $autoPlcToolsExe `
        -WorkingDirectory $autoPlcToolsDirectory
    Write-Host "AutoPLCTools started from: $autoPlcToolsDirectory"
}

Write-Host ""
Write-Host "== 5/5 Status =="
& (Join-Path $serverControl "Status-ServerStack.ps1")

Write-Host ""
Write-Host "Main URLs:"
Write-Host "  Current student server: http://103.238.69.131:8080/"
Write-Host "  Bai 3 Pixel Streaming: open the existing Bai3 flow in the student system"
Write-Host "  Bai 2 GX Works2 remote: http://103.238.69.131:8080/gxworks2/"
