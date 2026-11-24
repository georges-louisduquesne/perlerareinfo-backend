#!/usr/bin/env bash
# Déploie sur perle-rare.com (WordPress) :
# - les icônes Google (google-brands.png 30x30 et google-brands-2.png 25x26)
# - le mu-plugin d'avertissement dans l'éditeur WordPress (perle-rare-linkedin-crm-notice.php)
# - le mu-plugin qui masque le nom en doublon dans le bandeau des fiches (perle-rare-fiche-banner.php)
# - le mu-plugin qui déclare l'anglais sur les fiches conseiller anglaises (perle-rare-fiche-lang.php)
# - le mu-plugin du bouton Carte virtuelle / Virtual card (perle-rare-fiche-carte.php)
# - le mu-plugin qui lie le badge Google du pied de page (perle-rare-google-note.php)
# - le mu-plugin du bouton « Prenez contact ICI » en bas de Nous rejoindre (perle-rare-rejoindre-contact.php)
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
LANG_PLUGIN="$SCRIPT_DIR/mu-plugins/perle-rare-fiche-lang.php"
CARTE_PLUGIN="$SCRIPT_DIR/mu-plugins/perle-rare-fiche-carte.php"
GOOGLE_NOTE_PLUGIN="$SCRIPT_DIR/mu-plugins/perle-rare-google-note.php"
REJOINDRE_PLUGIN="$SCRIPT_DIR/mu-plugins/perle-rare-rejoindre-contact.php"

test -f "$KEY_FILE"
test -f "$ICON_1"
test -f "$ICON_2"
test -f "$MU_PLUGIN"
test -f "$BANNER_PLUGIN"
test -f "$LANG_PLUGIN"
test -f "$CARTE_PLUGIN"
test -f "$GOOGLE_NOTE_PLUGIN"
test -f "$REJOINDRE_PLUGIN"

"${SCP[@]}" "$ICON_1" "$ICON_2" "$MU_PLUGIN" "$BANNER_PLUGIN" "$LANG_PLUGIN" "$CARTE_PLUGIN" "$GOOGLE_NOTE_PLUGIN" "$REJOINDRE_PLUGIN" "$USER@$HOST:/tmp/"

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
sudo -n install -o www-data -g www-data -m 0644 /tmp/perle-rare-fiche-lang.php "$WP_MU/perle-rare-fiche-lang.php"
sudo -n install -o www-data -g www-data -m 0644 /tmp/perle-rare-fiche-carte.php "$WP_MU/perle-rare-fiche-carte.php"
sudo -n install -o www-data -g www-data -m 0644 /tmp/perle-rare-google-note.php "$WP_MU/perle-rare-google-note.php"
sudo -n install -o www-data -g www-data -m 0644 /tmp/perle-rare-rejoindre-contact.php "$WP_MU/perle-rare-rejoindre-contact.php"
rm -f /tmp/google-brands.png /tmp/google-brands-2.png /tmp/perle-rare-linkedin-crm-notice.php /tmp/perle-rare-fiche-banner.php /tmp/perle-rare-fiche-lang.php /tmp/perle-rare-fiche-carte.php /tmp/perle-rare-google-note.php /tmp/perle-rare-rejoindre-contact.php
php -l "$WP_MU/perle-rare-linkedin-crm-notice.php"
php -l "$WP_MU/perle-rare-fiche-banner.php"
php -l "$WP_MU/perle-rare-fiche-lang.php"
php -l "$WP_MU/perle-rare-fiche-carte.php"
php -l "$WP_MU/perle-rare-google-note.php"
php -l "$WP_MU/perle-rare-rejoindre-contact.php"
echo WP_CRM_SYNC_ASSETS_OK
REMOTE

curl -sS -o /dev/null -w "google-brands.png=%{http_code}\n" "https://perle-rare.com/wp-content/uploads/2021/02/google-brands.png"
curl -sS -o /dev/null -w "google-brands-2.png=%{http_code}\n" "https://perle-rare.com/wp-content/uploads/2021/02/google-brands-2.png"
