#!/usr/bin/env bash
set -euo pipefail
P="$(git rev-parse --show-toplevel)"; PKG="$(cygpath -m "$(realpath "$1")")"
u(){ unity --no-banner command --project-path "$P" "$@" --json; }
u console clear >/dev/null
s=$(date +%s)
CODE='UnityEditor.AssetDatabase.ImportPackage("'"$PKG"'", false); "import called"'
u script eval --timeout 3600 --code "$CODE" | grep -oE '"result":"[^"]*"'
seen=0
for i in $(seq 1 3600); do
  st=$(u editor status 2>/dev/null || true)
  if [ -z "$st" ] || echo "$st" | grep -qE '"compiling":true|"domainReloadInProgress":true|"status":"(busy|importing|compiling)"'; then seen=1
  elif echo "$st" | grep -q '"status":"ready"' && { [ $seen = 1 ] || [ $i -gt 20 ]; }; then break; fi
  sleep 1
done
echo "ready after $(( $(date +%s) - s ))s"
u console status | grep -oE '"(compilationFailed|consoleErrors|consoleWarnings)":[^,}]*' | tr '\n' ' '; echo
u console get --level error --tail 20 | grep -oE '"message":"[^"]{0,160}' || echo "no error entries"
