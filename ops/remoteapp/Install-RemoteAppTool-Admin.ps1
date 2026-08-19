$ErrorActionPreference = "Stop"

$msi = Join-Path $PSScriptRoot "RemoteApp.Tool.6100.msi"
$log = Join-Path $PSScriptRoot "install-admin.log"
$result = Join-Path $PSScriptRoot "install-result.txt"

if (-not (Test-Path -LiteralPath $msi)) {
    throw "RemoteApp Tool MSI not found: $msi"
}

$process = Start-Process msiexec.exe `
    -ArgumentList @('/i', "`"$msi`"", '/qn', '/norestart', '/L*v', "`"$log`"") `
    -Wait `
    -PassThru

"ExitCode=$($process.ExitCode)" | Set-Content -LiteralPath $result -Encoding ASCII

if ($process.ExitCode -notin @(0, 3010)) {
    throw "RemoteApp Tool installation failed with exit code $($process.ExitCode)."
}
