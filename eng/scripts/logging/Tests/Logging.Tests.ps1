Describe "Logging.Info" {
    BeforeAll {
        . "$PSScriptRoot/../Classes/Logging.ps1"
    }

    It "writes info with arguments" {
        Mock Write-Host -Scope It

        [Logging]::Info(@("Hello", "World"))

        Assert-MockCalled Write-Host -Times 1 -ParameterFilter {
            $Object -eq "[INFO] Hello World"
        }
    }
}

Describe "Logging.Warning" {
    BeforeAll {
        . "$PSScriptRoot/../Classes/Logging.ps1"
        . "$PSScriptRoot/../Private/Use-Azure-Pipelines.ps1"
        . "$PSScriptRoot/../Private/Use-GitHub-Actions.ps1"
    }

    It "writes Azure Pipelines warning" {
        Mock Use-Azure-Pipelines { $true } -Scope It
        Mock Use-GitHub-Actions { $false } -Scope It
        Mock Write-Host -Scope It

        [Logging]::Warning(@("Hello", "World"))

        Assert-MockCalled Write-Host -Times 1 -ParameterFilter {
            $Object -eq "##vso[task.LogIssue type=warning;]Hello World"
        }
    }

    It "writes GitHub Actions warning" {
        Mock Use-Azure-Pipelines { $false } -Scope It
        Mock Use-GitHub-Actions { $true } -Scope It
        Mock Write-Host -Scope It

        [Logging]::Warning(@("Hello", "World"))

        Assert-MockCalled Write-Host -Times 1 -ParameterFilter {
            $Object -eq "::warning::Hello World"
        }
    }

    It "writes local console warning" {
        Mock Use-Azure-Pipelines { $false } -Scope It
        Mock Use-GitHub-Actions { $false } -Scope It
        Mock Write-Host -Scope It

        [Logging]::Warning(@("Hello", "World"))

        Assert-MockCalled Write-Host -Times 1 -ParameterFilter {
            $Object -eq "Hello World" -and
            $ForegroundColor -eq "Yellow"
        }
    }

}
