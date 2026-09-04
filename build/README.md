# Building and publishing `seqcli`

This directory holds the PowerShell scripts that build, test, package, and publish `seqcli`. They are driven by the GitHub Actions workflow in [`.github/workflows/ci.yml`](../.github/workflows/ci.yml), but can also be run locally with `pwsh`.

| Script | Runs on | Produces |
|---|---|---|
| `Build.Common.ps1` | (dot-sourced by the others) | Version number helpers |
| `Build.Windows.ps1` | Windows | Release archives for every platform, the `seqcli` .NET tool package, generated docs, a GitHub release, NuGet publish |
| `Build.Linux.ps1` | Linux | `datalust/seqcli` Docker images for `linux/amd64` and `linux/arm64` |
| `Build.Npm.ps1` | Linux (any OS locally) | `@datalust/seqcli` and `@datalust/seqcli-<rid>` npm packages |

`7-zip/` contains a vendored copy of `7za.exe`, used by `Build.Windows.ps1` to produce `.zip` and `.tar.gz` archives with consistent contents on Windows.

## Versioning

Every artifact from one CI run shares a single version, computed by `Get-SemVer` in `Build.Common.ps1`:

```
<baseversion>.<build number>[-<branch>-<build number>]
```

* `<baseversion>` is read from [`baseversion`](../baseversion) in the repository root (e.g. `2026.1`). Bump it there when starting a new release line.
* `<build number>` is `CI_BUILD_NUMBER_BASE + 2300`, zero-padded to five digits (e.g. `02616`). `CI_BUILD_NUMBER_BASE` is the GitHub Actions `run_number`; the fixed offset keeps build numbers increasing across the move from the previous CI system. Note the comment at the top of `ci.yml`: renaming the workflow file resets `run_number`, which would produce lower version numbers than already-published releases. Locally, where the variable is unset, the build number is the literal string `local`.
* The prerelease suffix is added for every branch except `main`. It is the first ten characters of the branch name, stripped of anything other than letters, digits and hyphens, followed by the build number. A `dev` build is therefore `2026.1.02700-dev-02700`; a `main` build is `2026.1.02616`.

`CI_TARGET_BRANCH` overrides the branch detected from git. In CI it is set from `github.head_ref` (for pull requests) or `github.ref_name`.

### npm versions

npm enforces strict semver, which forbids leading zeros in numeric components, so `Get-NpmVersion` strips the padding from the patch component when publishing to npm: release `v2026.1.02616` becomes `2026.1.2616` on npm, and `2026.1.02700-dev-02700` becomes `2026.1.2700-dev-02700` (prerelease identifiers are left as-is). Archive names and GitHub release tags always use the padded form.

## The CI workflow

The workflow runs on pushes and pull requests to `dev` and `main`, and can be triggered manually with `workflow_dispatch`. Three jobs run:

1. **Build (Windows)** runs `Build.Windows.ps1`. On non-PR builds, it uploads the release archives as a workflow artifact named `archives` for the npm job to consume.
2. **Build (Linux)** runs `Build.Linux.ps1`, after configuring `binfmt` so that ARM64 images can be built on the x64 runner. It runs in parallel with the Windows job.
3. **Publish (npm)** runs `Build.Npm.ps1` after the Windows job succeeds, on every non-PR build.

Environment variables control what gets published:

| Variable | Set in CI to | Effect |
|---|---|---|
| `CI_BUILD_NUMBER_BASE` | `github.run_number` | Build number component of the version |
| `CI_TARGET_BRANCH` | `github.head_ref` or `github.ref_name` | Branch component of the version |
| `CI_PUBLISH` | `True` for pushes to `main`, or when the manual `publish` input is set | Whether `Build.Windows.ps1` creates a GitHub release |
| `NUGET_API_KEY` | `secrets.NUGET_API_KEY` | When non-empty, `Build.Windows.ps1` pushes the tool package to NuGet |
| `GH_TOKEN` | `secrets.GITHUB_TOKEN` | Used by `gh release create` |
| `DOCKER_USER`, `DOCKER_TOKEN` | Docker Hub secrets | When `DOCKER_TOKEN` is non-empty, `Build.Linux.ps1` pushes images |
| `NODE_AUTH_TOKEN` | `secrets.NPM_TOKEN` | Authenticates `npm publish` (see below) |
| `SEQ_DOCKER_TAG` | e.g. `2026.1` | Seq image tag used for the Linux end-to-end tests |

GitHub only supplies repository secrets to branch builds, not to pull requests, so PR builds run the full build and test steps but publish nothing.

### What each branch publishes

| Trigger | GitHub release | NuGet | Docker Hub | npm |
|---|---|---|---|---|
| Pull request | no | no | no | no |
| Push to `dev` | no (unless manual `publish`) | prerelease version | `datalust/seqcli-ci:<version>` | prerelease, dist-tag `dev` |
| Push to `main` | yes, `v<version>` | release version | `datalust/seqcli-ci:<version>` | release, dist-tag `latest` |

Note that `Build.Linux.ps1` always pushes to the `datalust/seqcli-ci` repository (the image name with a `-ci` suffix), never directly to `datalust/seqcli`. Promoting an image to the public `datalust/seqcli` repository, and tagging it `latest`, is a separate step outside this repository.

To publish a release from `dev` (for example a preview build), run the workflow manually from the Actions tab with the **Publish a GitHub release** input checked. The release is marked as a prerelease because the branch is not `main`.

## Running the builds locally

All scripts need PowerShell 7 (`pwsh`) and the .NET 10 SDK, and must be run from anywhere inside the repository (they `Push-Location` to the root themselves). Without the `CI_*` variables the version is `<baseversion>.local`, and nothing is published because the credential variables are unset.

* `Build.Windows.ps1` requires Windows: it uses `7za.exe`, installs Seq with Chocolatey for the end-to-end tests, and builds `win-*` RIDs. Run it as `./build/Build.Windows.ps1`.
* `Build.Linux.ps1` requires Docker with `buildx`. Building the `arm64` image on an x64 host needs `binfmt` configured, as the workflow does. Run it as `./build/Build.Linux.ps1 -SeqDockerTag 2026.1`.
* `Build.Npm.ps1` runs anywhere with `npm`, `tar`, and `gh`. To exercise it end to end without publishing, point it at a directory of archives (for example a local `artifacts/` from a Windows build, or assets downloaded from a release) and use `-DryRun`:

  ```shell
  gh release download v2026.1.02616 --dir ./npm-archives --pattern 'seqcli-*-*.*'
  ./build/Build.Npm.ps1 -Version 2026.1.02616 -ArchiveDir ./npm-archives -DryRun
  ls npm-staging/tarballs
  ```

`artifacts/`, `npm-staging/` and `npm-archives/` are all ignored by git.

## Checklist for a new release line

1. Update [`baseversion`](../baseversion).
2. Update the SDK version in [`ci.global.json`](../ci.global.json) to match the Seq release.
3. Update `SEQ_DOCKER_TAG` in [`ci.yml`](../.github/workflows/ci.yml) to the Seq image the end-to-end tests should run against.
4. If the target framework changes, update `$framework` in `Build.Windows.ps1` and `Build.Linux.ps1`, and the `COPY` paths in the Dockerfiles.
