$ErrorActionPreference = "Stop"

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this installer from an elevated PowerShell session."
}

$controllerScript = Join-Path $PSScriptRoot "LabSessionController.ps1"
if (-not (Test-Path -LiteralPath $controllerScript)) {
    throw "Controller script is missing: $controllerScript"
}

$runtimeRoot = "C:\ProgramData\PDTwin\LabControl"
$tokenFile = Join-Path $runtimeRoot "controller-token.txt"
$logFile = Join-Path $runtimeRoot "controller.log"
$taskName = "PDTwin Lab Session Controller"

New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null

if (-not (Test-Path -LiteralPath $tokenFile)) {
    $bytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    }
    finally {
        $rng.Dispose()
    }

    [Convert]::ToBase64String($bytes) |
        Set-Content -LiteralPath $tokenFile -Encoding ASCII -NoNewline
}

# SYSTEM and local administrators can manage the token. The account installing
# the controller receives read access so the local Spring backend can load it.
$acl = Get-Acl -LiteralPath $tokenFile
$acl.SetAccessRuleProtection($true, $false)
$systemSid = [Security.Principal.SecurityIdentifier]::new(
    [Security.Principal.WellKnownSidType]::LocalSystemSid,
    $null)
$administratorsSid = [Security.Principal.SecurityIdentifier]::new(
    [Security.Principal.WellKnownSidType]::BuiltinAdministratorsSid,
    $null)
$currentSid = $identity.User

foreach ($rule in @(
    [Security.AccessControl.FileSystemAccessRule]::new($systemSid, "FullControl", "Allow"),
    [Security.AccessControl.FileSystemAccessRule]::new($administratorsSid, "FullControl", "Allow"),
    [Security.AccessControl.FileSystemAccessRule]::new($currentSid, "Read", "Allow")
)) {
    $acl.AddAccessRule($rule)
}
Set-Acl -LiteralPath $tokenFile -AclObject $acl

$powerShell = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$arguments = @(
    "-NoProfile"
    "-ExecutionPolicy Bypass"
    "-File `"$controllerScript`""
    "-TokenFile `"$tokenFile`""
    "-LogFile `"$logFile`""
) -join " "

$action = New-ScheduledTaskAction -Execute $powerShell -Argument $arguments
$trigger = New-ScheduledTaskTrigger -AtStartup
$taskPrincipal = New-ScheduledTaskPrincipal `
    -UserId "SYSTEM" `
    -LogonType ServiceAccount `
    -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1)

Register-ScheduledTask `
    -TaskName $taskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $taskPrincipal `
    -Settings $settings `
    -Description "Loopback-only controller for PLC serial-port mode and RDP session lifecycle." `
    -Force | Out-Null

Start-ScheduledTask -TaskName $taskName

$deadline = (Get-Date).AddSeconds(10)
$healthy = $false
do {
    Start-Sleep -Milliseconds 400
    try {
        $response = Invoke-RestMethod -Uri "http://127.0.0.1:5010/health" -TimeoutSec 2
        $healthy = $response.status -eq "ok"
    }
    catch {
        $healthy = $false
    }
} while (-not $healthy -and (Get-Date) -lt $deadline)

if (-not $healthy) {
    throw "Lab Session Controller task was installed but did not become healthy. Check $logFile"
}

Write-Host "Installed and started: $taskName"
Write-Host "Health: http://127.0.0.1:5010/health"
Write-Host "Token file: $tokenFile"
Write-Host "Do not expose port 5010 through Caddy or copy the token into browser code."
