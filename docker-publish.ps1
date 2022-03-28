param (
  [Parameter(Mandatory=$True)]
  [string] $version
)

$ErrorActionPreference = "Stop"

$versionParts = $version.Split('.')

$major = $versionParts[0]
$minor = $versionParts[1]
$isPre = $version.endswith("pre")

$image = "datalust/seqcli"
$archs = @("x64", "arm64")
$publishImages = @()

foreach ($arch in $archs) {
    $ciImage = "$image-ci:$version-$arch"
    $publishImage = "$($image):$version-$arch";

    docker pull $ciImage
    if ($LASTEXITCODE) { exit 1 }

    docker tag $ciImage $publishImage
    if ($LASTEXITCODE) { exit 1 }

    docker push $publishImage    
    if ($LASTEXITCODE) { exit 1 }
    
    $publishImages += $publishImage
}

$publishManifest = "$($image):$version"

$pushTags = @($publishManifest)

if ($isPre -eq $True) {
    $pushTags += "$($image):preview"
} else {
    $pushTags += "$($image):$major", "$($image):$major.$minor", "$($image):latest"
}

$choices  = "&Yes", "&No"
$decision = $Host.UI.PromptForChoice("Publishing ($publishManifest) as ($pushTags)", "Does this look right?", $choices, 1)
if ($decision -eq 0) {
    foreach ($pushTag in $pushTags) {
        Write-Host "Publishing $pushTag"

        echo "creating manifest $pushTag from $publishImages"
        iex "docker manifest create $pushTag $publishImages"
        if ($LASTEXITCODE) { exit 1 }

        docker manifest push $pushTag
        if ($LASTEXITCODE) { exit 1 }
    }
} else {
    Write-Host "Cancelled"
}
