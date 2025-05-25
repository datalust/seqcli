Push-Location $PSScriptRoot/../

. ./build/Build.Common.ps1

Write-Host "Run Number: $env:CI_BUILD_NUMBER_BASE"
Write-Host "Target Branch: $env:CI_TARGET_BRANCH"
Write-Host "Published: $env:CI_PUBLISH"

$version = Get-SemVer

Write-Output "Building version $version"

$framework = "net9.0"
$image = "datalust/seqcli"
$archs = @(
    @{ rid = "x64"; platform = "linux/amd64" },
    @{ rid = "arm64"; platform = "linux/arm64/v8" }
)

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

function Build-DockerImage($arch)
{
    $rid = "linux-$($arch.rid)"
    
    & dotnet publish src/SeqCli/SeqCli.csproj -c Release -f $framework -r $rid --self-contained /p:VersionPrefix=$version /p:PublishSingleFile=true
    if($LASTEXITCODE -ne 0) { exit 2 }

    & docker buildx build --platform "$($arch.platform)" -f dockerfiles/seqcli/$rid.Dockerfile -t "$image-ci:$version-$($arch.rid)" .
    if($LASTEXITCODE -ne 0) { exit 3 }
}

function Login-ToDocker()
{
    $ErrorActionPreference = "SilentlyContinue"

    Write-Output "$env:DOCKER_TOKEN" | docker login -u $env:DOCKER_USER --password-stdin
    if ($LASTEXITCODE) { exit 3 }

    $ErrorActionPreference = "Stop"
}

function Publish-DockerImage($arch)
{
    $ErrorActionPreference = "SilentlyContinue"

    & docker push "$image-ci:$version-$($arch.rid)"
    if($LASTEXITCODE -ne 0) { exit 3 }

    $ErrorActionPreference = "Stop"
}

function Publish-DockerManifest($archs)
{
    $images = ""
    foreach ($arch in $archs) {
        $images += "$image-ci:$version-$($arch.rid) "
    }

    # We use `invoke-expression` here so each tag is treated as a separate arg
    invoke-expression "docker manifest create $image-ci:$version $images"
    if ($LASTEXITCODE) { exit 4 }
    
    docker manifest push $image-ci:$version
    if ($LASTEXITCODE) { exit 4 }
}

Execute-Tests

foreach ($arch in $archs) {
    Build-DockerImage($arch)
}

if ("$($env:DOCKER_TOKEN)" -ne "") {
    Login-ToDocker

    foreach ($arch in $archs) {
        Publish-DockerImage($arch)
    }

    Publish-DockerManifest($archs)
}

Pop-Location
