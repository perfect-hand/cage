Import-Module $PSScriptRoot/GetVersionNumber.psm1

$GitVersion = git --version
$DotNetVersion = dotnet --version
$NgVersion = ng --version
$SwaVersion = swa --version
$VersionNumber = Get-VersionNumber

Write-Output "Working directory: $(Get-Location)"

Write-Output "Git version: $GitVersion"
Write-Output ".NET version: $DotNetVersion"
Write-Output "Angular CLI version: $NgVersion"
Write-Output "Azure Static Web Apps CLI version: $SwaVersion"

Write-Output "Application version: $VersionNumber"
