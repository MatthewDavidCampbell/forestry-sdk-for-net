# Load private functions
Get-ChildItem "$PSScriptRoot/Private/*.ps1" | ForEach-Object {
    . $_.FullName
}

# Load classes
Get-ChildItem "$PSScriptRoot/Classes/*.ps1" | ForEach-Object {
    . $_.FullName
}

# Load public functions
Get-ChildItem "$PSScriptRoot/Public/*.ps1" | ForEach-Object {
    . $_.FullName
}

# Export only public functions
$public = Get-ChildItem "$PSScriptRoot/Public/*.ps1" |
    ForEach-Object { $_.BaseName }

Export-ModuleMember -Function $public
