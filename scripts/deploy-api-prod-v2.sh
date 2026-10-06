#!/usr/bin/env bash
# PRODUCTION — deploys the new API as a SECOND instance next to the current one.
#   https://api.perle-rare.info/v2/  ->  127.0.0.1:5002  ·  /var/www/api-v2.perle-rare.info
# The current API (api.perle-rare.info.service, 127.0.0.1:5000, /var/www/api.perle-rare.info)
# is never written, restarted or reconfigured: Aspose invoices, the mobile app and the
# scheduled imports keep running on it. Only an extra "ProxyPass /v2/" is added to its vhost.
#
# Run only after an explicit "déploie en prod":
#   CONFIRM_PROD=deploie-en-prod bash scripts/deploy-api-prod-v2.sh
# Rollback: CONFIRM_PROD=deploie-en-prod bash scripts/rollback-api-prod-v2.sh
set -euo pipefail

if [[ "${CONFIRM_PROD:-}" != "deploie-en-prod" ]]; then
	echo "Refused: production deploy. Set CONFIRM_PROD=deploie-en-prod after explicit approval." >&2
	exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
KEY_FILE="${SSH_KEY:-$REPO_ROOT/git.key}"
HOST="${OVH_HOST:-142.4.216.57}"
PORT="${OVH_PORT:-2477}"
USER="${OVH_USER:-administrateur}"
PROD_DIR="/var/www/api.perle-rare.info"
PROD_DLL="$PROD_DIR/ApiPerleRare.dll"
PROD_UNIT="api.perle-rare.info.service"
V2_DIR="/var/www/api-v2.perle-rare.info"
V2_UNIT="api-v2.perle-rare.info.service"
V2_PORT=5002
STAGING="/home/administrateur/api-v2-staging"
BACKUPS="/home/administrateur/backups"
PUBLISH_DIR="$REPO_ROOT/artifacts/api-v2-publish"
UNIT_SRC="$REPO_ROOT/infra/ovh/systemd/$V2_UNIT"
SNIPPET="$REPO_ROOT/infra/ovh/apache/api-v2.snippet.conf"
PUBLIC_V2="https://api.perle-rare.info/v2"

