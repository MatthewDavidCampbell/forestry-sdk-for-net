function Use-Azure-Pipelines() {
    # See https://learn.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=azure-devops&tabs=yaml
    return ($null -ne $env:SYSTEM_TEAMPROJECTID)
}