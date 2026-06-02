param(
    [switch]$Run
)

$ErrorActionPreference = "Continue"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = (Resolve-Path (Join-Path $scriptDirectory "..")).Path
$logsDirectory = Join-Path $projectRoot "logs"
$stdoutLogPath = Join-Path $logsDirectory "dev-build.stdout.log"
$stderrLogPath = Join-Path $logsDirectory "dev-build.stderr.log"
$buildLogPath = Join-Path $logsDirectory "dev-build.log"
$buildResultPath = Join-Path $logsDirectory "dev-build-result.txt"
$exitCodePath = Join-Path $logsDirectory "dev-build.exitcode.txt"
$childScriptPath = Join-Path $logsDirectory "dev-build-child.ps1"
$solutionPath = Join-Path $projectRoot "LifeRPG.Native.sln"
$buildCommand = "dotnet build `"$solutionPath`" -v:normal -p:GenerateFullPaths=true -p:UseSharedCompilation=false"
$checkedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$powerShellVersion = $PSVersionTable.PSVersion.ToString()

Set-Location $projectRoot
New-Item -ItemType Directory -Force -Path $logsDirectory | Out-Null
Remove-Item -Path $stdoutLogPath, $stderrLogPath, $buildLogPath, $buildResultPath, $exitCodePath, $childScriptPath -Force -ErrorAction SilentlyContinue

function Write-BuildResult {
    param(
        [int]$ExitCode,
        [string]$Source,
        [string[]]$ExtraLines = @()
    )

    $buildSucceeded = $ExitCode -eq 0
    $resultLines = @(
        "ScriptExitCodeSource: $Source",
        "PowerShellVersion: $powerShellVersion",
        "CurrentDirectory: $projectRoot",
        "SolutionPath: $solutionPath",
        "SolutionExists: $((Test-Path -Path $solutionPath -PathType Leaf).ToString().ToLowerInvariant())",
        "BuildExitCode: $ExitCode",
        "BuildSucceeded: $($buildSucceeded.ToString().ToLowerInvariant())",
        "BuildLogPath: $buildLogPath",
        "CheckedAt: $checkedAt",
        "Command: $buildCommand"
    )

    $resultLines += $ExtraLines
    $resultLines | Set-Content -Path $buildResultPath -Encoding UTF8
    Get-Content -Path $buildResultPath
}

if (-not (Test-Path -Path $solutionPath -PathType Leaf)) {
    "Solution file not found: $solutionPath" | Set-Content -Path $buildLogPath -Encoding UTF8
    Write-BuildResult -ExitCode 100 -Source "solution check" -ExtraLines @("Hint: Solution file not found.")
    exit 100
}

$buildServerOutput = & dotnet build-server shutdown 2>&1
$buildServerExitCode = $LASTEXITCODE

$childScript = @"
Set-Location '$($projectRoot.Replace("'", "''"))'
& dotnet build '$($solutionPath.Replace("'", "''"))' -v:normal -p:GenerateFullPaths=true -p:UseSharedCompilation=false > '$($stdoutLogPath.Replace("'", "''"))' 2> '$($stderrLogPath.Replace("'", "''"))'
`$exitCode = `$LASTEXITCODE
Set-Content -Path '$($exitCodePath.Replace("'", "''"))' -Value `$exitCode -Encoding UTF8
exit `$exitCode
"@
$childScript | Set-Content -Path $childScriptPath -Encoding UTF8

try {
    $process = Start-Process `
        -FilePath "powershell.exe" `
        -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$childScriptPath`"") `
        -WorkingDirectory $projectRoot `
        -WindowStyle Hidden `
        -PassThru
}
catch {
    $buildExitCode = 101
    $exceptionMessage = $_.Exception.Message
    @(
        "Command: $buildCommand",
        "Start-Process failed.",
        "Exception: $exceptionMessage"
    ) | Set-Content -Path $buildLogPath -Encoding UTF8
    Write-BuildResult -ExitCode $buildExitCode -Source "Start-Process failure" -ExtraLines @("Hint: Start-Process failed. See logs/dev-build.log for details.")
    exit $buildExitCode
}

$timeoutAt = (Get-Date).AddMinutes(5)
while (-not (Test-Path -Path $exitCodePath -PathType Leaf)) {
    if ((Get-Date) -gt $timeoutAt) {
        $buildExitCode = 102
        @(
            "Command: $buildCommand",
            "Build timed out while waiting for dotnet build exit code file.",
            "StartedProcessId: $($process.Id)"
        ) | Set-Content -Path $buildLogPath -Encoding UTF8
        Write-BuildResult -ExitCode $buildExitCode -Source "Start-Process timeout" -ExtraLines @("Hint: Timed out waiting for dotnet build to write exit code.")
        exit $buildExitCode
    }

    Start-Sleep -Milliseconds 200
}

$rawExitCode = Get-Content -Path $exitCodePath -Raw
$parsedExitCode = 0
if (-not [int]::TryParse($rawExitCode.Trim(), [ref]$parsedExitCode)) {
    $buildExitCode = 103
}
else {
    $buildExitCode = $parsedExitCode
}

$stdoutContent = Get-Content -Path $stdoutLogPath -ErrorAction SilentlyContinue
$stderrContent = Get-Content -Path $stderrLogPath -ErrorAction SilentlyContinue

@(
    "dotnet build-server shutdown exit code: $buildServerExitCode",
    "",
    "dotnet build-server shutdown output:",
    $buildServerOutput,
    "",
    "Command: $buildCommand",
    "Build exit code: $buildExitCode",
    "",
    "=== stdout ===",
    $stdoutContent,
    "",
    "=== stderr ===",
    $stderrContent
) | Set-Content -Path $buildLogPath -Encoding UTF8

$extraLines = @()
if ($buildExitCode -ne 0) {
    $extraLines += "Hint: Build failed. See logs/dev-build.log for full output."
}

Write-BuildResult -ExitCode $buildExitCode -Source "Start-Process dotnet build" -ExtraLines $extraLines

if ($Run -and $buildExitCode -eq 0) {
    dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj
}

exit $buildExitCode
