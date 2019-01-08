#!/bin/bash

# Default arguments to those passed to the container
args=$@

exec /bin/seqcli/seqcli $args
