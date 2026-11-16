#!/usr/bin/env bash
# TEMPEST Store · restore a backup made by backup.sh. The store is stopped for a few seconds.
#
# Usage (as root): bash /opt/tempest/deploy/restore.sh /var/backups/tempest/tempest-YYYYMMDD-HHMMSS.tar.gz
set -euo pipefail

ARCHIVE="${1:?Usage: restore.sh /var/backups/tempest/tempest-YYYYMMDD-HHMMSS.tar.gz}"
CONTAINER="${CONTAINER:-tempest-store}"
APP_UID=1654 # non-root user of the official .NET images
WORK="$(mktemp -d)"

# Whatever happens, start the store again and remove the temporary copy.
trap 'rm -rf "$WORK"; docker start "$CONTAINER" >/dev/null' EXIT

tar -xzf "$ARCHIVE" -C "$WORK"
if [ ! -f "$WORK/data/tempest.db" ]; then
    echo "Not a TEMPEST backup: $ARCHIVE" >&2
    exit 1
fi

docker stop "$CONTAINER" >/dev/null

# Same volumes as the store (--volumes-from), files owned by the app user.
# Stale -wal/-shm files are removed so SQLite opens exactly the restored snapshot.
docker run --rm --volumes-from "$CONTAINER" -v "$WORK:/restore:ro" -e APP_UID="$APP_UID" alpine:3.20 sh -euc '
    rm -f /app/data/tempest.db /app/data/tempest.db-wal /app/data/tempest.db-shm
    cp /restore/data/tempest.db /app/data/tempest.db
    rm -rf /app/data/keys
    if [ -d /restore/data/keys ]; then cp -r /restore/data/keys /app/data/keys; fi
    find /app/wwwroot/uploads -mindepth 1 -delete
    if [ -d /restore/uploads ]; then cp -r /restore/uploads/. /app/wwwroot/uploads/; fi
    chown -R "$APP_UID:$APP_UID" /app/data /app/wwwroot/uploads
'

echo "Restored $ARCHIVE; starting $CONTAINER"
