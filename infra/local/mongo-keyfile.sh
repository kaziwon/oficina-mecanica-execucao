#!/usr/bin/env bash
set -euo pipefail
umask 077
if [ ! -s /keys/mongo-keyfile ]; then
  openssl rand -base64 756 > /keys/mongo-keyfile
fi
chown mongodb:mongodb /keys/mongo-keyfile
chmod 400 /keys/mongo-keyfile
