#!/usr/bin/env bash
# Bakes Sidekick recipes (Assets/_Project/Art/Characters/*.sk) in the open Editor. Guide: docs/guides/asset-setup.md
# tools/assets/sidekick-bake.sh [name]   SIDEKICK_BAKE_FORCE=1 rebakes even when the recipe hash matches.
set -euo pipefail
ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
cd "$ROOT"
ls Assets/_Project/Art/Characters/*.sk >/dev/null 2>&1 || { echo "레시피 없음 (Assets/_Project/Art/Characters/*.sk)"; exit 0; }
code=$(sed "s/@ONLY@/${1:-}/; s/@FORCE@/${SIDEKICK_BAKE_FORCE:-0}/" tools/assets/sidekick-bake.cs)
out=$(unity --no-banner command --project-path "$ROOT" script eval --timeout 900 --json --code "$code")
res=$(echo "$out" | grep -oE '"result":"[^"]*"' || true)
[ -n "$res" ] || { echo "굽기 실패:"; echo "$out" | grep -oE '"(errorDetails|message)":"[^"]{0,400}' | head -5; exit 1; }
echo "$res"
echo "$res" | grep -q 'FAIL' && exit 1 || true
