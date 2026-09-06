#!/usr/bin/env bash
# Smoke local + lectures VPS. Aucun write distant.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

ok=0
fail=0
check() {
  local name="$1"
  shift
  if "$@" >/tmp/pr-check.out 2>/tmp/pr-check.err; then
    echo "OK   $name"
    ok=$((ok + 1))
  else
    echo "FAIL $name"
    sed 's/^/     /' /tmp/pr-check.err | head -5
    fail=$((fail + 1))
  fi
}

echo "== Perle Rare backend — check local (read-only) =="
test -f "$ROOT/git.key" || { echo "FAIL git.key missing"; exit 1; }
chmod 600 "$ROOT/git.key" 2>/dev/null || true

check "SSH TCP :2477" nc -z -G 8 142.4.216.57 2477
check "MariaDB TCP :17075" nc -z -G 8 142.4.216.57 17075
check "API /api/Test/info" curl -fsS -m 15 -o /dev/null "https://api.perle-rare.info/api/Test/info"
check "Swagger UI" curl -fsS -m 15 -o /dev/null "https://api.perle-rare.info/swagger/index.html"
check "SSH whoami" "$ROOT/infra/ovh/scripts/ssh.sh" "whoami"

echo
echo "decompiled .cs : $(find "$ROOT/api-perle-rare-decompiled" -name '*.cs' | wc -l | tr -d ' ')"
echo "publish dll    : $(test -f "$ROOT/api-perle-rare-publish/ApiPerleRare.dll" && echo present || echo missing)"
echo "dotnet sdks    : $(dotnet --list-sdks 2>/dev/null | tr '\n' ' ' || echo none)"
echo
echo "result: $ok ok / $fail fail"
test "$fail" -eq 0
