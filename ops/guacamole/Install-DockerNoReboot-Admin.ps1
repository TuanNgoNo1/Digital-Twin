$ErrorActionPreference = "Continue"

$logPath = Join-Path $PSScriptRoot "install-docker-no-reboot.log"
Start-Transcript -Path $logPath -Append | Out-Null

function Write-Step {
    param([string] $Message)
    Write-Host ""
    Write-Host "==> $Message"
}

try {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    $isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

    Write-Step "Install context"
    Write-Host "User: $($identity.Name)"
    Write-Host "Is admin: $isAdmin"
    Write-Host "Log: $logPath"

    if (-not $isAdmin) {
        throw "This script must be run as Administrator."
    }

    Write-Step "Enable WSL optional feature without reboot"
    dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
    Write-Host "DISM exit code: $LASTEXITCODE"

    Write-Step "Enable Virtual Machine Platform without reboot"
    dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
    Write-Host "DISM exit code: $LASTEXITCODE"

    Write-Step "Try to update/install WSL package"
    wsl.exe --update --web-download
    Write-Host "WSL update exit code: $LASTEXITCODE"

    Write-Step "Install Docker Desktop with winget"
    winget install --id Docker.DockerDesktop --exact --source winget --accept-source-agreements --accept-package-agreements --scope user --silent
    Write-Host "winget exit code: $LASTEXITCODE"

    Write-Step "Find Docker CLI"
    $dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
    if ($dockerCmd) {
        Write-Host "Docker CLI: $($dockerCmd.Source)"
        docker --version
    } else {
        Write-Host "Docker CLI is not available in this shell yet. It may appear after reopening PowerShell or signing in again."
    }

    Write-Step "Try to start Docker Desktop without reboot"
    $dockerDesktopCandidates = @(
        "$env:LOCALAPPDATA\Programs\Docker\Docker\Docker Desktop.exe",
        "$env:LOCALAPPDATA\Programs\DockerDesktop\Docker Desktop.exe",
        "$env:ProgramFiles\Docker\Docker\Docker Desktop.exe"
    )

    $dockerDesktop = $dockerDesktopCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if ($dockerDesktop) {
        Write-Host "Starting: $dockerDesktop"
        Start-Process -FilePath $dockerDesktop
    } else {
        Write-Host "Docker Desktop executable was not found in expected locations."
    }

    Write-Step "Final WSL status"
    wsl.exe --status
    Write-Host "WSL status exit code: $LASTEXITCODE"

    Write-Step "Done"
    Write-Host "If Windows reports that a restart is required, do not restart yet. Tell Codex and continue from the log."
}
catch {
    Write-Error $_
}
finally {
    Stop-Transcript | Out-Null
}

