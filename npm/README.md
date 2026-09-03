## How the package works

`@datalust/seqcli` is a small launcher. The self-contained `seqcli` binary for your platform is installed alongside it as an optional dependency from one of these packages:

| Package | Platform |
|---|---|
| `@datalust/seqcli-win-x64` | Windows x64 |
| `@datalust/seqcli-win-arm64` | Windows ARM64 |
| `@datalust/seqcli-osx-x64` | macOS x64 |
| `@datalust/seqcli-osx-arm64` | macOS ARM64 (Apple Silicon) |
| `@datalust/seqcli-linux-x64` | Linux x64 (glibc) |
| `@datalust/seqcli-linux-arm64` | Linux ARM64 (glibc) |
| `@datalust/seqcli-linux-musl-x64` | Linux x64 (musl, e.g. Alpine) |
| `@datalust/seqcli-linux-musl-arm64` | Linux ARM64 (musl, e.g. Alpine) |

The binaries are byte-for-byte the ones attached to the matching [GitHub release](https://github.com/datalust/seqcli/releases). Because the .NET runtime is bundled, no `dotnet` installation is needed. Each platform package is roughly 45 MB to download and 120 MB on disk.

Do not install with `--omit=optional` (or `--no-optional`): the platform package would be skipped and `seqcli` would fail to start with a message explaining how to fix it.

## Alpine Linux

The musl builds need the ICU globalization libraries, which minimal Alpine images don't include: either `apk add icu-libs`, or set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` to run without them.

## Versions

npm versions are the GitHub release versions with leading zeros removed from the last component, because npm requires strict semantic versioning. For example, release `v2026.1.02616` is published to npm as `2026.1.2616`. Prerelease builds are published under the `dev` dist-tag.

## Notes for Windows users

The Seq installer for Windows also installs `seqcli.exe`, into `C:\Program Files\Seq`. If that directory is on your `PATH` ahead of npm's global bin directory (`%APPDATA%\npm`), running `seqcli` will use the copy bundled with Seq rather than the one installed by npm. Run `where seqcli` to see which copies are found and in what order; `npx @datalust/seqcli <command>` always runs the npm-installed version. Both copies share the same `SeqCli.json` configuration.

Do not use the npm package to host the `seqcli forwarder` Windows service. The service registers the path of the executable that installed it, and npm replaces the installed files on every upgrade. Use the Seq installer or a release archive from GitHub instead.

