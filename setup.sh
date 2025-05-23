#!/bin/bash

# Exit if any command fails
set -e
set -o pipefail

sudo apt-get update || true
sudo apt-get install -y --no-install-recommends jq

RequiredDotnetVersion=$(jq -r '.sdk.version' ci.global.json)

curl https://dot.net/v1/dotnet-install.sh -sSfL --output dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --install-dir $HOME/.dotnetcli --no-path --version $RequiredDotnetVersion
rm dotnet-install.sh

docker run --privileged --rm linuxkit/binfmt:bebbae0c1100ebf7bf2ad4dfb9dfd719cf0ef132
sudo service docker restart
