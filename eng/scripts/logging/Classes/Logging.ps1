class Logging {
    static [void] Info([string[]]$Message) {
        # Join the array into a single string
        $text = $Message -join ' '

        # Output the message
        Write-Host "[INFO] $text"
    }

    static [void] Warning([string[]]$Message) {
        if (Use-Azure-Pipelines) {
            Write-Host ("##vso[task.LogIssue type=warning;]$Message" -replace "`n", "%0D%0A")
        }
        elseif (Use-GitHub-Actions) {
            Write-Host ("::warning::$Message" -replace "`n", "%0D%0A")
        }
        else {
            Write-Host "$Message" -ForegroundColor Yellow
        }
    }
}
