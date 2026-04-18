function Use-GitHub-Actions() {
    # https://docs.github.com/en/actions/reference/workflows-and-actions/variables
    return ($null -ne $env:GITHUB_ACTIONS)
}