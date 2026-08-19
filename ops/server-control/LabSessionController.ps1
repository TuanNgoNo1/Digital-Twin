param(
    [string]$ListenPrefix = "http://127.0.0.1:5010/",
    [string]$TokenFile = "C:\ProgramData\PDTwin\LabControl\controller-token.txt",
    [string]$LogFile = "C:\ProgramData\PDTwin\LabControl\controller.log"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $TokenFile)) {
    throw "Lab controller token file is missing: $TokenFile"
}

$controllerToken = (Get-Content -LiteralPath $TokenFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($controllerToken)) {
    throw "Lab controller token is empty."
}

$modeScript = Join-Path $PSScriptRoot "Invoke-LabMode.ps1"
$statusScript = Join-Path $PSScriptRoot "Get-LabModeStatus.ps1"
$watchdogScript = Join-Path $PSScriptRoot "LabSessionWatchdog.ps1"
$operationMutex = [System.Threading.Mutex]::new($false, "Global\PDTwinLabModeSwitch")

function Write-ControllerLog {
    param([string]$Message)

    $safeMessage = $Message -replace '[\r\n]+', ' '
    Add-Content -LiteralPath $LogFile -Value ("{0} {1}" -f (Get-Date).ToString("o"), $safeMessage)
}

function Send-Json {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][int]$StatusCode,
        [Parameter(Mandatory = $true)]$Body
    )

    $json = $Body | ConvertTo-Json -Depth 8 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $Context.Response.StatusCode = $StatusCode
    $Context.Response.ContentType = "application/json; charset=utf-8"
    $Context.Response.ContentLength64 = $bytes.Length
    $Context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
    $Context.Response.OutputStream.Close()
}

function Test-ControllerToken {
    param($Request)

    $supplied = $Request.Headers["X-Lab-Control-Token"]
    return (-not [string]::IsNullOrEmpty($supplied)) -and ($supplied -ceq $controllerToken)
}

$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add($ListenPrefix)

Start-Process powershell.exe `
    -ArgumentList @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", "`"$watchdogScript`"",
        "-LogFile", "`"$LogFile`""
    ) `
    -WindowStyle Hidden

$listener.Start()
Write-ControllerLog "Controller started on $ListenPrefix"

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $request = $context.Request
        $path = $request.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant()

        try {
            if ($request.HttpMethod -eq "GET" -and $path -eq "/health") {
                Send-Json $context 200 @{ status = "ok" }
                continue
            }

            if (-not (Test-ControllerToken $request)) {
                Send-Json $context 401 @{ error = "unauthorized" }
                continue
            }

            if ($request.HttpMethod -eq "GET" -and $path -eq "/api/lab/status") {
                $status = & $statusScript
                Send-Json $context 200 $status
                continue
            }

            $operation = $null
            $logoffStudent = $false
            if ($request.HttpMethod -eq "POST" -and $path -eq "/api/lab/mode/gxworks") {
                $operation = "GxWorks"
            }
            elseif ($request.HttpMethod -eq "POST" -and $path -eq "/api/lab/mode/gateway") {
                $operation = "Gateway"
            }
            elseif ($request.HttpMethod -eq "POST" -and $path -eq "/api/lab/session/end") {
                $operation = "Gateway"
                $logoffStudent = $true
            }

            if (-not $operation) {
                Send-Json $context 404 @{ error = "not_found" }
                continue
            }

            $ownsMutex = $false
            try {
                $ownsMutex = $operationMutex.WaitOne(0, $false)
            }
            catch [System.Threading.AbandonedMutexException] {
                $ownsMutex = $true
            }

            if (-not $ownsMutex) {
                Send-Json $context 409 @{ error = "mode_switch_in_progress" }
                continue
            }

            try {
                $actor = $request.Headers["X-Lab-Actor"]
                if ([string]::IsNullOrWhiteSpace($actor)) {
                    $actor = "backend"
                }
                Write-ControllerLog "actor=$actor path=$path operation=$operation"

                if ($logoffStudent) {
                    $result = & $modeScript -Mode $operation -LogoffPlcStudent
                }
                else {
                    $result = & $modeScript -Mode $operation
                }

                Send-Json $context 200 $result
            }
            finally {
                $operationMutex.ReleaseMutex()
            }
        }
        catch {
            Write-ControllerLog ("request_failed path={0} error={1}" -f $path, $_.Exception.Message)
            if ($context.Response.OutputStream.CanWrite) {
                Send-Json $context 500 @{ error = "operation_failed"; message = $_.Exception.Message }
            }
        }
    }
}
finally {
    Write-ControllerLog "Controller stopped"
    $listener.Stop()
    $listener.Close()
    $operationMutex.Dispose()
}
