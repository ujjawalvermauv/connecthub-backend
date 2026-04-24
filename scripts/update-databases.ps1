param(
    [string]$Configuration = "Development"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

$targets = @(
    @{ Name = "Auth"; Project = "ConnectHub.Auth/ConnectHub.Auth.csproj"; Context = "AuthDbContext" },
    @{ Name = "Message"; Project = "ConnectHub.Message/ConnectHub.Message.csproj"; Context = "MessageDbContext" },
    @{ Name = "Notification"; Project = "ConnectHub.Notification/ConnectHub.Notification.csproj"; Context = "NotificationDbContext" }
)

Push-Location $repoRoot
try {
    foreach ($target in $targets) {
        Write-Host "Updating $($target.Name) database..."
        & dotnet ef database update -p $target.Project -s $target.Project -c $target.Context
    }
}
finally {
    Pop-Location
}