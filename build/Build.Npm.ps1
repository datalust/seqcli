# Publishes the npm packages for a seqcli build: one `@datalust/seqcli-<rid>` package per release
# archive, then the launcher package `@datalust/seqcli` (from ./npm/seqcli) with its
# optionalDependencies pinned to the same version.
#
# In CI (see publish-npm in .github/workflows/ci.yml) the archives come from the build-windows job's
# artifacts, so dev builds are published as prereleases (dist-tag `dev`) and main builds as `latest`,
# matching NuGet. Packages that already exist on the registry at the target version are skipped, so
# a partially-failed run can simply be re-run.
#
# Usage:
#   ./build/Build.Npm.ps1 -ArchiveDir ./npm-archives                 # CI: version from Get-SemVer
#   ./build/Build.Npm.ps1 -Version 2026.1.02616                       # (re)publish GitHub release v2026.1.02616
#   ./build/Build.Npm.ps1 -Version 2026.1.02616 -ArchiveDir ./x -DryRun  # stage and `npm pack` only
param(
    # Build version as it appears in archive names, e.g. 2026.1.02616 or 2026.1.02700-dev-02700.
    # Defaults to Get-SemVer, which in CI reproduces the version computed by the build jobs.
    [string] $Version,

    # npm dist-tag; defaults to `latest` for release versions and `dev` for prereleases.
    [string] $DistTag,

    # Directory containing seqcli-<version>-<rid>.zip|.tar.gz archives; when omitted, the archives
    # are downloaded from GitHub release v<version> with `gh release download`.
    [string] $ArchiveDir,

    # GitHub repository to download release assets from. Also identifies the repository whose CI
    # publishes via npm trusted publishing (forks without an NPM_TOKEN skip publishing).
    [string] $Repo = 'datalust/seqcli',

    # Stage the packages and run `npm pack` instead of `npm publish`.
    [switch] $DryRun
)

Push-Location $PSScriptRoot/../

. ./build/Build.Common.ps1

$ErrorActionPreference = 'Stop'

$scope = '@datalust'
$launcherName = "$scope/seqcli"
$staging = './npm-staging'

if (-not $Version) {
    $Version = Get-SemVer
}

$npmVersion = Get-NpmVersion $Version

if (-not $DistTag) {
    $DistTag = @{ $true = 'dev'; $false = 'latest' }[$npmVersion.Contains('-')]
}

Write-Host "Release version: $Version"
Write-Host "npm version: $npmVersion"
Write-Host "npm dist-tag: $DistTag"
Write-Host "Dry run: $DryRun"

if (-not $DryRun -and -not $env:NODE_AUTH_TOKEN -and $env:GITHUB_REPOSITORY -ne $Repo) {
    # Forks have neither the NPM_TOKEN secret nor a trusted publisher configuration.
    Write-Host "Skipping npm publishing: no npm credentials are available in this environment"
    Pop-Location
    exit 0
}

function Get-Rids
{
    $([xml](Get-Content ./src/SeqCli/SeqCli.csproj)).Project.PropertyGroup.RuntimeIdentifiers.Split(';')
}

function Get-PlatformSpec($rid)
{
    $os = switch -Wildcard ($rid) {
        'win-*' { 'win32' }
        'osx-*' { 'darwin' }
        'linux-*' { 'linux' }
        default { throw "Unrecognized RID: $rid" }
    }

    $cpu = ($rid -split '-')[-1]

    $libc = $null
    if ($rid -like 'linux-musl-*') { $libc = 'musl' }
    elseif ($rid -like 'linux-*') { $libc = 'glibc' }

    return @{ os = $os; cpu = $cpu; libc = $libc; isWindows = ($os -eq 'win32') }
}

function Get-ReleaseArchive($rid)
{
    $pattern = "seqcli-$Version-$rid.*"

    if ($ArchiveDir) {
        $archive = Get-ChildItem -Path $ArchiveDir -Filter $pattern | Select-Object -First 1
        if (-not $archive) { throw "No archive matching $pattern in $ArchiveDir" }
        return $archive.FullName
    }

    $downloads = "$staging/download"
    New-Item -ItemType Directory -Force -Path $downloads | Out-Null

    & gh release download "v$Version" --repo $Repo --dir $downloads --pattern $pattern --clobber
    if ($LASTEXITCODE -ne 0) { throw "Downloading $pattern from release v$Version failed" }

    $archive = Get-ChildItem -Path $downloads -Filter $pattern | Select-Object -First 1
    if (-not $archive) { throw "Release v$Version has no asset matching $pattern" }
    return $archive.FullName
}

