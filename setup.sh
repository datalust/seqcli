#!/bin/bash

# Exit if any command fails
set -e
set -o pipefail

sudo apt-get update || true
sudo apt-get install -y --no-install-recommends jq

RequiredDotnetVersion=$(jq -r '.sdk.version' global.json)

curl https://dot.net/v1/dotnet-install.sh -sSfL --output dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --install-dir $HOME/.dotnetcli --no-path --version $RequiredDotnetVersion
rm dotnet-install.sh

docker run --privileged --rm docker/binfmt
service docker restart
