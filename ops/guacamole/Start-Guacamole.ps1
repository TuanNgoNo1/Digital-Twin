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

docker compose up -d
docker compose ps
