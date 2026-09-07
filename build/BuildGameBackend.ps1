Import-Module $PSScriptRoot/GetVersionNumber.psm1


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

Write-Output "`nBuilding docker image..."
Set-Location $PSScriptRoot/../source/Cage.Backend.Game
$VersionNumber = Get-VersionNumber
docker build --build-arg version=$VersionNumber -t perfect-hand/cage-backend-game:$VersionNumber .

Set-Location $PSScriptRoot
