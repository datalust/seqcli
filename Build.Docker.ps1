$IsCIBuild = $null -ne $env:APPVEYOR_BUILD_NUMBER
$IsPublishedBuild = ($env:APPVEYOR_REPO_BRANCH -eq "main" -or $env:APPVEYOR_REPO_BRANCH -eq "dev") -and $null -eq $env:APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH

$version = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "99.99.99" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];
$framework = "net6.0"
$rids = @("linux-x64", "linux-arm64")
$baseTag = "datalust/seqcli-ci:$version"

function Execute-Tests
{
    & dotnet test ./test/SeqCli.Tests/SeqCli.Tests.csproj -c Release -f $framework /p:Configuration=Release /p:VersionPrefix=$version
    if ($LASTEXITCODE -ne 0) { exit 1 }

    cd ./test/SeqCli.EndToEnd/
    docker pull datalust/seq:latest
    & dotnet run -f $framework -- --docker-server
    if ($LASTEXITCODE -ne 0)
    { 
        cd ../..
        exit 1 
    }
    cd ../..
}

function Build-DockerImage($rid)
{
    & dotnet publish src/SeqCli/SeqCli.csproj -c Release -f $framework -r $rid --self-contained /p:VersionPrefix=$version
    if($LASTEXITCODE -ne 0) { exit 2 }

    & docker build -f dockerfiles/seqcli/$rid.Dockerfile -t "$baseTag-$rid" .
    if($LASTEXITCODE -ne 0) { exit 3 }
}

function Publish-DockerImage($rid)
{
    $ErrorActionPreference = "SilentlyContinue"

    if ($IsCIBuild) {
        Write-Output "$env:DOCKER_TOKEN" | docker login -u $env:DOCKER_USER --password-stdin
        if ($LASTEXITCODE) { exit 3 }
    }

    & docker push "$baseTag-$rid"
    if($LASTEXITCODE -ne 0) { exit 3 }

    $ErrorActionPreference = "Stop"
}

Push-Location $PSScriptRoot

Execute-Tests

foreach ($rid in $rids) {
    Build-DockerImage($rid)

    if ($IsPublishedBuild) {
        Publish-DockerImage($rid)
    }
}

Pop-Location
