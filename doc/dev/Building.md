# Biometria SDK for .Net - Building

# Projects
Every library requires a ```.csproj``` to spell out what the library does and any specific requirements.  The ```Directory.Build.props``` in the library are read before and ```Directory.Build.targets``` after the ```.csproj```.

## Properties
When MSBuild runs ```Microsoft.Common.props``` it hunts for user defined properties.  Every library delegates default property settings defined by ```Directory.Build.props``` files to others in the following order:

1. Root directory
2. Root engineering directory (suffix Commons)

That MSBuild grabs **properties** is misleading because the properties are not just settings rather even drive how profiling, packaging and artifacts are implemented.

### Root
The root ```Directory.Build.props``` has smart tricks:
- paths that point out engineering and only Biometria libraries
- default profiles e.g. Debug, any CPU and platform
- singular artifacts spot rather than per library
- packaging (central package management CPM)

#### Artifacts
Artifacts pumped to the ```artifacts``` directory ignored by Git speeds up incremental builds in a big repository:
- eliminates output collisions by putting output under a library directory then different Net frameworks (i.e. deterministic paths)
- better caching where deterministic paths ease marks unchanged input and thereby skip compilation i.e. unnecessary rebuilds

With all outputs (i.e. ```bin```) in a predictable place build scripts can reference outputs directly so tools can without re-evaluating a project:
- run tests
- NuGet packing
- run compatibility checks

Faster Git operations are possible in a large repository by not having to ignore ```bin``` and ```object``` per project instead only the ```artifacts``` directory.  Same idea means easy clean by just wiping out ```artifacts```.

Pipelines can expect just one artifact station for build, artifact collections, and publishing makes caching, uploading, and simplier jobs a bonus.

#### Packaging
After delegating to engineering the packaging (CPM) is grabbed ```eng\packaging\Directory.Packages.props``` instructs Nuget (see [documentation](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)).  The package properties segregates Nuget packages for testing, code coverage etc.

### Engineering
The engineering  ```Directory.Build.Common.props``` sets these generic categories for all libraries:
- kind e.g. client or test
- defaults e.g. lastest language version
- code analysis
- metadata e.g. company == Biometria
- support Net frameworks
- references internal SDK library dependencies 

## Build
MSBuild has a ```Microsoft.Common.targets``` that aims to import ```Directory.Build.targets``` early in the **build** process.  Most libraries don't have their own build settings rather delegation starts at the root then balls over to engineering.

### Root
Currently the root build doesn't do much than track where test assemblies originate from using reflection helping:
- CI pipelines trace
- debugging and diagnostics
- tooling that consumes metadata

### Engineering
Here build guidelines are enforced.  Any PowerShell script from MSBuild references ```scripts/Set-Environment.ps1``` constructs a PowerShell environment.  Good documentation to have handy is [PowerShell state](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_automatic_variables?view=powershell-7.5#psscriptroot) variables.

#### Package version
Every package has the following version format:
```
MAJOR.MINOR.PATCH-PRERELEASE
```
Use the ```.csproj``` to define the version in ```Version``` property.

MSBuild replaces any prerelease identifier (e.g. ```beta```) with a `alpha.yyyyMMdd.r` ensuring a unique build identification during development. That date derives from either today's date or a property named `BUILD_IDENTIFICATION` delegated from the CI pipeline's build number (see ```globals.yml```).

TODO: Auto version bumping in the ```.csproj``` and change log.

## Pipelines
Biometria uses [Azure Pipelines](https://learn.microsoft.com/azure/devops/pipelines/) for each library with minimal input per package.  Here is layout of Yaml directives:

- sdk/{library}/ci.yml
  - eng/pipelines/stages/library.yml
    - eng/pipelines/variables/globals.yml
    - eng/pipelines/jobs/ci.yml
    
      Where generic pipelines feed into repo-specific pipelines (jobs, stegs etc.)
    - eng/pipelines/stages/release.yml

