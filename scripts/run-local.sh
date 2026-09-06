#!/usr/bin/env bash
# Start the API locally: no hosted jobs, no HTTPS redirect, SELECT-only DB.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export PATH="${HOME}/.dotnet:${PATH}"
export DOTNET_ROOT="${HOME}/.dotnet"
export ASPNETCORE_ENVIRONMENT=Development
export PR_LOCAL_SAFE=1
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://localhost:5080}"

CONF="$ROOT/api-perle-rare-decompiled/appsettings.Development.json"
if [[ ! -f "$CONF" ]]; then
  echo "Missing $CONF" >&2
  echo "Copy appsettings.example.json and fill local values (gitignored)." >&2
  exit 1
fi

cd "$ROOT/api-perle-rare-decompiled"
echo "Starting ApiPerleRare on ${ASPNETCORE_URLS} (PR_LOCAL_SAFE=1, jobs off)"
exec dotnet run --project ApiPerleRare.csproj --no-launch-profile
