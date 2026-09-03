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

Show-DebugInfo

$VersionNumber = Get-VersionNumber
Write-Output "Application version: $VersionNumber"

Build-Simulation
Set-Location $PSScriptRoot
