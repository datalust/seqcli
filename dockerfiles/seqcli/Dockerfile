# based on: https://github.com/dotnet/dotnet-docker/blob/master/2.0/runtime-deps/jessie/amd64/Dockerfile

FROM ubuntu:18.04

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        liblmdb-dev \
        libc6 \
        libgcc1 \
        libgssapi-krb5-2 \
        libicu60 \
        liblttng-ust0 \
        libssl1.0.0 \
        libstdc++6 \
        zlib1g \
&& rm -rf /var/lib/apt/lists/*

COPY dockerfiles/seqcli/run.sh /run.sh
COPY src/SeqCli/bin/Release/net5.0/linux-x64/publish /bin/seqcli

ENTRYPOINT ["/run.sh"]

LABEL Description="seqcli" Vendor="Datalust Pty Ltd"
