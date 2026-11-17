#!/bin/bash
# capture.sh [s1,s2,...]: stage shots for the feedback tool (docs/art/stage-dressing.md 7). Empty = every stage.
# Current shots move to history/<id>/<time>/ first, so the tool can show before/after.
set -e
D="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$D/../.." && pwd -W)"
OUT="$ROOT/verify-artifacts/stage-review"
SHOTS="$OUT/shots"
V="$ROOT/.claude/skills/verify-unity/scripts/vu.sh"
export VU_PROJECT="${VU_PROJECT:-$ROOT}"
mkdir -p "$SHOTS"

stamp=$(date +%Y%m%d-%H%M%S)
for j in "$SHOTS"/*.json; do
  [ -e "$j" ] || break
  id=$(basename "$j" .json)
  if [ -z "$1" ] || [[ ",$1," == *",$id,"* ]]; then
    mkdir -p "$OUT/history/$id/$stamp"
    mv "$SHOTS/$id".* "$OUT/history/$id/$stamp/"
  fi
done

echo "editor..."
launch=$(VU_WAIT="${VU_WAIT:-1800}" bash "$V" launch); echo "$launch"
echo "capturing ${1:-all}..."
unity --no-banner command --project-path "$VU_PROJECT" script eval --timeout 1200 --json \
  --code "VampireSpawn.Editor.StageShots.Capture(@\"$SHOTS\", \"$1\")"
echo
# Close only an editor this run opened, never one the user or another session had open.
[[ "$launch" == *"opening editor"* ]] && bash "$V" close || true
echo "done"
