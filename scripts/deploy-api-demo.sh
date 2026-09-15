#!/usr/bin/env bash
# Deploy the new API to an isolated VPS demo. Never touches production.
# Target: https://dev.perle-rare.info/api-demo/
# Process: 127.0.0.1:5080  ·  files: /home/administrateur/api-client-demo
# Does NOT change Angular /demo/ and does NOT restart api.perle-rare.info.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
KEY_FILE="${SSH_KEY:-$REPO_ROOT/git.key}"
HOST="${OVH_HOST:-142.4.216.57}"
PORT="${OVH_PORT:-2477}"
USER="${OVH_USER:-administrateur}"
DEMO_DIR="/home/administrateur/api-client-demo"
PROD_DLL="/var/www/api.perle-rare.info/ApiPerleRare.dll"
PROD_UNIT="api.perle-rare.info.service"
DEMO_UNIT="api-demo.perle-rare.info.service"
PUBLISH_DIR="$REPO_ROOT/artifacts/api-demo-publish"
SETTINGS_OUT="$REPO_ROOT/artifacts/api-demo-appsettings.json"
DEV_SETTINGS="$REPO_ROOT/api-perle-rare-decompiled/appsettings.Development.json"
SNIPPET="$REPO_ROOT/infra/ovh/apache/dev-api-demo.snippet.conf"
UNIT_SRC="$REPO_ROOT/infra/ovh/systemd/api-demo.perle-rare.info.service"

