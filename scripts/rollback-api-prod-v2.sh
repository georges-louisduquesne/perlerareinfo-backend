#!/usr/bin/env bash
# PRODUCTION rollback of deploy-api-prod-v2.sh. The current API (api.perle-rare.info.service)
# is never touched.
#
# Default (MODE=restore): puts back the previous v2 build from the archive the last deploy took
# (api-v2-LATEST, or BACKUP=/home/administrateur/backups/api-v2-<ts>.tgz) and restarts v2.
#   CONFIRM_PROD=deploie-en-prod bash scripts/rollback-api-prod-v2.sh
#
# MODE=unplug: removes "ProxyPass /v2/" from the api.perle-rare.info vhost and stops v2.
# The production front calls v2: unplugging it breaks the CRM unless the front is rolled back too.
#   CONFIRM_PROD=deploie-en-prod MODE=unplug bash scripts/rollback-api-prod-v2.sh
set -euo pipefail

if [[ "${CONFIRM_PROD:-}" != "deploie-en-prod" ]]; then
	echo "Refused: production rollback. Set CONFIRM_PROD=deploie-en-prod after explicit approval." >&2
	exit 1
fi
MODE="${MODE:-restore}"
case "$MODE" in restore|unplug) ;; *) echo "MODE must be restore or unplug" >&2; exit 1;; esac

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
KEY_FILE="${SSH_KEY:-$REPO_ROOT/git.key}"
HOST="${OVH_HOST:-142.4.216.57}"
PORT="${OVH_PORT:-2477}"
USER="${OVH_USER:-administrateur}"
test -f "$KEY_FILE" || { echo "SSH key not found: $KEY_FILE" >&2; exit 1; }
SSH=(ssh -i "$KEY_FILE" -p "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new "$USER@$HOST")

if [[ "$MODE" == "restore" ]]; then
	"${SSH[@]}" BACKUP="${BACKUP:-}" bash -s <<'REMOTE'
set -euo pipefail
BACKUPS=/home/administrateur/backups
V2_DIR=/var/www/api-v2.perle-rare.info
TGZ="${BACKUP:-$(cat $BACKUPS/api-v2-LATEST)}"
case "$TGZ" in $BACKUPS/api-v2-*.tgz) ;; *) echo "bad backup path: $TGZ" >&2; exit 1;; esac
test -f "$TGZ"
TMP=$(mktemp -d /home/administrateur/api-v2-restore.XXXXXX)
sudo -n tar -C "$TMP" -xzf "$TGZ"
sudo -n test -x "$TMP/api-v2.perle-rare.info/ApiPerleRare"
sudo -n rsync -a --delete --exclude 'logs/' "$TMP/api-v2.perle-rare.info/" "$V2_DIR/"
sudo -n rm -rf "$TMP"
sudo -n systemctl restart api-v2.perle-rare.info.service
for i in $(seq 1 30); do
  if ss -ltn | grep -F '127.0.0.1:5002' >/dev/null; then break; fi
  sleep 1
done
BODY=$(curl -sS -H 'Content-Type: application/json' -d '{"login":"x","password":"y"}' http://127.0.0.1:5002/api/User/authenticate)
echo "$BODY" | grep -q 'Login or password is incorrect' || { echo "unexpected: $BODY" >&2; sudo -n journalctl -u api-v2.perle-rare.info.service -n 60 --no-pager; exit 1; }
sudo -n rm -f $BACKUPS/api-v2-DEPLOYED-COMMIT
systemctl is-active --quiet api.perle-rare.info.service
echo "ROLLBACK_OK from $TGZ"
REMOTE
	curl -sS -o /dev/null -w "v2_refresh_status=%{http_code} (401 expected without token)\n" "https://api.perle-rare.info/v2/api/User/refresh"
	curl -sS -o /dev/null -w "current_api_status=%{http_code}\n" "https://api.perle-rare.info/api/Test/info"
	exit 0
fi

"${SSH[@]}" bash -s <<'REMOTE'
set -euo pipefail
BACKUPS=/home/administrateur/backups
TS=$(date +%Y%m%d%H%M%S)
sudo -n mkdir -p "$BACKUPS/apache"
sudo -n chown administrateur:administrateur "$BACKUPS/apache"
VHOSTS=$(grep -l 'ServerName api.perle-rare.info' /etc/apache2/sites-enabled/*.conf | xargs -n1 readlink -f | sort -u)
CHANGED=""
for f in $VHOSTS; do
  grep -q 'ProxyPass /v2/' "$f" || continue
  cp "$f" "$BACKUPS/apache/$(basename "$f").pre-rollback.$TS"
  python3 - "$f" <<'PY'
import pathlib, sys
vhost = pathlib.Path(sys.argv[1])
keep = [l for l in vhost.read_text().splitlines(keepends=True)
        if "127.0.0.1:5002" not in l and "API v2 (Kestrel 127.0.0.1:5002)" not in l]
pathlib.Path("/tmp/pr-api-vhost.rollback").write_text("".join(keep))
PY
  sudo -n cp /tmp/pr-api-vhost.rollback "$f"
  rm -f /tmp/pr-api-vhost.rollback
  CHANGED="$CHANGED $f"
  echo "unpatched: $f"
done
if ! sudo -n apache2ctl -t; then
  for f in $CHANGED; do sudo -n cp "$BACKUPS/apache/$(basename "$f").pre-rollback.$TS" "$f"; done
  echo "configtest failed — vhost left as before rollback" >&2
  exit 1
fi
sudo -n systemctl reload apache2
sudo -n systemctl disable --now api-v2.perle-rare.info.service || true
sudo -n rm -f $BACKUPS/api-v2-DEPLOYED-COMMIT
systemctl is-active --quiet api.perle-rare.info.service
echo ROLLBACK_OK
REMOTE

curl -sS -o /dev/null -w "current_api_status=%{http_code}\n" "https://api.perle-rare.info/api/Test/info"
curl -sS -o /dev/null -w "v2_status=%{http_code} (404/503 expected)\n" "https://api.perle-rare.info/v2/api/User/refresh"
