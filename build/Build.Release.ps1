# Creates (or refreshes) the draft GitHub release for a seqcli build, with every file in -AssetDir
# attached. The tag isn't created until the release is published, at the commit given by -Target.
#
# In CI (see draft-release in .github/workflows/publish.yml) the assets are the artifacts of the CI
# run, and the version is reproduced by Get-SemVer from CI_BUILD_NUMBER_BASE and CI_TARGET_BRANCH.
# Builds from branches other than `main` are marked as prereleases. If a draft for the tag already
# exists (left by an earlier attempt of the same run), its assets are replaced instead.
#
# The release tag is written to $env:GITHUB_OUTPUT as `tag` when that variable is set.
#
# Usage:
#   ./build/Build.Release.ps1 -AssetDir ./release-assets -Target <sha>
param(
    # Directory containing the files to attach to the release.
    [Parameter(Mandatory)]
    [string] $AssetDir,

    # Commit the release's tag is created at when the release is published.
    [Parameter(Mandatory)]
    [string] $Target,

    # GitHub repository to create the release in.
    [string] $Repo = $env:GITHUB_REPOSITORY
)

$ErrorActionPreference = 'Stop'

if (-not $Repo) { throw "No repository given; pass -Repo or set GITHUB_REPOSITORY" }

$assets = @(Get-ChildItem $AssetDir -File | ForEach-Object { $_.FullName })
if ($assets.Count -eq 0) { throw "No release assets found in $AssetDir" }

Push-Location $PSScriptRoot/../
try {
    . ./build/Build.Common.ps1
    $tag = "v$(Get-SemVer)"
} finally {
    Pop-Location
}

if ($env:GITHUB_OUTPUT) {
    "tag=$tag" >> $env:GITHUB_OUTPUT
}

$isDraft = gh release view $tag --repo $Repo --json isDraft --jq .isDraft 2>$null
if ($LASTEXITCODE -eq 0) {
    # A draft left by an earlier attempt of this run; refresh its assets.
    if ($isDraft -ne 'true') { throw "Release $tag has already been published" }
    Write-Host "Updating existing draft release $tag"
    & gh release upload $tag --repo $Repo --clobber @assets
    if ($LASTEXITCODE -ne 0) { throw "Uploading release assets failed" }
} else {
    Write-Host "Creating draft release $tag"
    $arguments = @('release', 'create', $tag, '--draft', '--title', $tag, '--target', $Target, '--generate-notes', '--repo', $Repo)
    # Get-SemVer adds a prerelease suffix for every branch except `main`.
    if ($tag.Contains('-')) { $arguments += '--prerelease' }
    & gh @arguments @assets
    if ($LASTEXITCODE -ne 0) { throw "Creating the draft release failed" }
}