SSH=(ssh -i "$KEY_FILE" -p "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new "$USER@$HOST")
RSYNC_SSH_WRAP="/tmp/pr-rsync-ssh.sh"
cat > "$RSYNC_SSH_WRAP" <<WRAP
#!/usr/bin/env bash
exec ssh -i "$KEY_FILE" -p "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new "\$@"
WRAP
chmod +x "$RSYNC_SSH_WRAP"

if [[ ! -f "$KEY_FILE" ]]; then
	echo "SSH key not found: $KEY_FILE" >&2
	exit 1
fi
if [[ ! -f "$DEV_SETTINGS" ]]; then
	echo "Missing $DEV_SETTINGS (gitignored). Needed to build SELECT-only demo settings." >&2
	exit 1
fi
test -f "$SNIPPET"
test -f "$UNIT_SRC"

echo "==> Preflight disk + prod fingerprint"
PROD_SHA="$("${SSH[@]}" "sha256sum $PROD_DLL | awk '{print \$1}'")"
PROD_PID="$("${SSH[@]}" "systemctl show -p MainPID --value $PROD_UNIT")"
"${SSH[@]}" bash -s <<REMOTE
set -euo pipefail
df -h / /home
AVAIL_HOME_KB=\$(df -k /home | awk 'NR==2 {print \$4}')
AVAIL_ROOT_KB=\$(df -k / | awk 'NR==2 {print \$4}')
test "\$AVAIL_HOME_KB" -gt 500000
test "\$AVAIL_ROOT_KB" -gt 200000
test -f $PROD_DLL
systemctl is-active --quiet $PROD_UNIT
ss -ltn > /tmp/pr-listen-pre.txt
grep -F '127.0.0.1:5000' /tmp/pr-listen-pre.txt >/dev/null
echo PREFLIGHT_OK
REMOTE
echo "Prod DLL sha256=$PROD_SHA pid=$PROD_PID"

echo "==> Generate demo appsettings (SELECT-only DB, dummy JWT, isolated uploads)"
mkdir -p "$REPO_ROOT/artifacts"
python3 - <<PY
import json
from pathlib import Path
src = json.loads(Path(r"$DEV_SETTINGS").read_text())
cs = src["ConnectionStrings"]["PerleRareDB"].replace("Server=142.4.216.57", "Server=127.0.0.1")
src["ConnectionStrings"]["PerleRareDB"] = cs
src["ConnectionStrings"]["Exchange"] = ""
src["AppSettings"]["YanportToken"] = ""
src["AppSettings"]["OldWebSiteFolder"] = "$DEMO_DIR/uploads"
if not src["AppSettings"].get("Secret"):
    src["AppSettings"]["Secret"] = "LOCAL-DEV-JWT-SECRET-DO-NOT-USE-IN-PROD"
Path(r"$SETTINGS_OUT").write_text(json.dumps(src, indent=2) + "\n")
print("settings_written")
PY

echo "==> Publish self-contained linux-x64 (no .NET 8 install on VPS)"
export PATH="${HOME}/.dotnet:${PATH}"
export DOTNET_ROOT="${HOME}/.dotnet"
# Stale DLLs in this folder (e.g. JwtBearer 7.x) survive `dotnet publish -o` and crash Kestrel on start.
rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"
dotnet publish "$REPO_ROOT/api-perle-rare-decompiled/ApiPerleRare.csproj" \
	-c Release -r linux-x64 --self-contained true \
	-p:PublishTrimmed=false \
	-o "$PUBLISH_DIR"
find "$PUBLISH_DIR" -name 'appsettings*.json' -delete
cp "$SETTINGS_OUT" "$PUBLISH_DIR/appsettings.json"
chmod +x "$PUBLISH_DIR/ApiPerleRare"
test -x "$PUBLISH_DIR/ApiPerleRare"
PUB_MB=$(du -sm "$PUBLISH_DIR" | awk '{print $1}')
echo "Publish size: ${PUB_MB}M"
test "$PUB_MB" -lt 250

echo "==> Rsync -> $DEMO_DIR only"
"${SSH[@]}" "mkdir -p $DEMO_DIR/uploads $DEMO_DIR/logs"
rsync -az --delete \
	--exclude 'logs/' \
	--exclude 'uploads/' \
	--exclude '.dev.vhost.bak.*' \
	-e "$RSYNC_SSH_WRAP" \
	"$PUBLISH_DIR/" "$USER@$HOST:$DEMO_DIR/"

echo "==> Install isolated systemd unit (does not touch $PROD_UNIT)"
scp -i "$KEY_FILE" -P "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new \
	"$UNIT_SRC" "$USER@$HOST:/tmp/$DEMO_UNIT"
scp -i "$KEY_FILE" -P "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new \
	"$SNIPPET" "$USER@$HOST:$DEMO_DIR/.apache-snippet.conf"
"${SSH[@]}" bash -s <<REMOTE
set -euo pipefail
test -x $DEMO_DIR/ApiPerleRare
test -f $DEMO_DIR/appsettings.json
sudo -n cp /tmp/$DEMO_UNIT /etc/systemd/system/$DEMO_UNIT
rm -f /tmp/$DEMO_UNIT
sudo -n systemctl daemon-reload
sudo -n systemctl enable $DEMO_UNIT
sudo -n systemctl restart $DEMO_UNIT
for i in 1 2 3 4 5 6 7 8 9 10; do
  ss -ltn > /tmp/pr-listen-demo.txt
  if grep -F '127.0.0.1:5080' /tmp/pr-listen-demo.txt >/dev/null; then
    echo DEMO_LISTEN_OK
    break
  fi
  sleep 1
done
ss -ltn > /tmp/pr-listen-demo.txt
grep -F '127.0.0.1:5080' /tmp/pr-listen-demo.txt >/dev/null
REMOTE

echo "==> Patch DEV vhost only (backup + configtest + reload)"
"${SSH[@]}" bash -s <<'REMOTE'
set -euo pipefail
VHOST=/etc/apache2/sites-available/dev.perle-rare.info.conf
SNIP=/home/administrateur/api-client-demo/.apache-snippet.conf
test -f "$VHOST"
test -f "$SNIP"
grep -q 'ServerName dev.perle-rare.info' "$VHOST"
if grep -q 'ProxyPass /api-demo/' "$VHOST"; then
  echo VHOST_ALREADY_PATCHED
else
  BAK=/home/administrateur/api-client-demo/.dev.vhost.bak.$(date +%Y%m%d%H%M%S)
  sudo -n cp "$VHOST" "$BAK"
  sudo -n chown administrateur:administrateur "$BAK"
  python3 - "$VHOST" "$SNIP" <<'PY'
import pathlib, sys
vhost = pathlib.Path(sys.argv[1])
snippet = pathlib.Path(sys.argv[2]).read_text().rstrip() + "\n\n"
text = vhost.read_text()
idx = text.rfind("</VirtualHost>")
if idx < 0:
    raise SystemExit("unexpected vhost footer")
pathlib.Path("/tmp/dev.perle-rare.info.conf.next").write_text(text[:idx] + snippet + text[idx:])
PY
  sudo -n cp /tmp/dev.perle-rare.info.conf.next "$VHOST"
  rm -f /tmp/dev.perle-rare.info.conf.next
  echo VHOST_PATCHED
fi
sudo -n apache2ctl -t
sudo -n systemctl reload apache2
echo APACHE_RELOADED
REMOTE

echo "==> Verify prod untouched + demo live"
"${SSH[@]}" bash -s <<REMOTE
set -euo pipefail
test "\$(sha256sum $PROD_DLL | awk '{print \$1}')" = "$PROD_SHA"
test "\$(systemctl show -p MainPID --value $PROD_UNIT)" = "$PROD_PID"
systemctl is-active --quiet $PROD_UNIT
systemctl is-active --quiet $DEMO_UNIT
ss -ltn > /tmp/pr-listen-post.txt
grep -F '127.0.0.1:5000' /tmp/pr-listen-post.txt >/dev/null
grep -F '127.0.0.1:5080' /tmp/pr-listen-post.txt >/dev/null
df -h / /home
REMOTE

echo "==> HTTP smoke"
curl -sfI "https://dev.perle-rare.info/demo/" | head -5
echo
curl -sS -o /tmp/pr-api-demo-swagger.html -w "swagger_status=%{http_code}\n" \
	"https://dev.perle-rare.info/api-demo/swagger/index.html"
grep -q 'swagger' /tmp/pr-api-demo-swagger.html
rm -f /tmp/pr-api-demo-swagger.html
curl -sS -o /tmp/pr-api-demo-auth.json -w "authenticate_status=%{http_code}\n" \
	-H 'Content-Type: application/json' \
	-d '{"login":"x","password":"y"}' \
	"https://dev.perle-rare.info/api-demo/api/User/authenticate"
python3 - <<'PY'
import json
from pathlib import Path
doc = json.loads(Path("/tmp/pr-api-demo-auth.json").read_text())
assert doc.get("message") == "Login or password is incorrect", doc
print("authenticate_contract_ok")
PY
rm -f /tmp/pr-api-demo-auth.json
curl -sS -o /dev/null -w "prod_api_status=%{http_code}\n" "https://api.perle-rare.info/api/Test/info"

echo
echo "OK — demo API: https://dev.perle-rare.info/api-demo/"
echo "Swagger:       https://dev.perle-rare.info/api-demo/swagger/index.html"
echo "Front demo unchanged: https://dev.perle-rare.info/demo/"
echo "Prod fingerprint unchanged: $PROD_SHA pid=$PROD_PID"