function Expand-ReleaseArchive($archive, $destination)
{
    if (Test-Path $destination) { Remove-Item -Recurse -Force $destination }
    New-Item -ItemType Directory -Force -Path $destination | Out-Null

    if ($archive -like '*.zip') {
        Expand-Archive -Path $archive -DestinationPath $destination -Force
    } else {
        & tar -xzf $archive -C $destination
        if ($LASTEXITCODE -ne 0) { throw "Extracting $archive failed" }
    }

    # The archives contain a single `seqcli-<version>-<rid>/` root folder; the package needs the
    # binary at its root, so lift the contents up one level.
    $entries = @(Get-ChildItem -Force $destination)
    if ($entries.Count -eq 1 -and $entries[0].PSIsContainer) {
        $root = $entries[0].FullName
        Get-ChildItem -Force $root | Move-Item -Destination $destination
        Remove-Item -Force $root
    }
}

function Write-PlatformPackageJson($rid, $spec, $destination)
{
    $package = Get-Content ./npm/platform-package.json -Raw | ConvertFrom-Json -AsHashtable
    $package.name = "$scope/seqcli-$rid"
    $package.version = $npmVersion
    $package.description = $package.description.Replace('{{rid}}', $rid)
    $package.os = @($spec.os)
    $package.cpu = @($spec.cpu)
    if ($spec.libc) { $package.libc = @($spec.libc) }

    $package | ConvertTo-Json -Depth 5 | Set-Content -Path "$destination/package.json" -NoNewline
}

function Test-NpmPublished($name)
{
    $output = & npm view "$name@$npmVersion" version --json 2>$null
    return ($LASTEXITCODE -eq 0) -and -not [string]::IsNullOrWhiteSpace(($output -join ''))
}

function Publish-NpmPackage($name, $directory)
{
    if ($DryRun) {
        Write-Host "Packing $name@$npmVersion"
        $tarballs = "$staging/tarballs"
        New-Item -ItemType Directory -Force -Path $tarballs | Out-Null
        & npm pack $directory --pack-destination $tarballs
        if ($LASTEXITCODE -ne 0) { throw "Packing $name failed" }
        return
    }

    if (Test-NpmPublished $name) {
        Write-Host "Skipping $name@$npmVersion; already published"
        return
    }

    Write-Host "Publishing $name@$npmVersion with dist-tag $DistTag"
    $arguments = @('publish', $directory, '--access', 'public', '--tag', $DistTag)
    if ($env:GITHUB_ACTIONS -eq 'true') { $arguments += '--provenance' }
    & npm @arguments
    if ($LASTEXITCODE -ne 0) { throw "Publishing $name failed" }
}

function Assert-NpmPublished($name)
{
    # The registry can take a moment to reflect a new version.
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        if (Test-NpmPublished $name) { return }
        Start-Sleep -Seconds 5
    }
    throw "$name@$npmVersion is not visible on the registry"
}

function Stage-PlatformPackage($rid)
{
    $spec = Get-PlatformSpec $rid
    $directory = "$staging/seqcli-$rid"

    $archive = Get-ReleaseArchive $rid
    Write-Host "Staging $scope/seqcli-$rid from $archive"
    Expand-ReleaseArchive $archive $directory

    $binary = Join-Path $directory $(if ($spec.isWindows) { 'seqcli.exe' } else { 'seqcli' })
    if (-not (Test-Path $binary)) { throw "Expected $binary in $archive" }

    if (-not $spec.isWindows) {
        & chmod +x $binary
        if ($LASTEXITCODE -ne 0) { throw "chmod failed for $binary" }
    }

    Write-PlatformPackageJson $rid $spec $directory

    return $directory
}

function Stage-LauncherPackage($rids)
{
    $directory = "$staging/seqcli"
    if (Test-Path $directory) { Remove-Item -Recurse -Force $directory }
    Copy-Item -Recurse ./npm/seqcli $directory

    $package = Get-Content "$directory/package.json" -Raw | ConvertFrom-Json -AsHashtable
    $package.version = $npmVersion
    $package.optionalDependencies = [ordered]@{}
    foreach ($rid in $rids) {
        $package.optionalDependencies["$scope/seqcli-$rid"] = $npmVersion
    }

    $package | ConvertTo-Json -Depth 5 | Set-Content -Path "$directory/package.json" -NoNewline

    return $directory
}

if (Test-Path $staging) { Remove-Item -Recurse -Force $staging }
New-Item -ItemType Directory -Force -Path $staging | Out-Null

$rids = Get-Rids

foreach ($rid in $rids) {
    $directory = Stage-PlatformPackage $rid
    Publish-NpmPackage "$scope/seqcli-$rid" $directory
}

if (-not $DryRun) {
    # Never expose a launcher whose optional dependencies can't all be resolved.
    foreach ($rid in $rids) {
        Assert-NpmPublished "$scope/seqcli-$rid"
    }
}

$launcherDirectory = Stage-LauncherPackage $rids
Publish-NpmPackage $launcherName $launcherDirectory

if (-not $DryRun) {
    Assert-NpmPublished $launcherName
    & npm view $launcherName dist-tags
    Write-Host "Install with: npm install -g $launcherName@$npmVersion"
}

Pop-Location
