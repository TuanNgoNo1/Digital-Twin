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

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is not installed or not available in PATH."
}

if (-not (Test-Path -LiteralPath ".\initdb")) {
    New-Item -ItemType Directory -Path ".\initdb" | Out-Null
}

$initSql = ".\initdb\001-initdb.sql"
if (-not (Test-Path -LiteralPath $initSql)) {
    Write-Host "Generating Guacamole PostgreSQL schema..."
    docker run --rm guacamole/guacamole:1.6.0 /opt/guacamole/bin/initdb.sh --postgresql |
        Set-Content -LiteralPath $initSql -Encoding ASCII
}

Write-Host "Pulling Guacamole containers..."
docker compose pull

Write-Host "Starting Guacamole stack..."
docker compose up -d

Write-Host ""
Write-Host "Guacamole should be available locally at:"
Write-Host "  http://127.0.0.1:8081/gxworks2/"
Write-Host ""
Write-Host "Default first login:"
Write-Host "  username: guacadmin"
Write-Host "  password: guacadmin"
Write-Host ""
Write-Host "Change the default password immediately after first login."
