# Stop all ConnectHub service processes
# Usage: From repo root run: .\scripts\stop-all-services.ps1

$pattern = 'ConnectHub.*'
$procs = Get-Process | Where-Object { $_.Name -like $pattern }

if (-not $procs) {
    Write-Host "No ConnectHub processes found." -ForegroundColor Yellow
    return
}

Write-Host "Found the following ConnectHub processes:" -ForegroundColor Cyan
$procs | Format-Table Id, Name, CPU, @{Name='StartTime';Expression={try{$_.StartTime}catch{'N/A'}}}

$procs | ForEach-Object {
    try {
        Write-Host "Stopping PID $($_.Id) - $($_.Name)" -ForegroundColor Green
        Stop-Process -Id $_.Id -Force -ErrorAction Stop
    }
    catch {
        Write-Warning "Failed to stop PID $($_.Id): $($_.Exception.Message)"
    }
}

Write-Host "Stop attempts complete." -ForegroundColor Green
