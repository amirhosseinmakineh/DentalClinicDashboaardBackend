# Local NuGet package build (non-destructive)

This script packs Framework into a local folder and builds the backend with the
opt-in PackageReference. Existing source and ProjectReferences are not removed.

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$feed = Join-Path $root 'artifacts/framework'

dotnet restore (Join-Path $root 'DentalDashboard.slnx')
if ($LASTEXITCODE -ne 0) { throw 'Solution restore failed' }

dotnet build (Join-Path $root 'DentalDashboard.slnx') --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Solution build failed' }

dotnet pack (Join-Path $root 'DentalDashboard.Framwork/DentalDashboard.Framwork.csproj') --configuration Release --no-build --output $feed
if ($LASTEXITCODE -ne 0) { throw 'Framework pack failed' }

dotnet restore (Join-Path $root 'DentalDashboard/DentalDashboard.csproj') --property:UsePrivateFrameworkPackage=true --source $feed --source 'https://api.nuget.org/v3/index.json'
if ($LASTEXITCODE -ne 0) { throw 'Package-mode restore failed' }

dotnet build (Join-Path $root 'DentalDashboard/DentalDashboard.csproj') --configuration Release --no-restore --property:UsePrivateFrameworkPackage=true
if ($LASTEXITCODE -ne 0) { throw 'Package-mode build failed' }

Write-Host 'Package-mode backend build passed. No project files or source were removed.'
