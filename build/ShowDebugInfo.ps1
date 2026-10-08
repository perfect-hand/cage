Import-Module $PSScriptRoot/GetVersionNumber.psm1

$GitVersion = git --version
$PowerShellVersion = $PSVersionTable.PSVersion
$NodeVersion = node --version
$NpmVersion = npm --version
$DotNetVersion = dotnet --version
$NgVersion = ng --version
$SwaVersion = swa --version
$DockerVersion = docker --version
$AzureCliVersion = az version --query '"azure-cli"' --output tsv
$VersionNumber = Get-VersionNumber

Write-Output "Working directory: $(Get-Location)"

Write-Output "Git version: $GitVersion"
Write-Output "PowerShell version: $PowerShellVersion"
Write-Output "Node.js version: $NodeVersion"
Write-Output "npm version: $NpmVersion"
Write-Output ".NET version: $DotNetVersion"
Write-Output "Angular CLI version: $NgVersion"
Write-Output "Azure Static Web Apps CLI version: $SwaVersion"
Write-Output "Docker version: $DockerVersion"
Write-Output "Azure CLI version: $AzureCliVersion"

Write-Output "Application version: $VersionNumber"
