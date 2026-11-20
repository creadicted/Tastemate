#!/usr/bin/env bash
# Syntax check of the plugin's browser scripts. A syntax error in the page script stops the whole page
# (no lists, dead buttons) and nothing else in the build notices it.
set -euo pipefail
cd "$(dirname "$0")/.."

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

node -e '
const fs = require("fs");
const html = fs.readFileSync("Jellyfin.Plugin.Tastemate/Configuration/configPage.html", "utf8");
const scripts = [...html.matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(m => m[1]);
if (!scripts.length) { console.error("no inline script found in configPage.html"); process.exit(1); }
scripts.forEach((s, i) => fs.writeFileSync(process.argv[1] + "/page" + i + ".js", s));
' "$tmp"

for f in "$tmp"/page*.js Jellyfin.Plugin.Tastemate/Web/*.js; do
  node --check "$f"
  echo "ok: ${f#$tmp/}"
done
