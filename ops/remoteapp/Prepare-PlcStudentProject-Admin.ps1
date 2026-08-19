param(
    [string]$TemplateProject = "C:\Users\Server-Lab602\Desktop\server-to-plc.gxw",
    [string]$StudentWorkspace = "C:\PLC",
    [string]$TemplateStore = "C:\ProgramData\PDTwin\PLC-Templates",
    [string]$StudentUser = "plc_student",
    [string]$OperatorUser = "$env:USERDOMAIN\$env:USERNAME"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $TemplateProject -PathType Leaf)) {
    throw "GX Works2 template project not found: $TemplateProject"
}

$projectName = "Bai2.gxw"
$templateCopy = Join-Path $TemplateStore $projectName
$studentCopy = Join-Path $StudentWorkspace $projectName

New-Item -ItemType Directory -Path $TemplateStore, $StudentWorkspace -Force | Out-Null
Copy-Item -LiteralPath $TemplateProject -Destination $templateCopy -Force
Copy-Item -LiteralPath $TemplateProject -Destination $studentCopy -Force

# Keep the clean template admin-only. The short C:\PLC path is the student workspace.
# Remove inherited broad write rules only from these two lab-owned directories.
foreach ($directory in @($TemplateStore, $StudentWorkspace)) {
    & icacls.exe $directory /inheritance:r | Out-Null
    & icacls.exe $directory /grant:r `
        "*S-1-5-18:(OI)(CI)F" `
        "*S-1-5-32-544:(OI)(CI)F" `
        "${OperatorUser}:(OI)(CI)F" | Out-Null
}

& icacls.exe $StudentWorkspace /grant:r "${StudentUser}:(OI)(CI)M" | Out-Null

if ($LASTEXITCODE -ne 0) {
    throw "Could not grant project-folder permissions to $StudentUser."
}

Write-Host "GX Works2 student project is ready:"
Write-Host "  $studentCopy"
Write-Host ""
Write-Host "In GX Works2, choose Project > Open and open C:\PLC\Bai2.gxw."
