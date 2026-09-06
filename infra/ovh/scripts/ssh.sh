#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_ROOT="$(cd "$ROOT_DIR/../.." && pwd)"
KEY_FILE="${SSH_KEY:-$REPO_ROOT/git.key}"
HOST="${OVH_HOST:-142.4.216.57}"
PORT="${OVH_PORT:-2477}"
USER="${OVH_USER:-administrateur}"

if [[ ! -f "$KEY_FILE" ]]; then
  echo "SSH key not found: $KEY_FILE" >&2
  echo "Set SSH_KEY to your private key path." >&2
  exit 1
fi

chmod 600 "$KEY_FILE" 2>/dev/null || true

exec ssh \
  -i "$KEY_FILE" \
  -p "$PORT" \
  -o IdentitiesOnly=yes \
  -o StrictHostKeyChecking=accept-new \
  "$USER@$HOST" \
  "$@"
