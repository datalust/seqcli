function Get-SemVer()
{
    $branch = @{ $true = $env:CI_TARGET_BRANCH; $false = $(git symbolic-ref --short -q HEAD) }[$NULL -ne $env:CI_TARGET_BRANCH];
    $revision = @{ $true = "{0:00000}" -f $([convert]::ToInt32($env:CI_BUILD_NUMBER_BASE, 10) + 2300); $false = "local" }[$NULL -ne $env:CI_BUILD_NUMBER_BASE]
    $suffix = @{ $true = ""; $false = "$($branch.Substring(0, [math]::Min(10,$branch.Length)) -replace '([^a-zA-Z0-9\-]*)', '')-$revision"}[$branch -eq "main" -and $revision -ne "local"]

    $base = $(Get-Content ./baseversion).Trim()

    if ($suffix) {
        $base + "." + $revision + "-" + $suffix
    } else {
        $base + "." + $revision
    }
}
