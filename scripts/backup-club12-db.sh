#!/usr/bin/env bash
#
# Weekly logical backup of the Club12 Postgres container.
#
# Runs `pg_dump` inside the `db` service (custom format) and keeps the newest
# RETENTION dumps under BACKUP_DIR. Intended for a host crontab entry, e.g.:
#
#   0 3 * * 0  /home/docker/backup-club12-db.sh >> /var/log/club12-db-backup.log 2>&1
#
# Restore a dump with:  pg_restore --no-owner --clean --if-exists -d <db> <file>
#
set -euo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-/home/docker-compose/Club12/docker-compose.yml}"
BACKUP_DIR="${BACKUP_DIR:-/home/docker/backups/club12}"
RETENTION="${RETENTION:-8}"
SERVICE="db"

timestamp="$(date +%Y%m%d-%H%M)"
target="${BACKUP_DIR}/club12-${timestamp}.dump"
tmp="${target}.partial"

mkdir -p "${BACKUP_DIR}"

# pg_dump reads POSTGRES_USER / POSTGRES_DB from the container's own environment
# (set via env_file in docker-compose.yml); a local socket connection as the
# superuser needs no password. `set -o pipefail` makes a pg_dump failure abort.
docker compose -f "${COMPOSE_FILE}" exec -T "${SERVICE}" \
  sh -c 'pg_dump -U "$POSTGRES_USER" --format=custom "$POSTGRES_DB"' > "${tmp}"

mv "${tmp}" "${target}"
echo "$(date -Is) backup ok: ${target} ($(du -h "${target}" | cut -f1))"

# Prune: keep the newest RETENTION dumps, delete the rest.
mapfile -t stale < <(ls -1t "${BACKUP_DIR}"/club12-*.dump 2>/dev/null | tail -n +"$((RETENTION + 1))")
if ((${#stale[@]} > 0)); then
  printf '%s\n' "${stale[@]}" | xargs -r rm -f
  echo "$(date -Is) pruned ${#stale[@]} old dump(s), kept ${RETENTION}"
fi
