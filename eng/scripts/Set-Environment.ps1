#Requires -Version 7.0

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot .. ..)
$EngineeringDirectory = Join-Path $RepoRoot "eng"
$EngineeringScriptsDirectory = Join-Path $EngineeringDirectory "scripts"

# Modules when not duplicate i.e. inherent to PowerShell
Import-Module (Join-Path $EngineeringScriptsDirectory "package" "Package.psd1") -Force
Import-Module (Join-Path $EngineeringScriptsDirectory "logging" "Logging.psd1") -Force
