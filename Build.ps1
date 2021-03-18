$ErrorActionPreference = 'Stop'

$framework = 'net5.0'
$windowsTfmSuffix = '-windows'

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

function Publish-Archives($version)
{
	$rids = @("linux-x64", "linux-musl-x64", "osx-x64", "win-x64")
	foreach ($rid in $rids) {
	    $tfm = $framework
	    if ($rid -eq "win-x64") {
	        $tfm = "$tfm$windowsTfmSuffix"
	    }
	    
		& dotnet publish ./src/SeqCli/SeqCli.csproj -c Release -f $tfm -r $rid /p:VersionPrefix=$version
		if($LASTEXITCODE -ne 0) { exit 4 }

		# Make sure the archive contains a reasonable root filename
		mv ./src/SeqCli/bin/Release/$tfm/$rid/publish/ ./src/SeqCli/bin/Release/$tfm/$rid/seqcli-$version-$rid/

		if ($rid.StartsWith("win-")) {
			& ./build/7-zip/7za.exe a -tzip ./artifacts/seqcli-$version-$rid.zip ./src/SeqCli/bin/Release/$tfm/$rid/seqcli-$version-$rid/
			if($LASTEXITCODE -ne 0) { exit 5 }
		} else {
			& ./build/7-zip/7za.exe a -ttar seqcli-$version-$rid.tar ./src/SeqCli/bin/Release/$tfm/$rid/seqcli-$version-$rid/
			if($LASTEXITCODE -ne 0) { exit 5 }

			# Back to the original directory name
			mv ./src/SeqCli/bin/Release/$tfm/$rid/seqcli-$version-$rid/ ./src/SeqCli/bin/Release/$tfm/$rid/publish/
			
			& ./build/7-zip/7za.exe a -tgzip ./artifacts/seqcli-$version-$rid.tar.gz seqcli-$version-$rid.tar
			if($LASTEXITCODE -ne 0) { exit 6 }

			rm seqcli-$version-$rid.tar
		}
	}
}

function Publish-DotNetTool($version)
{	
	# Tool packages have to target a single non-platform-specific TFM; doing this here is cleaner than attempting it in the CSPROJ directly
	dotnet pack ./src/SeqCli/SeqCli.csproj -c Release --output ./artifacts /p:VersionPrefix=$version /p:TargetFramework=$framework /p:TargetFrameworks=
}

Push-Location $PSScriptRoot

$version = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "99.99.99" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];
Write-Output "Building version $version"

Clean-Output
Create-ArtifactDir
Restore-Packages
Publish-Archives($version)
Publish-DotNetTool($version)
Execute-Tests

Pop-Location
