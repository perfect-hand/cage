Import-Module $PSScriptRoot/GetVersionNumber.psm1


Set-Location $PSScriptRoot/../source/Cage.Simulation

Write-Output "`nCleaning simulation library..."
dotnet clean

Write-Output "`nBuilding simulation library..."
dotnet build


Set-Location $PSScriptRoot/../source/Cage.Simulation.Tests

Write-Output "`nCleaning simulation tests..."
dotnet clean

Write-Output "`nBuilding simulation tests..."
dotnet build

Write-Output "`nRunning simulation tests..."
dotnet test
