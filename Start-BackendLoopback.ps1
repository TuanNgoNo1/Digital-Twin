$ErrorActionPreference = "Stop"

$workingDirectory = "C:\Users\Server-Lab602\PTnew"
$jar = Join-Path $workingDirectory "pdtwin-backend-0.0.1-SNAPSHOT.jar"
$java = "C:\Program Files\Eclipse Adoptium\jdk-17.0.19.10-hotspot\bin\java.exe"

if (-not (Test-Path -LiteralPath $java)) {
    $java = "java"
}

if (-not (Test-Path -LiteralPath $jar)) {
    throw "Backend JAR not found: $jar"
}

$listener = Get-NetTCPConnection -State Listen -LocalPort 8080 -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalAddress -in @("127.0.0.1", "::1") }
if ($listener) {
    exit 0
}

$arguments = @(
    "-jar"
    $jar
    "--spring.profiles.active=h2"
    "--app.upload.dir=uploads/"
    "--app.unity.dir=unity-builds"
    "--server.address=127.0.0.1"
    "--server.port=8080"
)

Start-Process `
    -FilePath $java `
    -ArgumentList $arguments `
    -WorkingDirectory $workingDirectory `
    -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $PSScriptRoot "backend.out.log") `
    -RedirectStandardError (Join-Path $PSScriptRoot "backend.err.log")
