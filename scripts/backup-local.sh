#!/bin/sh
set -eu
# Usage: sh scripts/backup-local.sh FILE [docker compose ...]
file=${1:?Informe arquivo de destino novo}
shift
[ "$#" -gt 0 ] || set -- docker compose
umask 077
if [ -e "$file" ]; then
    echo 'Destino existente; escolha outro arquivo.' >&2
    exit 1
fi
temporary=$(mktemp "${file}.tmp.XXXXXX")
trap 'rm -f "$temporary"' EXIT HUP INT TERM
"$@" exec -T postgres sh -c 'exec pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc' > "$temporary"
"$@" exec -T postgres pg_restore --list < "$temporary" > /dev/null
# Atomic publication without overwriting an existing backup.
ln "$temporary" "$file"
echo 'Backup PostgreSQL criado e catálogo validado.'
