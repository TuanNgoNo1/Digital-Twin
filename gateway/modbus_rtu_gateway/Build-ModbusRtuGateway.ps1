$ErrorActionPreference = "Stop"

$source = Join-Path $PSScriptRoot "ModbusRtuGateway.cs"
$binDirectory = Join-Path $PSScriptRoot "bin"
$output = Join-Path $binDirectory "ModbusRtuGateway.exe"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw "C# compiler not found: $compiler"
}

New-Item -ItemType Directory -Path $binDirectory -Force | Out-Null
& $compiler /nologo /optimize+ /target:exe /out:$output /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll $source
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed with exit code $LASTEXITCODE"
}

Write-Host "Built $output"
