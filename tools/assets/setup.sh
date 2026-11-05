#!/usr/bin/env bash
# Imports only the Synty packages this repo needs, then proves every referenced asset resolves.
# tools/assets/setup.sh [package dir]   (default UPackage/, or $SYNTY_PACKAGES). Guide: docs/guides/asset-setup.md
set -euo pipefail
ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
cd "$ROOT"
PKG_DIR="${1:-${SYNTY_PACKAGES:-$ROOT/UPackage}}"
V=.claude/skills/verify-unity/scripts/vu.sh

if node tools/assets/check.mjs "$PKG_DIR"; then echo "임포트할 것 없음"; else
  need=$(node tools/assets/check.mjs "$PKG_DIR" | awk '$1=="필요"{print $2}' || true)
  for f in $need; do [ -f "$PKG_DIR/$f" ] || { echo "없음: $PKG_DIR/$f  (Synty 구매 계정에서 같은 판을 내려받아 이 폴더에 둔다)"; missing=1; }; done
  [ -z "${missing:-}" ] || exit 1
  bash "$V" launch
  for f in $need; do echo "== 임포트 $f"; bash tools/import-unitypackage.sh "$PKG_DIR/$f"; done
  node tools/assets/check.mjs "$PKG_DIR"
fi

bash "$V" doctor
echo "== 에디터 안에서 확인 (RequiredAssetsTests)"
bash "$V" test editor RequiredAssets | grep -oE '"Summary":\{[^}]*\}|"Message":"[^"]{1,400}'
if ! git diff --quiet -- Packages ProjectSettings; then
  echo "주의: 임포트가 Packages/ 또는 ProjectSettings/ 를 바꿨다. 커밋하기 전에 확인한다:"
  git diff --stat -- Packages ProjectSettings
fi
echo "다음: bash $V test editor  /  bash $V test playmode"