SSH=(ssh -i "$KEY_FILE" -p "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new "$USER@$HOST")
SCP=(scp -i "$KEY_FILE" -P "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new)
RSYNC_SSH_WRAP="/tmp/pr-rsync-ssh.sh"
cat > "$RSYNC_SSH_WRAP" <<WRAP
#!/usr/bin/env bash
exec ssh -i "$KEY_FILE" -p "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new "\$@"
WRAP
chmod +x "$RSYNC_SSH_WRAP"

test -f "$KEY_FILE" || { echo "SSH key not found: $KEY_FILE" >&2; exit 1; }
test -f "$UNIT_SRC"
test -f "$SNIPPET"
if [[ -n "$(git -C "$REPO_ROOT" status --porcelain -- api-perle-rare-decompiled)" ]]; then
	echo "Refused: uncommitted changes in api-perle-rare-decompiled (deploy what is on main)." >&2
	exit 1
fi

echo "==> Preflight (disk, memory, sudo, current API fingerprint, port $V2_PORT)"
PROD_SHA="$("${SSH[@]}" "sha256sum $PROD_DLL | awk '{print \$1}'")"
PROD_PID="$("${SSH[@]}" "systemctl show -p MainPID --value $PROD_UNIT")"
"${SSH[@]}" bash -s <<REMOTE
set -euo pipefail
sudo -n true
df -h / /home
test "\$(df -k / | awk 'NR==2 {print \$4}')" -gt 1000000
test "\$(df -k /home | awk 'NR==2 {print \$4}')" -gt 1000000
MEM_AVAIL_MB=\$(awk '/MemAvailable/ {print int(\$2/1024)}' /proc/meminfo)
echo "MemAvailable=\${MEM_AVAIL_MB}M"
test "\$MEM_AVAIL_MB" -gt 700
test -f $PROD_DLL
test -f $PROD_DIR/appsettings.json
test -f $PROD_DIR/appsettings.Production.json
systemctl is-active --quiet $PROD_UNIT
ss -ltn > /tmp/pr-listen-pre.txt
grep -F '127.0.0.1:5000' /tmp/pr-listen-pre.txt >/dev/null
if grep -F '127.0.0.1:$V2_PORT' /tmp/pr-listen-pre.txt >/dev/null && ! systemctl is-active --quiet $V2_UNIT; then
  echo "Port $V2_PORT already used by something else" >&2
  exit 1
fi
echo PREFLIGHT_OK
REMOTE
echo "Current API DLL sha256=$PROD_SHA pid=$PROD_PID"

echo "==> Publish self-contained linux-x64 (VPS has no .NET 8 runtime)"
export PATH="${HOME}/.dotnet:${PATH}"
export DOTNET_ROOT="${HOME}/.dotnet"
rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"
dotnet publish "$REPO_ROOT/api-perle-rare-decompiled/ApiPerleRare.csproj" \
	-c Release -r linux-x64 --self-contained true \
	-p:PublishTrimmed=false \
	-o "$PUBLISH_DIR"
find "$PUBLISH_DIR" -name 'appsettings*.json' -delete
chmod +x "$PUBLISH_DIR/ApiPerleRare"
PUB_MB=$(du -sm "$PUBLISH_DIR" | awk '{print $1}')
echo "Publish size: ${PUB_MB}M"
test "$PUB_MB" -lt 250

echo "==> Upload to staging $STAGING"
"${SSH[@]}" "mkdir -p $STAGING"
rsync -az --delete -e "$RSYNC_SSH_WRAP" "$PUBLISH_DIR/" "$USER@$HOST:$STAGING/"
"${SCP[@]}" "$UNIT_SRC" "$USER@$HOST:$STAGING.unit"
"${SCP[@]}" "$SNIPPET" "$USER@$HOST:$STAGING.snippet"

echo "==> Install $V2_DIR (settings copied server-side from the current API, same JWT secret)"
"${SSH[@]}" bash -s <<REMOTE
set -euo pipefail
TS=\$(date +%Y%m%d%H%M%S)
mkdir -p $BACKUPS
if [ -d $V2_DIR ]; then
  sudo -n tar -C /var/www -czf $BACKUPS/api-v2-\$TS.tgz --exclude=logs api-v2.perle-rare.info
  sudo -n chown administrateur:administrateur $BACKUPS/api-v2-\$TS.tgz
fi
sudo -n mkdir -p $V2_DIR
sudo -n rsync -a --delete --exclude 'appsettings*.json' --exclude 'logs/' $STAGING/ $V2_DIR/
sudo -n cp $PROD_DIR/appsettings.json $V2_DIR/appsettings.json
sudo -n cp $PROD_DIR/appsettings.Production.json $V2_DIR/appsettings.Production.json
sudo -n chown -R www-data:www-data $V2_DIR
sudo -n chmod 640 $V2_DIR/appsettings.json $V2_DIR/appsettings.Production.json
sudo -n test -x $V2_DIR/ApiPerleRare
echo FILES_OK
REMOTE

echo "==> Start $V2_UNIT and check it locally before exposing it"
"${SSH[@]}" bash -s <<REMOTE
set -euo pipefail
sudo -n cp $STAGING.unit /etc/systemd/system/$V2_UNIT
sudo -n systemctl daemon-reload
sudo -n systemctl enable $V2_UNIT
sudo -n systemctl restart $V2_UNIT
for i in \$(seq 1 30); do
  if ss -ltn | grep -F '127.0.0.1:$V2_PORT' >/dev/null; then break; fi
  sleep 1
done
ss -ltn | grep -F '127.0.0.1:$V2_PORT' >/dev/null || { sudo -n journalctl -u $V2_UNIT -n 60 --no-pager; exit 1; }
BODY=\$(curl -sS -H 'Content-Type: application/json' -d '{"login":"x","password":"y"}' http://127.0.0.1:$V2_PORT/api/User/authenticate)
echo "\$BODY" | grep -q 'Login or password is incorrect' || { echo "unexpected: \$BODY" >&2; sudo -n journalctl -u $V2_UNIT -n 60 --no-pager; exit 1; }
echo V2_LOCAL_OK
REMOTE

echo "==> Add ProxyPass /v2/ to the api.perle-rare.info vhost (backup + configtest + graceful reload)"
"${SSH[@]}" bash -s <<'REMOTE'
set -euo pipefail
SNIP=/home/administrateur/api-v2-staging.snippet
BACKUPS=/home/administrateur/backups
VHOSTS=$(grep -l 'ServerName api.perle-rare.info' /etc/apache2/sites-enabled/*.conf | xargs -n1 readlink -f | sort -u)
TARGETS=""
for f in $VHOSTS; do
  if grep -Eq '^\s*ProxyPass\s+/\s+http://127\.0\.0\.1:5000/?\s*$' "$f"; then TARGETS="$TARGETS $f"; fi
done
test -n "$TARGETS" || { echo "No api.perle-rare.info vhost with 'ProxyPass / http://127.0.0.1:5000/'" >&2; exit 1; }
TS=$(date +%Y%m%d%H%M%S)
mkdir -p "$BACKUPS/apache"
CHANGED=""
for f in $TARGETS; do
  if grep -q 'ProxyPass /v2/' "$f"; then echo "already patched: $f"; continue; fi
  cp "$f" "$BACKUPS/apache/$(basename "$f").$TS"
  python3 - "$f" "$SNIP" <<'PY'
import pathlib, re, sys
vhost = pathlib.Path(sys.argv[1])
snippet = pathlib.Path(sys.argv[2]).read_text().rstrip("\n") + "\n"
lines = vhost.read_text().splitlines(keepends=True)
root = re.compile(r"^\s*ProxyPass\s+/\s+http://127\.0\.0\.1:5000/?\s*$")
out = []
for line in lines:
    if root.match(line):
        out.append(snippet)
    out.append(line)
pathlib.Path("/tmp/pr-api-vhost.next").write_text("".join(out))
PY
  sudo -n cp /tmp/pr-api-vhost.next "$f"
  rm -f /tmp/pr-api-vhost.next
  CHANGED="$CHANGED $f"
  echo "patched: $f (backup $BACKUPS/apache/$(basename "$f").$TS)"
done
if ! sudo -n apache2ctl -t; then
  for f in $CHANGED; do sudo -n cp "$BACKUPS/apache/$(basename "$f").$TS" "$f"; done
  echo "configtest failed — vhost restored" >&2
  exit 1
fi
sudo -n systemctl reload apache2
echo APACHE_RELOADED
REMOTE

echo "==> Verify current API untouched"
"${SSH[@]}" bash -s <<REMOTE
set -euo pipefail
test "\$(sha256sum $PROD_DLL | awk '{print \$1}')" = "$PROD_SHA"
test "\$(systemctl show -p MainPID --value $PROD_UNIT)" = "$PROD_PID"
systemctl is-active --quiet $PROD_UNIT
systemctl is-active --quiet $V2_UNIT
rm -rf $STAGING $STAGING.unit $STAGING.snippet
df -h / /home
REMOTE

echo "==> Public smoke"
AUTH="$(curl -sS -w '\n%{http_code}' -H 'Content-Type: application/json' -d '{"login":"x","password":"y"}' "$PUBLIC_V2/api/User/authenticate")"
echo "$AUTH" | tail -1 | sed 's/^/v2_authenticate_status=/'
echo "$AUTH" | grep -q 'Login or password is incorrect'
curl -sS -o /dev/null -w "v2_refresh_status=%{http_code} (401 expected without token)\n" "$PUBLIC_V2/api/User/refresh"
curl -sS -o /dev/null -w "current_api_status=%{http_code}\n" "https://api.perle-rare.info/api/Test/info"
curl -sS -o /dev/null -w "current_api_auth_status=%{http_code}\n" -H 'Content-Type: application/json' -d '{"login":"x","password":"y"}' "https://api.perle-rare.info/api/User/authenticate"

echo
echo "OK — API v2: $PUBLIC_V2/api/   (current API unchanged: sha=$PROD_SHA pid=$PROD_PID)"
echo "Next: cross-token check (runbook), then the front deploy."
