function Get-SemVer($shortver)
{
    # This script originally (c) 2016 Serilog Contributors - license Apache 2.0
    $branch = @{ $true = $env:APPVEYOR_REPO_BRANCH; $false = $(git symbolic-ref --short -q HEAD) }[$env:APPVEYOR_REPO_BRANCH -ne $NULL];
    $suffix = @{ $true = ""; $false = ($branch.Substring(0, [math]::Min(10,$branch.Length)) -replace '[\/\+]','-').Trim("-")}[$branch -eq "main"]

    if ($suffix) {
        $shortver + "-" + $suffix
    } else {
        $shortver
    }
}
