$ErrorActionPreference = "Stop"

$RequiredDotnetVersion =  $(cat ./global.json | convertfrom-json).sdk.version

New-Item -ItemType Directory -Force "./build/" | Out-Null

Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile "./build/installcli.ps1"
& ./build/installcli.ps1 -InstallDir "$pwd/.dotnetcli" -NoPath -Version $RequiredDotnetVersion
if ($LASTEXITCODE) { exit 1 }

$env:Path = "$pwd/.dotnetcli;$env:Path"


Invoke-WebRequest "https://datalust.co/download/begin?version=2020.3.4761" -outfile "./build/Seq.msi"
Start-Process -Wait -FilePath msiexec -ArgumentList '/quiet /i ./build/Seq.msi WIXUI_EXITDIALOGOPTIONALCHECKBOX=0 INSTALLFOLDER="C:\Program Files\Seq"'
if ($LASTEXITCODE) { exit 1 }
