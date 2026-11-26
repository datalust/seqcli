param(
    [string] $SeqDockerTag = "latest"
)

Push-Location $PSScriptRoot/../

. ./build/Build.Common.ps1

Write-Host "Run Number: $env:CI_BUILD_NUMBER_BASE"
Write-Host "Target Branch: $env:CI_TARGET_BRANCH"
Write-Host "Published: $env:CI_PUBLISH"

$version = Get-SemVer

Write-Output "Testing version $version"

$framework = "net10.0"

function Execute-Tests
{
    & dotnet test ./test/SeqCli.Tests/SeqCli.Tests.csproj -c Release -f $framework /p:Configuration=Release /p:VersionPrefix=$version
    if ($LASTEXITCODE -ne 0) { exit 1 }

    cd ./test/SeqCli.EndToEnd/
    docker pull datalust/seq:$SeqDockerTag
    & dotnet run -f $framework -- --docker-server --docker-tag=$SeqDockerTag
    if ($LASTEXITCODE -ne 0)
    { 
        cd ../..
        exit 1 
    }
    cd ../..
}

Execute-Tests

Pop-Location
