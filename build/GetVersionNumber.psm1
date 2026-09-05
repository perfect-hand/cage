$MajorVersion = 1

function Get-VersionNumber {
    $MinorVersion = git rev-list --count HEAD
    "$MajorVersion.$MinorVersion"
}
