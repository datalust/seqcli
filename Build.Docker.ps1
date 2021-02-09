$IsCIBuild = $null -ne $env:APPVEYOR_BUILD_NUMBER
$IsPublishedBuild = ($env:APPVEYOR_REPO_BRANCH -eq "main" -or $env:APPVEYOR_REPO_BRANCH -eq "dev") -and $null -eq $env:APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH

$version = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "99.99.99" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];
$framework = "net5.0"
$rid = "linux-x64"
$tag = "datalust/seqcli-ci:$version"

function Execute-Tests
{
    & dotnet test ./test/SeqCli.Tests/SeqCli.Tests.csproj -c Release /p:Configuration=Release /p:Platform=x64 /p:VersionPrefix=$version
    if ($LASTEXITCODE -ne 0) { exit 1 }

    cd ./test/SeqCli.EndToEnd/
    docker pull datalust/seq:latest
    & dotnet run -- --docker-server
    if ($LASTEXITCODE -ne 0) { exit 1 }
    cd ../..
}

function Build-DockerImage
{
    & dotnet publish src/SeqCli/SeqCli.csproj -c Release -f $framework -r $rid /p:VersionPrefix=$version /p:SeqCliRid=$rid /p:ShowLinkerSizeComparison=true
    if($LASTEXITCODE -ne 0) { exit 2 }

    & docker build -f dockerfiles/seqcli/Dockerfile -t $tag .
    if($LASTEXITCODE -ne 0) { exit 3 }
}

function Publish-DockerImage
{
    $ErrorActionPreference = "SilentlyContinue"

    if ($IsCIBuild) {
        Write-Output "$env:DOCKER_TOKEN" | docker login -u $env:DOCKER_USER --password-stdin
        if ($LASTEXITCODE) { exit 3 }
    }

    & docker push $tag
    if($LASTEXITCODE -ne 0) { exit 3 }

    $ErrorActionPreference = "Stop"
}

Push-Location $PSScriptRoot

Execute-Tests

Build-DockerImage

if ($IsPublishedBuild) {
    Publish-DockerImage
}

Pop-Location
