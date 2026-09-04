$MajorVersion = 1
function Show-DebugInfo {
    $GitVersion = git --version
    
    Write-Output "Git version: $GitVersion"
    Write-Output "Working directory: $(Get-Location)"
}

function Get-VersionNumber {
    $MinorVersion = git rev-list --count HEAD
    "$MajorVersion.$MinorVersion"
}

function Build-Simulation {
    Set-Location $PSScriptRoot/../source/Cage.Simulation

    Write-Output "`nCleaning simulation library..."
    dotnet clean

    Write-Output "`nBuilding simulation library..."
    dotnet build
}

function Test-Simulation {
    Set-Location $PSScriptRoot/../source/Cage.Simulation.Tests

    Write-Output "`nCleaning simulation tests..."
    dotnet clean

    Write-Output "`nBuilding simulation tests..."
    dotnet build

    Write-Output "`nRunning simulation tests..."
    dotnet test
}

Show-DebugInfo

$VersionNumber = Get-VersionNumber
Write-Output "Application version: $VersionNumber"

Build-Simulation
Test-Simulation

Set-Location $PSScriptRoot
