#!/bin/sh
set -eu

BASE_DIR='/mnt/sdb/routerlogs'
LIVE_FILE="$BASE_DIR/live/system.log"
ARCHIVE_DIR="$BASE_DIR/archive"
DATE_STAMP="$(date +%Y-%m-%d_%H-%M-%S)"
OUTPUT_FILE="$ARCHIVE_DIR/system-$DATE_STAMP.log"

mkdir -p "$ARCHIVE_DIR"

logread > "$OUTPUT_FILE"
chmod 664 "$OUTPUT_FILE"

logread -c >/dev/null 2>&1 || true

if [ -f "$LIVE_FILE" ]; then
    : > "$LIVE_FILE"
fi

find "$ARCHIVE_DIR" -type f -mtime +30 -exec rm -f {} \;
