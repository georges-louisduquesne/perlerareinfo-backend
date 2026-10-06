#!/usr/bin/env bash
# PRODUCTION rollback of deploy-api-prod-v2.sh: removes "ProxyPass /v2/" from the
# api.perle-rare.info vhost and stops api-v2.perle-rare.info.service.
# The current API (api.perle-rare.info.service) is not touched. Files in
# /var/www/api-v2.perle-rare.info are kept for diagnosis.
#   CONFIRM_PROD=deploie-en-prod bash scripts/rollback-api-prod-v2.sh
set -euo pipefail

if [[ "${CONFIRM_PROD:-}" != "deploie-en-prod" ]]; then
	echo "Refused: production rollback. Set CONFIRM_PROD=deploie-en-prod after explicit approval." >&2
	exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
KEY_FILE="${SSH_KEY:-$REPO_ROOT/git.key}"
HOST="${OVH_HOST:-142.4.216.57}"
PORT="${OVH_PORT:-2477}"
USER="${OVH_USER:-administrateur}"
test -f "$KEY_FILE" || { echo "SSH key not found: $KEY_FILE" >&2; exit 1; }

ssh -i "$KEY_FILE" -p "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new "$USER@$HOST" bash -s <<'REMOTE'
set -euo pipefail
BACKUPS=/home/administrateur/backups
TS=$(date +%Y%m%d%H%M%S)
mkdir -p "$BACKUPS/apache"
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
systemctl is-active --quiet api.perle-rare.info.service
echo ROLLBACK_OK
REMOTE

curl -sS -o /dev/null -w "current_api_status=%{http_code}\n" "https://api.perle-rare.info/api/Test/info"
curl -sS -o /dev/null -w "v2_status=%{http_code} (404/503 expected)\n" "https://api.perle-rare.info/v2/api/User/refresh"
