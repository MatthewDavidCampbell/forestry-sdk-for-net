# Scripting

## Pipeline stages
Each pipeline stage has a corresponding directory under ```scripts``` with any classes under a ```{Directory}.Classes.ps1``` then functions in separate PowerShell files.

### Build

### Analyze

### Test

### Package
The big thing here is defining a correct package version and synchronizing with the change log per library.

### Publish

## PowerShell test
Pester was not shipped with PowerShell prior to version 5.1.  The normal pattern is to have a ```Tests``` directory with files suffixed with ```Tests```.  Pester automatically looks for these suffixed files when running:

```Invoke-Pester```
```Invoke-Pester -Path scripts\package\Tests\Get-Version.Tests.ps1```
```Invoke-Pester -Tag "Version"```