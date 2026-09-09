$ErrorActionPreference = "Stop"

$gxWorks = "C:\Program Files (x86)\MELSOFT\GPPW2\GD2.EXE"
$allowList = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList"
$applications = Join-Path $allowList "Applications"
$remoteAppRoot = "C:\ProgramData\PDTwin\RemoteApp"
$logoffSource = Join-Path $PSScriptRoot "EndPlcSession.cs"
$logoffApp = Join-Path $remoteAppRoot "EndPlcSession.exe"

if (-not (Test-Path -LiteralPath $gxWorks)) {
    throw "GX Works2 executable not found: $gxWorks"
}

if (-not (Test-Path -LiteralPath $logoffSource)) {
    throw "RemoteApp logoff source not found: $logoffSource"
}

New-Item -ItemType Directory -Path $remoteAppRoot -Force | Out-Null

if (Test-Path -LiteralPath $logoffApp) {
    Remove-Item -LiteralPath $logoffApp -Force
}

Add-Type `
    -Path $logoffSource `
    -ReferencedAssemblies @("System.Windows.Forms.dll", "System.Drawing.dll") `
    -OutputAssembly $logoffApp `
    -OutputType WindowsApplication

New-Item -Path $applications -Force | Out-Null
New-ItemProperty -Path $allowList -Name "fDisabledAllowList" -Value 0 -PropertyType DWord -Force | Out-Null

function Register-RemoteApp {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Alias,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $appKey = Join-Path $applications $Alias
    New-Item -Path $appKey -Force | Out-Null

    New-ItemProperty -Path $appKey -Name "Name" -Value $Name -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $appKey -Name "Path" -Value $Path -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $appKey -Name "VPath" -Value $Path -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $appKey -Name "RequiredCommandLine" -Value "" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $appKey -Name "CommandLineSetting" -Value 0 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $appKey -Name "IconPath" -Value $Path -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $appKey -Name "IconIndex" -Value 0 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $appKey -Name "ShowInTSWA" -Value 0 -PropertyType DWord -Force | Out-Null
}

Register-RemoteApp -Alias "GXWorks2" -Name "GX Works2" -Path $gxWorks
Register-RemoteApp -Alias "PLCLogoff" -Name "Ket thuc phien PLC" -Path $logoffApp

Write-Host "Registered RemoteApp alias: GXWorks2"
Write-Host "Application: $gxWorks"
Write-Host "Registered RemoteApp alias: PLCLogoff"
Write-Host "Application: $logoffApp"
