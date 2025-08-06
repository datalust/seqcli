Push-Location $PSScriptRoot/../

. ./build/Build.Common.ps1

$ErrorActionPreference = 'Stop'

Write-Host "Run Number: $env:CI_BUILD_NUMBER_BASE"
Write-Host "Target Branch: $env:CI_TARGET_BRANCH"
Write-Host "Published: $env:CI_PUBLISH"

$version = Get-SemVer

Write-Output "Building version $version"

$framework = 'net9.0'

function Clean-Output
{
    if(Test-Path ./artifacts) { rm ./artifacts -Force -Recurse }
}

function Restore-Packages
{
    & dotnet restore
    if($LASTEXITCODE -ne 0) { throw "Build failed" }
}

function Execute-Tests($version)
{
    & dotnet test ./test/SeqCli.Tests/SeqCli.Tests.csproj -c Release --framework "$framework" /p:Configuration=Release /p:Platform=x64 /p:VersionPrefix=$version
    if($LASTEXITCODE -ne 0) { throw "Build failed" }

    choco install seq
    $env:PATH="C:\Program Files\Seq;$env:PATH"

    cd ./test/SeqCli.EndToEnd/
    & dotnet run -f $framework
    if ($LASTEXITCODE -ne 0)
    { 
        cd ../..
        exit 1 
    }
    cd ../..
}

function Create-ArtifactDir
{
    mkdir ./artifacts
}

function Publish-Archives($version)
{
    $rids = $([xml](Get-Content .\src\SeqCli\SeqCli.csproj)).Project.PropertyGroup.RuntimeIdentifiers.Split(';')
    foreach ($rid in $rids) {
        $tfm = $framework

        & dotnet publish ./src/SeqCli/SeqCli.csproj -c Release -f $tfm -r $rid --self-contained /p:VersionPrefix=$version
        if($LASTEXITCODE -ne 0) { throw "Build failed" }

        # Make sure the archive contains a reasonable root filename
        mv ./src/SeqCli/bin/Release/$tfm/$rid/publish/ ./src/SeqCli/bin/Release/$tfm/$rid/seqcli-$version-$rid/

        if ($rid.StartsWith("win-")) {
            & ./build/7-zip/7za.exe a -tzip ./artifacts/seqcli-$version-$rid.zip ./src/SeqCli/bin/Release/$tfm/$rid/seqcli-$version-$rid/
            if($LASTEXITCODE -ne 0) { throw "Build failed" }
        } else {
            & ./build/7-zip/7za.exe a -ttar seqcli-$version-$rid.tar ./src/SeqCli/bin/Release/$tfm/$rid/seqcli-$version-$rid/
            if($LASTEXITCODE -ne 0) { throw "Build failed" }

            # Back to the original directory name
            mv ./src/SeqCli/bin/Release/$tfm/$rid/seqcli-$version-$rid/ ./src/SeqCli/bin/Release/$tfm/$rid/publish/
            
            & ./build/7-zip/7za.exe a -tgzip ./artifacts/seqcli-$version-$rid.tar.gz seqcli-$version-$rid.tar
            if($LASTEXITCODE -ne 0) { throw "Build failed" }

            rm seqcli-$version-$rid.tar
        }
    }
}

function Publish-DotNetTool($version)
{
    Write-Output "Building dotnet tool"

    # Tool packages have to target a single non-platform-specific TFM; doing this here is cleaner than attempting it in the CSPROJ directly
    dotnet pack ./src/SeqCli/SeqCli.csproj -c Release --output ./artifacts /p:VersionPrefix=$version /p:TargetFrameworks=$framework
    if($LASTEXITCODE -ne 0) { throw "Build failed" }
}

function Publish-Docs($version)
{
    Write-Output "Generating markdown documentation"

    & dotnet run --project ./src/SeqCli/SeqCli.csproj -f $framework -- help --markdown > ./artifacts/seqcli-$version.md
    if($LASTEXITCODE -ne 0) { throw "Build failed" }
}

function Upload-NugetPackages
{
    # GitHub Actions will only supply this to branch builds and not PRs. We publish
    # builds from any branch this action targets (i.e. main and dev).

    Write-Output "Publishing NuGet packages"

    foreach ($nupkg in Get-ChildItem artifacts/*.nupkg) {
        & dotnet nuget push -k $env:NUGET_API_KEY -s https://api.nuget.org/v3/index.json "$nupkg"
        if($LASTEXITCODE -ne 0) { throw "Publishing failed" }
    }
}

function Upload-GitHubRelease($version)
{
    Write-Output "Creating release for version $version"

    iex "gh release create v$version --title v$version --generate-notes $(get-item ./artifacts/*)"
}

function Remove-GlobalJson
{
    if(Test-Path ./global.json) { rm ./global.json }    
}

function Create-GlobalJson
{
    # It's very important that SeqCli use the same .NET SDK version as its matching Seq version, to avoid
    # container and installer bloat. But, highly-restrictive global.json files are annoying during development. So,
    # we create a temporary global.json from ci.global.json to use during CI builds.
    Remove-GlobalJson
    cp ./ci.global.json global.json
}

$env:Path = "$pwd/.dotnetcli;$env:Path"

Clean-Output
Create-ArtifactDir
Create-GlobalJson
Restore-Packages
Publish-Archives($version)
Publish-DotNetTool($version)
Execute-Tests($version)
Publish-Docs($version)

if ("$($env:NUGET_API_KEY)" -ne "")
{
    Upload-NugetPackages
}

if ($env:CI_PUBLISH -eq "True") {
    Upload-GitHubRelease($version)
}

Remove-GlobalJson

Pop-Location
