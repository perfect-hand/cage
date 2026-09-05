Import-Module ./GetVersionNumber.psm1

$GitVersion = git --version
$DotNetVersion = dotnet --version
$VersionNumber = Get-VersionNumber

Write-Output "Working directory: $(Get-Location)"

Write-Output "Git version: $GitVersion"
Write-Output ".NET version: $DotNetVersion"

Write-Output "Application version: $VersionNumber"
