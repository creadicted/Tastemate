#!/usr/bin/env bash
# End-to-end test: build the plugin, serve it through a local repository manifest,
# install it into a fresh Jellyfin container via the API, restart and exercise the API.
#
# Usage: test/e2e.sh [jellyfin-image-tag]   (default: 10.11.5)
# Needs: docker or podman, dotnet, curl, python
set -euo pipefail

TAG="${1:-10.11.5}"
IMAGE="docker.io/jellyfin/jellyfin:$TAG"
NAME="tastemate-e2e"
JF_PORT=8097
VERSION="1.0.0.0"
GUID="0f930e3f-2977-4737-ac6f-65e6d9a4cc6d"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK="$(mktemp -d)"
JF="http://localhost:$JF_PORT"
AUTH='MediaBrowser Client="e2e", Device="e2e", DeviceId="e2e", Version="1"'

if command -v python >/dev/null && python -c '' 2>/dev/null; then PY=python; else PY=python3; fi

if command -v docker >/dev/null; then
  CLI=docker
elif command -v podman >/dev/null; then
  CLI=podman
else
  echo "docker or podman required" >&2
  exit 1
fi
REPO_NAME="tastemate-e2e-repo"
NET="tastemate-e2e-net"
REPO_URL="http://$REPO_NAME"

cleanup() {
  "$CLI" rm -f "$NAME" "$REPO_NAME" >/dev/null 2>&1 || true
  "$CLI" network rm "$NET" >/dev/null 2>&1 || true
  rm -rf "$WORK"
}
trap cleanup EXIT

# pyj <expr>: evaluate a Python expression on JSON from stdin as `d`; print result, exit 1 if falsy
pyj() { $PY -I -c 'import json,sys; d=json.load(sys.stdin); r=eval(sys.argv[1]); print(r); sys.exit(0 if r else 1)' "$1"; }

step() { echo "==> $*"; }
fail() { echo "FAIL: $*" >&2; "$CLI" logs --tail 40 "$NAME" >&2 || true; exit 1; }

wait_for() { # url, attempts
  for _ in $(seq 1 "$2"); do
    curl -fs -o /dev/null "$1" && return 0
    sleep 2
  done
  return 1
}

step "Build plugin $VERSION"
dotnet publish "$ROOT/Jellyfin.Plugin.Tastemate/Jellyfin.Plugin.Tastemate.csproj" -c Release -o "$WORK/out" \
  -p:Version=$VERSION -p:AssemblyVersion=$VERSION -p:FileVersion=$VERSION >/dev/null
mkdir -p "$WORK/repo"
ZIP="$WORK/repo/tastemate_$VERSION.zip"
CHECKSUM="$($PY -I - "$ZIP" "$WORK/out/Jellyfin.Plugin.Tastemate.dll" <<'PY'
import hashlib, sys, zipfile
with zipfile.ZipFile(sys.argv[1], "w", zipfile.ZIP_DEFLATED) as z:
    z.write(sys.argv[2], "Jellyfin.Plugin.Tastemate.dll")
print(hashlib.md5(open(sys.argv[1], "rb").read()).hexdigest())
PY
)"

step "Write manifest and serve it from a sidecar container"
$PY -I - "$WORK/repo/manifest.json" "$VERSION" "$CHECKSUM" "$GUID"   "$REPO_URL/tastemate_$VERSION.zip" <<'PY'
import json, sys
path, version, checksum, guid, url = sys.argv[1:]
json.dump([{"guid": guid, "name": "Tastemate", "description": "e2e", "overview": "e2e",
            "owner": "e2e", "category": "General",
            "versions": [{"version": version, "changelog": "e2e", "targetAbi": "10.11.0.0",
                          "sourceUrl": url, "checksum": checksum,
                          "timestamp": "2026-01-01T00:00:00Z"}]}], open(path, "w"))
PY
"$CLI" rm -f "$NAME" "$REPO_NAME" >/dev/null 2>&1 || true
"$CLI" network rm "$NET" >/dev/null 2>&1 || true
"$CLI" network create "$NET" >/dev/null
"$CLI" create --name "$REPO_NAME" --network "$NET" docker.io/library/nginx:alpine >/dev/null
SRC="$(cygpath -m "$WORK/repo" 2>/dev/null || echo "$WORK/repo")"
MSYS_NO_PATHCONV=1 "$CLI" cp "$SRC/." "$REPO_NAME:/usr/share/nginx/html/"
"$CLI" start "$REPO_NAME" >/dev/null

step "Start Jellyfin $TAG"
"$CLI" run -d --name "$NAME" --network "$NET" -p "$JF_PORT:8096" "$IMAGE" >/dev/null
wait_for "$JF/Startup/Configuration" 60 || fail "Jellyfin did not start"

step "Complete setup wizard"
curl -fs -X POST "$JF/Startup/Configuration" -H 'Content-Type: application/json' \
  -d '{"UICulture":"en-US","MetadataCountryCode":"US","PreferredMetadataLanguage":"en"}'
