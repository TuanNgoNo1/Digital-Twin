$ErrorActionPreference = "Stop"

# [2026-08-12] Bundle da gom ve C:\PixelStreamingBundle (truoc o Downloads\PLC-Server-Bundle).
# Day la duong chay SAU MOI LAN REBOOT (POST-REBOOT-RUN-THIS.bat ->
# Start-FullLabAfterReboot.ps1 -> file nay), sai duong dan la reboot xong ca cum khong len.
$startAll = "C:\PixelStreamingBundle\START-ALL.bat"
$bridge = "C:\PixelStreamingBundle\Bridge\PlcBridge.exe"

if (-not (Test-Path -LiteralPath $startAll)) {
    throw "Missing PLC Pixel Streaming starter: $startAll"
}

Start-Process -FilePath $startAll -WorkingDirectory (Split-Path -Parent $startAll)

# USB serial adapters can receive a different COM number after a reboot.
Start-Sleep -Seconds 10
$bridgeListening = Get-NetTCPConnection -LocalPort 9090 -State Listen -ErrorAction SilentlyContinue
$gatewayListening = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue
if (-not $bridgeListening -and $gatewayListening) {
    Write-Host "PlcBridge remains offline because the HTTP PLC gateway currently owns the serial port."
} elseif (-not $bridgeListening -and (Test-Path -LiteralPath $bridge)) {
    # Bai 1 uses the CH340 adapter on COM3. Never let PlcBridge claim COM5 or
    # COM8 because those ports are reserved for other practical lessons.
    $usbSerial = Get-PnpDevice -Class Ports -PresentOnly -ErrorAction SilentlyContinue |
        Where-Object { $_.FriendlyName -match 'CH340' -and $_.FriendlyName -match '\(COM3\)' } |
        Select-Object -First 1

    if ($usbSerial -and $usbSerial.FriendlyName -match '\((COM\d+)\)') {
        $comPort = $Matches[1]
        Get-Process -Name PlcBridge -ErrorAction SilentlyContinue | Stop-Process -Force
        Start-Sleep -Seconds 1
        Start-Process -FilePath $bridge `
            -ArgumentList @($comPort, '9090') `
            -WorkingDirectory (Split-Path -Parent $bridge) `
            -WindowStyle Hidden
        Write-Host "Restarted PlcBridge with detected USB serial port $comPort."
    } else {
        Write-Warning "PlcBridge is not listening on 9090 and CH340/COM3 was not available. COM5 and COM8 were intentionally ignored."
    }
}

Write-Host "Requested start of Pixel Streaming stack:"
Write-Host "  TURN relay"
Write-Host "  Node signalling on 8090/8888/8889"
Write-Host "  PlcBridge on 9090"
Write-Host "  Unreal PLC.exe"
