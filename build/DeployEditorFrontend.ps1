Import-Module $PSScriptRoot/GetVersionNumber.psm1

$VersionNumber = Get-VersionNumber

Set-Location $PSScriptRoot/../source/cage-editor


Write-Output "`Writing configuration files..."

"export const version = '$VersionNumber';" | Out-File -FilePath .\src\environments\version.production.ts


Write-Output "`Installing editor frontend packages..."

npm ci


Write-Output "`Building editor frontend..."

ng build


Write-Output "`Deploying editor frontend to Azure Static Web App..."

swa deploy cageeditor-dev-stapp `
    --env production `
    --deployment-token $env:CAGE_EDITOR_SWA_DEPLOYMENT_TOKEN


Set-Location $PSScriptRoot
