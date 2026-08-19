$ErrorActionPreference = "Stop"

$releaseScript = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "Start-GXWorksMode.ps1"

if (-not (Test-Path -LiteralPath $releaseScript)) {
    throw "Missing GX Works mode script: $releaseScript"
}

& $releaseScript -ReleaseOnly

Write-Host "Remote GX Works2 mode is ready."
Write-Host "Caddy, student web, camera, Pixel Streaming, RDP, and Guacamole remain online."
Write-Host "The student can now open GX Works2 inside the Guacamole RDP session."
