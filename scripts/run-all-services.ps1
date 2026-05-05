# Run all ConnectHub services concurrently in separate PowerShell windows
# Usage: From the repository root run: .\scripts\run-all-services.ps1

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")

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
    Write-Host "Launching: $projPath"
    $dotnetCmd = "dotnet run --project `"$projPath`""
    Start-Process -FilePath $pwshCmd -ArgumentList "-NoExit", "-Command", $dotnetCmd
    Start-Sleep -Milliseconds 250
}

# Check for web folder without csproj (common dev setup)
if ((Test-Path (Join-Path $repoRoot 'ConnectHub.Web')) -and -not (Get-ChildItem -Path (Join-Path $repoRoot 'ConnectHub.Web') -Filter *.csproj -Recurse -ErrorAction SilentlyContinue)) {
    Write-Warning "Folder ConnectHub.Web exists but no .csproj found. Launch it manually if needed."
}

Write-Host "Started launch for all found projects." -ForegroundColor Green
