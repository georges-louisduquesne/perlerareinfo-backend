#!/usr/bin/env bash
# Déploie sur perle-rare.com (WordPress) :
# - les icônes Google (google-brands.png 30x30 et google-brands-2.png 25x26)
# - le mu-plugin d'avertissement dans l'éditeur WordPress (perle-rare-linkedin-crm-notice.php)
# - le mu-plugin qui masque le nom en doublon dans le bandeau des fiches (perle-rare-fiche-banner.php)
# Ne modifie aucune page en base : l'API prod publie ensuite au fil des enregistrements CRM.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
KEY_FILE="${SSH_KEY:-$REPO_ROOT/git.key}"
HOST="${OVH_HOST:-142.4.216.57}"
PORT="${OVH_PORT:-2477}"
USER="${OVH_USER:-administrateur}"

SSH=(ssh -i "$KEY_FILE" -p "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new "$USER@$HOST")
SCP=(scp -i "$KEY_FILE" -P "$PORT" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new)

ICON_1="$SCRIPT_DIR/assets/google-brands.png"
ICON_2="$SCRIPT_DIR/assets/google-brands-2.png"
MU_PLUGIN="$SCRIPT_DIR/mu-plugins/perle-rare-linkedin-crm-notice.php"
BANNER_PLUGIN="$SCRIPT_DIR/mu-plugins/perle-rare-fiche-banner.php"

test -f "$KEY_FILE"
test -f "$ICON_1"
test -f "$ICON_2"
test -f "$MU_PLUGIN"
test -f "$BANNER_PLUGIN"

"${SCP[@]}" "$ICON_1" "$ICON_2" "$MU_PLUGIN" "$BANNER_PLUGIN" "$USER@$HOST:/tmp/"

"${SSH[@]}" bash -s <<'REMOTE'
set -euo pipefail
WP_UPLOADS="/var/www/perle-rare.com-v2/web/wp-content/uploads/2021/02"
WP_MU="/var/www/perle-rare.com-v2/web/wp-content/mu-plugins"
test -d "$WP_UPLOADS"
test -d "$WP_MU"
sudo -n install -o www-data -g www-data -m 0644 /tmp/google-brands.png "$WP_UPLOADS/google-brands.png"
sudo -n install -o www-data -g www-data -m 0644 /tmp/google-brands-2.png "$WP_UPLOADS/google-brands-2.png"
sudo -n install -o www-data -g www-data -m 0644 /tmp/perle-rare-linkedin-crm-notice.php "$WP_MU/perle-rare-linkedin-crm-notice.php"
sudo -n install -o www-data -g www-data -m 0644 /tmp/perle-rare-fiche-banner.php "$WP_MU/perle-rare-fiche-banner.php"
rm -f /tmp/google-brands.png /tmp/google-brands-2.png /tmp/perle-rare-linkedin-crm-notice.php /tmp/perle-rare-fiche-banner.php
php -l "$WP_MU/perle-rare-linkedin-crm-notice.php"
php -l "$WP_MU/perle-rare-fiche-banner.php"
echo WP_CRM_SYNC_ASSETS_OK
REMOTE

curl -sS -o /dev/null -w "google-brands.png=%{http_code}\n" "https://perle-rare.com/wp-content/uploads/2021/02/google-brands.png"
curl -sS -o /dev/null -w "google-brands-2.png=%{http_code}\n" "https://perle-rare.com/wp-content/uploads/2021/02/google-brands-2.png"
