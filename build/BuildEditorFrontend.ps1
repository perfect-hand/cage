Import-Module $PSScriptRoot/GetVersionNumber.psm1

$VersionNumber = Get-VersionNumber

Set-Location $PSScriptRoot/../source/cage-editor


Write-Output "`Writing configuration files..."

"export const version = '$VersionNumber';" | Out-File -FilePath .\src\environments\version.production.ts


Write-Output "`Installing editor frontend packages..."

npm ci


Write-Output "`Building editor frontend..."

ng build


Set-Location $PSScriptRoot
