$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

@(
    "$env:LOCALAPPDATA\Programs\DockerDesktop\resources\bin",
    "$env:LOCALAPPDATA\Programs\Docker\Docker\resources\bin",
    "$env:ProgramFiles\Docker\Docker\resources\bin"
) | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object {
    if (($env:PATH -split ';') -notcontains $_) {
        $env:PATH = "$_;$env:PATH"
    }
}

Write-Host "Starting Docker Desktop..."
$dockerDesktop = "$env:LOCALAPPDATA\Programs\DockerDesktop\Docker Desktop.exe"
if (Test-Path -LiteralPath $dockerDesktop) {
    Start-Process -FilePath $dockerDesktop
}

Write-Host "Waiting for Docker engine..."
$ready = $false
for ($i = 1; $i -le 60; $i++) {
    docker info *> $null
    if ($LASTEXITCODE -eq 0) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 5
}

if (-not $ready) {
    throw "Docker engine did not become ready within 5 minutes."
}

.\Initialize-Guacamole.ps1
.\Enable-CaddyGxWorksRoute.ps1

Write-Host ""
Write-Host "Open:"
Write-Host "  http://127.0.0.1:8081/gxworks2/"
Write-Host "  http://103.238.69.131:8080/gxworks2/"

