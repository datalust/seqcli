FROM ubuntu:22.04

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        libc6 \
        libgcc1 \
        libgssapi-krb5-2 \
        libicu70 \
        libssl3 \
        libstdc++6 \
        zlib1g \
    && rm -rf /var/lib/apt/lists/*

COPY src/SeqCli/bin/Release/net7.0/linux-x64/publish /bin/seqcli

ENTRYPOINT ["/bin/seqcli/seqcli"]

LABEL Description="seqcli" Vendor="Datalust Pty Ltd"
