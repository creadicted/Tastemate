#!/usr/bin/env bash
# Run Jellyfin locally with the current plugin build for manual testing.
# Re-run after code changes: it rebuilds, copies the DLL in and restarts the server.
#
# Usage:
#   test/serve.sh            build + start (or update + restart) on http://localhost:8096
#   test/serve.sh --stop     stop and remove the container, keep config and library data
#   test/serve.sh --reset    stop and also delete all Jellyfin data (fresh setup wizard)
#
# Env:
#   MEDIA_DIR   host folder mounted read-only at /media (movies for ranking), optional
#   TAG         Jellyfin image tag, default 10.11.5
#   PORT        host port, default 8096
set -euo pipefail

TAG="${TAG:-10.11.5}"
PORT="${PORT:-8096}"
NAME="tastemate-dev"
VOLUME="tastemate-dev-config"
IMAGE="docker.io/jellyfin/jellyfin:$TAG"
export MSYS_NO_PATHCONV=1  # Git Bash: do not rewrite container paths like /config
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ROOT="$(cygpath -m "$ROOT" 2>/dev/null || echo "$ROOT")"  # Windows-style path when under Git Bash

if command -v docker >/dev/null; then CLI=docker
elif command -v podman >/dev/null; then CLI=podman
else echo "docker or podman required" >&2; exit 1
fi

case "${1:-}" in
  --stop)
    "$CLI" rm -f "$NAME" >/dev/null 2>&1 || true
    echo "Stopped. Data kept in volume $VOLUME (use --reset to delete)."
    exit 0
    ;;
  --reset)
    "$CLI" rm -f "$NAME" >/dev/null 2>&1 || true
    "$CLI" volume rm "$VOLUME" >/dev/null 2>&1 || true
    echo "Removed container and volume $VOLUME."
    exit 0
    ;;
  "") ;;
  *) echo "unknown option: $1" >&2; exit 1 ;;
esac

OUT="$ROOT/artifacts/serve"
rm -rf "$OUT"
echo "==> Build plugin"
if command -v dotnet >/dev/null; then
  dotnet publish "$ROOT/Jellyfin.Plugin.Tastemate/Jellyfin.Plugin.Tastemate.csproj" -c Debug -o "$OUT" >/dev/null
else
  # No local SDK (e.g. plain WSL): build in the SDK container
  "$CLI" run --rm -v "$ROOT:/src" -w /src mcr.microsoft.com/dotnet/sdk:9.0 \
    dotnet publish Jellyfin.Plugin.Tastemate/Jellyfin.Plugin.Tastemate.csproj -c Debug -o artifacts/serve >/dev/null
fi

echo "==> Start Jellyfin $TAG"
"$CLI" rm -f "$NAME" >/dev/null 2>&1 || true
MEDIA=()
[ -n "${MEDIA_DIR:-}" ] && MEDIA=(-v "$MEDIA_DIR:/media:ro")
"$CLI" create --name "$NAME" -p "$PORT:8096" -v "$VOLUME:/config" "${MEDIA[@]}" "$IMAGE" >/dev/null

# Copy into the stopped container so the plugin is there on first load
"$CLI" cp "$OUT/Jellyfin.Plugin.Tastemate.dll" "$NAME:/config/plugins/Tastemate/Jellyfin.Plugin.Tastemate.dll" 2>/dev/null || {
  # plugins dir does not exist yet on a fresh volume: create it via a throwaway container
  "$CLI" run --rm -v "$VOLUME:/config" --entrypoint mkdir "$IMAGE" -p /config/plugins/Tastemate
  "$CLI" cp "$OUT/Jellyfin.Plugin.Tastemate.dll" "$NAME:/config/plugins/Tastemate/Jellyfin.Plugin.Tastemate.dll"
}
"$CLI" start "$NAME" >/dev/null

echo "==> Jellyfin starting at http://localhost:$PORT"
echo "    Plugin page: Dashboard > Plugins > Tastemate"
echo "    Logs:        $CLI logs -f $NAME"
echo "    Stop:        test/serve.sh --stop"
