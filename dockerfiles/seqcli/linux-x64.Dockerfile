# based on: https://github.com/dotnet/dotnet-docker/blob/master/2.0/runtime-deps/jessie/amd64/Dockerfile

FROM --platform=linux/amd64 ubuntu:20.04

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        libc6 \
        libgcc1 \
        libgssapi-krb5-2 \
        libicu66 \
        liblttng-ust0 \
        libssl1.1 \
        libstdc++6 \
        zlib1g \
&& rm -rf /var/lib/apt/lists/*

COPY src/SeqCli/bin/Release/net6.0/linux-x64/publish /bin/seqcli

ENTRYPOINT ["/bin/seqcli/seqcli"]

LABEL Description="seqcli" Vendor="Datalust Pty Ltd"