curl -fs "$JF/Startup/User" >/dev/null
curl -fs -X POST "$JF/Startup/User" -H 'Content-Type: application/json' -d '{"Name":"admin","Password":"e2e-pass"}'
curl -fs -X POST "$JF/Startup/RemoteAccess" -H 'Content-Type: application/json' \
  -d '{"EnableRemoteAccess":true,"EnableAutomaticPortMapping":false}'
curl -fs -X POST "$JF/Startup/Complete"

login() {
  curl -fs -X POST "$JF/Users/AuthenticateByName" -H 'Content-Type: application/json' \
    -H "Authorization: $AUTH" -d '{"Username":"admin","Pw":"e2e-pass"}' | pyj 'd["AccessToken"]'
}
TOKEN="$(login)"
api() { curl -fs -H "Authorization: $AUTH, Token=\"$TOKEN\"" -H 'Content-Type: application/json' "$@"; }

step "Add plugin repository"
api -X POST "$JF/Repositories" \
  -d "[{\"Name\":\"e2e\",\"Url\":\"$REPO_URL/manifest.json\",\"Enabled\":true}]" >/dev/null

step "Install Tastemate from catalog"
api "$JF/Packages" | pyj "any(p['guid'].replace('-','')=='${GUID//-/}' for p in d)" >/dev/null || fail "plugin missing from catalog"
api -X POST "$JF/Packages/Installed/Tastemate?assemblyGuid=$GUID&version=$VERSION&repositoryUrl=$REPO_URL/manifest.json" >/dev/null
sleep 5

step "Restart Jellyfin"
api -X POST "$JF/System/Restart" >/dev/null || true
sleep 5
wait_for "$JF/System/Info/Public" 60 || fail "Jellyfin did not come back after restart"
TOKEN="$(login)"

step "Check plugin is active"
STATUS="$(api "$JF/Plugins" | pyj "next((p['Status'] for p in d if p['Id'].replace('-','')=='${GUID//-/}'), '')" || true)"
[ "$STATUS" = "Active" ] || fail "plugin status is '${STATUS:-missing}', expected Active"

step "Exercise API"
api "$JF/Tastemate/Topics" | pyj 'len(d)==0' >/dev/null || fail "expected empty topic list"
TOPIC="$(api -X POST "$JF/Tastemate/Topics" -d '{"Title":"Top 3 e2e","Size":3}' | pyj 'd["Id"]')"
[ -n "$TOPIC" ] || fail "topic not created"
api "$JF/Tastemate/Topics" | pyj 'len(d)==1' >/dev/null || fail "expected one topic"

step "Rank movies that are not in the library and compare two users"
api "$JF/Tastemate/Search?query=matrix" | pyj "isinstance(d, list)" >/dev/null || fail "search endpoint failed"
api -X POST "$JF/Users/New" -d '{"Name":"mate","Password":"e2e-pass"}' >/dev/null
MATE_TOKEN="$(curl -fs -X POST "$JF/Users/AuthenticateByName" -H 'Content-Type: application/json'   -H "Authorization: $AUTH" -d '{"Username":"mate","Pw":"e2e-pass"}' | pyj 'd["AccessToken"]')"
mate_api() { curl -fs -H "Authorization: $AUTH, Token=\"$MATE_TOKEN\"" -H 'Content-Type: application/json' "$@"; }
MATE_ID="$(mate_api "$JF/Users/Me" | pyj 'd["Id"]')"
MATRIX='{"Rank":1,"TmdbId":"603","Title":"The Matrix","Year":1999}'
PULP='{"Rank":2,"TmdbId":"680","Title":"Pulp Fiction","Year":1994}'
api -X PUT "$JF/Tastemate/Topics/$TOPIC/Ranking" -d "{\"Entries\":[$MATRIX,$PULP]}" >/dev/null || fail "saving external ranking failed"
mate_api -X PUT "$JF/Tastemate/Topics/$TOPIC/Ranking"   -d '{"Entries":[{"Rank":1,"TmdbId":"680","Title":"Pulp Fiction","Year":1994},{"Rank":2,"TmdbId":"603","Title":"The Matrix","Year":1999}]}' >/dev/null   || fail "saving second ranking failed"
api "$JF/Tastemate/Topics/$TOPIC/Compare/$MATE_ID"   | pyj "len(d['Result']['Shared']) == 2 and d['Result']['Similarity'] == 44.4 and d['Movies']['tmdb:603']['Name'] == 'The Matrix'" >/dev/null   || fail "comparison of external movies is wrong"
if api -X PUT "$JF/Tastemate/Topics/$TOPIC/Ranking"   -d '{"Entries":[{"Rank":1,"TmdbId":"603","Title":"x","Poster":"https://evil.example/p.jpg"}]}' >/dev/null 2>&1; then
  fail "foreign poster address was accepted"
fi
if api -X PUT "$JF/Tastemate/Topics/$TOPIC/Ranking"   -d "{\"Entries\":[$MATRIX,{\"Rank\":2,\"TmdbId\":\"603\",\"Title\":\"The Matrix\"}]}" >/dev/null 2>&1; then
  fail "duplicate movie was accepted"
fi

api -X DELETE "$JF/Tastemate/Topics/$TOPIC" >/dev/null
api "$JF/Tastemate/Topics" | pyj 'len(d)==0' >/dev/null || fail "topic not deleted"

echo "PASS"
