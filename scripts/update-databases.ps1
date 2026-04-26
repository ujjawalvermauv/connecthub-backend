param(
    [string]$Configuration = "Development"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

$targets = @(
    @{ Name = "Auth"; Project = "ConnectHub.Auth/ConnectHub.Auth.csproj"; Context = "AuthDbContext" },
    @{ Name = "Message"; Project = "ConnectHub.Message/ConnectHub.Message.csproj"; Context = "MessageDbContext" },
<<<<<<< HEAD
    @{ Name = "Notification"; Project = "ConnectHub.Notification/ConnectHub.Notification.csproj"; Context = "NotificationDbContext" }
=======
    @{ Name = "Notification"; Project = "ConnectHub.Notification/ConnectHub.Notification.csproj"; Context = "NotificationDbContext" },
    @{ Name = "Media"; Project = "ConnectHub.Media/ConnectHub.Media.csproj"; Context = "MediaDbContext" }
>>>>>>> media-service
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