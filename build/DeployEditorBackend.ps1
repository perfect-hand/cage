Import-Module $PSScriptRoot/GetVersionNumber.psm1

$VersionNumber = Get-VersionNumber

Write-Output "`Deploying editor backend to Azure Container App..."

az login `
    --service-principal `
    --username 281d9078-5563-4d6d-9741-8a2275a254b4 `
    --password $env:AZURE_CLIENT_SECRET `
    --tenant 0573377a-7932-4757-bce2-25fe85980a29

az account set --subscription 2f314f3f-f0a7-4227-a8fd-d21027d83b77

az containerapp update `
    --name cageeditor-dev-ca `
    --resource-group cageeditor-dev-rg `
    --image ghcr.io/perfect-hand/cage-backend-editor:$VersionNumber
