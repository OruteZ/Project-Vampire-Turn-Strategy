#!/usr/bin/env bash
# Windows build of local dev into Builds/VampireSpawn/ (overwritten each run); the version shows on the main menu.
# Builds from its own worktree because batchmode can't run while an Editor holds the main tree.
set -euo pipefail
MAIN="$(cd "$(dirname "$0")/../.." && pwd -W)"
TREE="${BUILD_TREE:-C:/Unity Projects/PVS-build}"
REF="${BUILD_REF:-dev}"

if [ ! -d "$TREE" ]; then
  git -C "$MAIN" worktree add -q --detach "$TREE" "$REF"
  cmd //c mklink //J "$(cygpath -w "$TREE/Assets/Synty")" "$(cygpath -w "$MAIN/Assets/Synty")" >/dev/null
  cmd //c mklink //J "$(cygpath -w "$TREE/UPackage")" "$(cygpath -w "$MAIN/UPackage")" >/dev/null
fi
# -f: the build stamps bundleVersion into this tree's ProjectSettings.
git -C "$TREE" checkout -q -f --detach "$REF"

base="$(awk '/bundleVersion:/{print $2; exit}' "$TREE/ProjectSettings/ProjectSettings.asset")"
export BUILD_VERSION="${base}-$(date +%Y%m%d)-$(git -C "$TREE" rev-parse --short HEAD)"
out="$MAIN/Builds/VampireSpawn"
rm -rf "$out"

VU_PROJECT="$TREE" VU_WAIT="${VU_WAIT:-1800}" bash "$MAIN/.claude/skills/verify-unity/scripts/vu.sh" batch \
  --no-banner build --target StandaloneWindows64 --execute-method VampireSpawn.Editor.BuildScript.Windows \
  --output-path "$out/VampireSpawn.exe" --no-tail "$TREE"
[ -f "$out/VampireSpawn.exe" ] || { echo "BUILD FAILED: no exe at $out (log: $TREE/Logs/)" >&2; exit 1; }
echo "BUILT v$BUILD_VERSION $out"
