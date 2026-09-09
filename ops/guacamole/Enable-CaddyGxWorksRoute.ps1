$ErrorActionPreference = "Stop"

$liveCaddyFile = "D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Caddyfile"
$bundleCaddyFile = "C:\PixelStreamingBundle\deploy\Caddyfile"
$caddyFiles = @($liveCaddyFile, $bundleCaddyFile) | Select-Object -Unique

foreach ($caddyFile in $caddyFiles) {
    if (-not (Test-Path -LiteralPath $caddyFile)) {
        Write-Host "Skip missing Caddyfile: $caddyFile"
        continue
    }

    $backupFile = "$caddyFile.bak-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    $content = Get-Content -LiteralPath $caddyFile -Raw

    if ($content -match "handle\s+/gxworks2") {
        Write-Host "Caddy GX Works2 route already exists in: $caddyFile"
        continue
    }

    Copy-Item -LiteralPath $caddyFile -Destination $backupFile
    Write-Host "Backup created: $backupFile"

    $route = @'

	handle /gxworks2 {
		redir * /gxworks2/ 308
	}

	handle /gxworks2/* {
		reverse_proxy 127.0.0.1:8081
	}

'@

    $pixelMarker = '(?m)^(\s*)# Pixel Streaming frontend assets are exposed below this path\.$'
    if ($content -match $pixelMarker) {
        $content = [regex]::Replace($content, $pixelMarker, "$route`$0", 1)
    } else {
        $fallbackMarker = '(?m)^\s*handle \{$'
        if ($content -notmatch $fallbackMarker) {
            throw "Could not find a safe insertion point in Caddyfile."
        }
        $content = [regex]::Replace($content, $fallbackMarker, "$route`$0", 1)
    }

    Set-Content -LiteralPath $caddyFile -Value $content -Encoding UTF8
    Write-Host "Inserted /gxworks2 route into: $caddyFile"
}

$caddy = Get-Command caddy -ErrorAction SilentlyContinue
if (-not $caddy) {
    throw "caddy command not found in PATH. Route was written, but config was not reloaded."
}

Write-Host "Validating Caddyfile..."
& $caddy.Source validate --config $liveCaddyFile
if ($LASTEXITCODE -ne 0) {
    throw "Caddy validation failed. Check the backup next to: $liveCaddyFile"
}

Write-Host "Reloading Caddy..."
& $caddy.Source reload --config $liveCaddyFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "Caddy reload failed, likely because admin API is disabled. Restarting Caddy process..."
    Get-Process -Name caddy -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2

    $startCaddy = "D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Start-Caddy.ps1"
    if (-not (Test-Path -LiteralPath $startCaddy)) {
        throw "Caddy route was written, but Start-Caddy.ps1 was not found: $startCaddy"
    }

    Start-Process powershell.exe `
        -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $startCaddy) `
        -WindowStyle Hidden

    Write-Host "Caddy restart requested."
}
