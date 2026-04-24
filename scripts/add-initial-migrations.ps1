param(
    [string]$Configuration = "Development"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

$targets = @(
    @{ Name = "Auth"; Project = "ConnectHub.Auth/ConnectHub.Auth.csproj"; Context = "AuthDbContext"; Migration = "InitialAuthDb"; MigrationFolder = "ConnectHub.Auth/Migrations" },
    @{ Name = "Message"; Project = "ConnectHub.Message/ConnectHub.Message.csproj"; Context = "MessageDbContext"; Migration = "InitialMessageDb"; MigrationFolder = "ConnectHub.Message/Migrations" },
    @{ Name = "Notification"; Project = "ConnectHub.Notification/ConnectHub.Notification.csproj"; Context = "NotificationDbContext"; Migration = "InitialNotificationDb"; MigrationFolder = "ConnectHub.Notification/Migrations" }
)

Push-Location $repoRoot
try {
    foreach ($target in $targets) {
        $existingMigration = Get-ChildItem -Path $target.MigrationFolder -Filter "*${($target.Migration)}*.cs" -ErrorAction SilentlyContinue

        if ($existingMigration) {
            Write-Host "$($target.Name): migration $($target.Migration) already exists, skipping."
            continue
        }

        & dotnet ef migrations add $target.Migration -p $target.Project -s $target.Project -c $target.Context -o Migrations
    }
}
finally {
    Pop-Location
}