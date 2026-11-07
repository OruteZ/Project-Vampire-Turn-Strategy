#!/usr/bin/env bash
# Bakes Sidekick recipes (Assets/_Project/Art/Characters/*.sk) in the open Editor, the same code as the Tools window button.
# tools/assets/sidekick-bake.sh [name]   SIDEKICK_BAKE_FORCE=1 rebakes even when the recipe hash matches. Guide: docs/guides/asset-setup.md
set -euo pipefail
ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
cd "$ROOT"
ls Assets/_Project/Art/Characters/*.sk >/dev/null 2>&1 || { echo "레시피 없음 (Assets/_Project/Art/Characters/*.sk)"; exit 0; }
u(){ unity --no-banner command --project-path "$ROOT" "$@"; }
installed=$(u script eval --json --code 'return VampireSpawn.Editor.SyntyReload.InstallBaker().ToString();' | grep -oE '"result":"[^"]*"' || true)
if echo "$installed" | grep -q True; then
  u script recompile >/dev/null
  for i in $(seq 1 120); do s=$(u script recompile_status 2>&1 || true); echo "$s" | grep -qE "compiling|triggered|찾을 수 없|not find" || break; sleep 2; done
fi
force=$([ "${SIDEKICK_BAKE_FORCE:-0}" = 1 ] && echo true || echo false)
out=$(u script eval --timeout 900 --json --code "return VampireSpawn.Editor.SyntyReload.Bake(\"${1:-}\", $force);")
res=$(echo "$out" | grep -oE '"result":"[^"]*"' || true)
[ -n "$res" ] || { echo "굽기 실패:"; echo "$out" | grep -oE '"(errorDetails|message)":"[^"]{0,400}' | head -5; exit 1; }
echo "$res"
echo "$res" | grep -q 'FAIL' && exit 1 || true
