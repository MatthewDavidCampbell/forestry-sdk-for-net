Import-Module "$PSScriptRoot/../Package.psd1" -Force

Describe "Get-Version" {
    It "returns a version" {
        Get-Version | Should -Match '\d+\.\d+\.\d+'
    }
}