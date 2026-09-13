Import-Module $PSScriptRoot/GetVersionNumber.psm1

$VersionNumber = Get-VersionNumber

Write-Output "`nAdding Git tag..."

git tag $VersionNumber
git push origin $VersionNumber
