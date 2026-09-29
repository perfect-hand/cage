Set-Location $PSScriptRoot/../source/cage-editor

Write-Output "`Building editor frontend..."

ng build

Write-Output "`Deploying editor frontend to Azure Static Web App..."

swa deploy cageeditor-dev-stapp `
    --env production `
    --deployment-token $env:CAGE_EDITOR_SWA_DEPLOYMENT_TOKEN

Set-Location $PSScriptRoot
