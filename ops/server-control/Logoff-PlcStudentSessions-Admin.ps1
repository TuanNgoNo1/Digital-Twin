$ErrorActionPreference = "Stop"

$sessionLines = & query.exe user 2>&1
$sessionIds = foreach ($line in $sessionLines) {
    if ($line -match '^\s*>?\s*plc_student\s+(?:\S+\s+)?(\d+)\s+(Active|Disc)\b') {
        [int]$Matches[1]
    }
}

$sessionIds = @($sessionIds | Sort-Object -Unique)
if ($sessionIds.Count -eq 0) {
    Write-Host "No plc_student sessions were found."
    exit 0
}

foreach ($sessionId in $sessionIds) {
    Write-Host "Logging off plc_student session $sessionId..."
    & logoff.exe $sessionId
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to log off session $sessionId."
    }
}

Write-Host "All plc_student sessions have been logged off."
