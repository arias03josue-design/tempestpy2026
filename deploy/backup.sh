#!/usr/bin/env bash
# TEMPEST Store · backup WITHOUT stopping the store.
#   data/tempest.db → consistent SQLite snapshot (VACUUM INTO, run by the app inside the container)
#   data/keys       → Data Protection keys (admin sessions stay valid after a restore)
#   uploads/        → images uploaded from the admin panel
#
# Usage (as root):   bash /opt/tempest/deploy/backup.sh
# Cron (daily 4:00): 0 4 * * * bash /opt/tempest/deploy/backup.sh >> /var/log/tempest-backup.log 2>&1
# Settings:          BACKUP_DIR=/var/backups/tempest  KEEP_DAYS=14  CONTAINER=tempest-store
set -euo pipefail
umask 077 # the archives contain the session keys: readable by root only

CONTAINER="${CONTAINER:-tempest-store}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/tempest}"
KEEP_DAYS="${KEEP_DAYS:-14}"
STAMP="$(date +%Y%m%d-%H%M%S)"
SNAPSHOT="/tmp/tempest-$STAMP.db" # inside the container
WORK="$(mktemp -d)"

cleanup() {
    rm -rf "$WORK"
    docker exec "$CONTAINER" rm -f "$SNAPSHOT" >/dev/null 2>&1 || true
}
trap cleanup EXIT

mkdir -p "$BACKUP_DIR" "$WORK/data"

# 1. Consistent snapshot of the live database, written by the app user inside the container.
docker exec "$CONTAINER" dotnet Tempest.WebUI.dll --backup "$SNAPSHOT"
docker cp "$CONTAINER:$SNAPSHOT" "$WORK/data/tempest.db"

# 2. Session keys and uploaded images.
docker cp "$CONTAINER:/app/data/keys" "$WORK/data/keys"
docker cp "$CONTAINER:/app/wwwroot/uploads" "$WORK/uploads"

# 3. One archive per run + retention.
ARCHIVE="$BACKUP_DIR/tempest-$STAMP.tar.gz"
tar -czf "$ARCHIVE" -C "$WORK" data uploads
find "$BACKUP_DIR" -maxdepth 1 -name 'tempest-*.tar.gz' -mtime "+$KEEP_DAYS" -delete

echo "$(date -Is) backup OK: $ARCHIVE ($(du -h "$ARCHIVE" | cut -f1))"
