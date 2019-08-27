param (
  [Parameter(Mandatory=$true)]
  [string] $version
)

$versionParts = $version.Split('.')

$major = $versionParts[0]
$minor = $versionParts[1]

$baseImage = "datalust/seqcli-ci:$version"
$publishImages = "datalust/seqcli:latest", "datalust/seqcli:$major", "datalust/seqcli:$major.$minor", "datalust/seqcli:$version"

$choices  = "&Yes", "&No"
$decision = $Host.UI.PromptForChoice("Publishing ($baseImage) as ($publishImages)", "Does this look right?", $choices, 1)
if ($decision -eq 0) {
    foreach ($publishImage in $publishImages) {
        Write-Host "Publishing $publishImage"

        docker tag $baseImage $publishImage
        if ($LASTEXITCODE) { exit 1 }

        docker push $publishImage
        if ($LASTEXITCODE) { exit 1 }
    }
} else {
    Write-Host "Cancelled"
}
