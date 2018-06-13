$ErrorActionPreference = 'Stop'

$framework = 'netcoreapp2.1'

function Clean-Output
{
	if(Test-Path ./artifacts) { rm ./artifacts -Force -Recurse }
}

function Restore-Packages
{
	& dotnet restore
}

function Execute-Tests
{
    & dotnet test ./test/SeqCli.Tests/SeqCli.Tests.csproj -c Release /p:Configuration=Release /p:Platform=x64 /p:VersionPrefix=$version
    if($LASTEXITCODE -ne 0) { exit 3 }
}

function Create-ArtifactDir
{
	mkdir ./artifacts
}

function Publish-Gzips($version)
{
	$rids = @("linux-x64", "osx-x64")
	foreach ($rid in $rids) {
		& dotnet publish src/SeqCli/SeqCli.csproj -c Release -f $framework -r $rid /p:VersionPrefix=$version /p:ShowLinkerSizeComparison=true
	    if($LASTEXITCODE -ne 0) { exit 4 }
	
		# Make sure the archive contains a reasonable root filename
		mv ./src/SeqCli/bin/Release/$framework/$rid/publish/ ./src/SeqCli/bin/Release/$framework/$rid/seqcli-$version-$rid/

		& ./build/7-zip/7za.exe a -ttar seqcli-$version-$rid.tar ./src/SeqCli/bin/Release/$framework/$rid/seqcli-$version-$rid/
		if($LASTEXITCODE -ne 0) { exit 5 }

		# Back to the original directory name
		mv ./src/SeqCli/bin/Release/$framework/$rid/seqcli-$version-$rid/ ./src/SeqCli/bin/Release/$framework/$rid/publish/
		
		& ./build/7-zip/7za.exe a -tgzip ./artifacts/seqcli-$version-$rid.tar.gz seqcli-$version-$rid.tar
		if($LASTEXITCODE -ne 0) { exit 6 }

		rm seqcli-$version-$rid.tar
	}
}

function Publish-Msi($version)
{
	& dotnet publish ./src/SeqCli/SeqCli.csproj -c Release -f $framework -r win-x64 /p:VersionPrefix=$version /p:ShowLinkerSizeComparison=true
	if($LASTEXITCODE -ne 0) { exit 7 }

	& msbuild ./setup/SeqCli.Setup/SeqCli.Setup.wixproj /t:Build /p:Configuration=Release /p:Platform=x64 /p:SeqCliVersion=$version /p:BuildProjectReferences=false
	if($LASTEXITCODE -ne 0) { exit 8 }

	mv ./setup/SeqCli.Setup/bin/Release/seqcli.msi ./artifacts/seqcli-$version.msi
}

Push-Location $PSScriptRoot

$version = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "99.99.99" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];
Write-Output "Building version $version"

Clean-Output
Create-ArtifactDir
Restore-Packages
Publish-Msi($version)
Publish-Gzips($version)
Execute-Tests

Pop-Location
