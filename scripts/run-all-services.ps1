# Run all ConnectHub services concurrently in separate PowerShell windows
# Usage: From the repository root run: .\scripts\run-all-services.ps1

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")

# Directory for per-service logs
$logDir = Join-Path $scriptDir 'service-logs'
if (-not (Test-Path $logDir)) { New-Item -Path $logDir -ItemType Directory | Out-Null }

# Locate a PowerShell executable (prefer pwsh, fall back to Windows PowerShell)
$pwshCmd = $null
try { $pwshCmd = (Get-Command pwsh -ErrorAction Stop).Source } catch {}
if (-not $pwshCmd) {
    try { $pwshCmd = (Get-Command pwsh.exe -ErrorAction Stop).Source } catch {}
}
if (-not $pwshCmd) {
    try { $pwshCmd = (Get-Command powershell -ErrorAction Stop).Source } catch {}
}
if (-not $pwshCmd) {
    Write-Error "No PowerShell executable found (pwsh or powershell). Install PowerShell or run services manually."
    exit 1
}

# Find all service csproj files (exclude test projects)
$csprojFiles = Get-ChildItem -Path $repoRoot -Recurse -Filter *.csproj -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch '\.Tests\.csproj$' }

if (-not $csprojFiles -or $csprojFiles.Count -eq 0) {
    Write-Error "No .csproj files found to run."
    exit 1
}

foreach ($proj in $csprojFiles) {
    $projPath = $proj.FullName
    $serviceName = [System.IO.Path]::GetFileNameWithoutExtension($proj.Name)
    $timestamp = (Get-Date -Format 'yyyyMMdd_HHmmss')
    $logPath = Join-Path $logDir "$($serviceName)_$timestamp.log"

    Write-Host "Launching: $projPath -> log: $logPath"

    # Use PowerShell's Tee-Object so console output is written to a log file as well.
    $dotnetCmd = "dotnet run --project `"$projPath`" 2>&1 | Tee-Object -FilePath `"$logPath`""

    Start-Process -FilePath $pwshCmd -ArgumentList "-NoExit", "-Command", $dotnetCmd
    Start-Sleep -Milliseconds 250
}

# Check for web folder without csproj (common dev setup)
if ((Test-Path (Join-Path $repoRoot 'ConnectHub.Web')) -and -not (Get-ChildItem -Path (Join-Path $repoRoot 'ConnectHub.Web') -Filter *.csproj -Recurse -ErrorAction SilentlyContinue)) {
    Write-Warning "Folder ConnectHub.Web exists but no .csproj found. Launch it manually if needed."
}

Write-Host "Started launch for all found projects. Logs: $logDir" -ForegroundColor Green
