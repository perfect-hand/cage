Import-Module $PSScriptRoot/GetVersionNumber.psm1

$VersionNumber = Get-VersionNumber


Set-Location $PSScriptRoot/../source/Cage.Backend.Game

Write-Output "`nCleaning game backend..."
dotnet clean

Write-Output "`nBuilding game backend..."
dotnet build


Set-Location $PSScriptRoot/../source/Cage.Backend.Game.Tests

Write-Output "`nCleaning game backend tests..."
dotnet clean

Write-Output "`nBuilding game backend tests..."
dotnet build

Write-Output "`nRunning game backend tests..."
dotnet test


Set-Location $PSScriptRoot/../source/Cage.Backend.Game

Write-Output "`nPublishing game backend..."
dotnet publish -p:Version=$VersionNumber

Write-Output "`nBuilding game backend docker image..."
Set-Location $PSScriptRoot/../source/Cage.Backend.Game
docker build --build-arg version=$VersionNumber -t ghcr.io/perfect-hand/cage-backend-game:$VersionNumber .

Write-Output "`Pushing game backend docker image..."
$env:GITHUB_TOKEN | docker login ghcr.io -u $env:GITHUB_USER --password-stdin
docker push ghcr.io/perfect-hand/cage-backend-game:$VersionNumber

Set-Location $PSScriptRoot
