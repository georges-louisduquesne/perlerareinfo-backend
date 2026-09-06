#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"$SCRIPT_DIR/ssh.sh" bash -s <<'REMOTE'
set -euo pipefail

echo "========== SYSTEM =========="
hostname
uname -a
cat /etc/os-release 2>/dev/null || true

echo
echo "========== RESOURCES =========="
df -h
free -h 2>/dev/null || true

echo
echo "========== ROOT =========="
ls -la /

echo
echo "========== HOME =========="
ls -la /home

echo
echo "========== WEB ROOTS =========="
ls -la /var/www 2>/dev/null || echo "(no /var/www)"
ls -la /var/www/html 2>/dev/null || true

echo
echo "========== APACHE SITES =========="
ls -la /etc/apache2/sites-enabled 2>/dev/null || echo "(no apache sites-enabled)"

echo
echo "========== SERVICES =========="
systemctl is-active apache2 2>/dev/null || true
systemctl is-active nginx 2>/dev/null || true
systemctl is-active docker 2>/dev/null || true

echo
echo "========== RUNNING PROCESSES =========="
ps aux | grep -E 'kestrel|dotnet|apache|nginx|php|mysql|postgres' | grep -v grep || true

echo
echo "========== DOCKER =========="
docker ps 2>/dev/null || echo "(docker not available)"

echo
echo "========== USER HOME =========="
ls -la ~ 2>/dev/null || true

echo
echo "========== GIT REPOS (depth 2) =========="
find /home -maxdepth 3 -name '*.git' -o -type d -name '*.git' 2>/dev/null | head -30 || true
REMOTE
