Import-Module $PSScriptRoot/GetVersionNumber.psm1

$VersionNumber = Get-VersionNumber


Set-Location $PSScriptRoot/../source/Cage.Backend.Editor

Write-Output "`nCleaning editor backend..."
dotnet clean

Write-Output "`nBuilding editor backend..."
dotnet build


Set-Location $PSScriptRoot/../source/Cage.Backend.Editor.Tests

Write-Output "`nCleaning editor backend tests..."
dotnet clean

Write-Output "`nBuilding editor backend tests..."
dotnet build

Write-Output "`nRunning editor backend tests..."
dotnet test


Set-Location $PSScriptRoot/../source/Cage.Backend.Editor

Write-Output "`nPublishing editor backend..."
dotnet publish -p:Version=$VersionNumber

Write-Output "`nBuilding editor backend docker image..."
docker build --build-arg version=$VersionNumber -t ghcr.io/perfect-hand/cage-backend-editor:$VersionNumber .

Write-Output "`Pushing editor backend docker image..."
$env:GITHUB_TOKEN | docker login ghcr.io -u $env:GITHUB_USER --password-stdin
docker push ghcr.io/perfect-hand/cage-backend-editor:$VersionNumber

Set-Location $PSScriptRoot
