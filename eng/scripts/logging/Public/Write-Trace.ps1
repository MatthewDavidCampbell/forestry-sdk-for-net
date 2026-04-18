
function Write-Info {
<#
.SYNOPSIS
    Log only arguments joined by spaces
#>
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments)]
        [string[]]$Message
    )

    [Logging]::Info($Message)
}
