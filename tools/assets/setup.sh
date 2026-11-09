#!/usr/bin/env bash
# Imports only the Synty packages this repo needs, then proves every referenced asset resolves.
# tools/assets/setup.sh [package dir]   (default UPackage/, or $SYNTY_PACKAGES). Guide: docs/guides/asset-setup.md
set -euo pipefail
ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
cd "$ROOT"
PKG_DIR="${1:-${SYNTY_PACKAGES:-$ROOT/UPackage}}"
V=.claude/skills/verify-unity/scripts/vu.sh

report=$(node tools/assets/check.mjs "$PKG_DIR" || true)
echo "$report"
need=$(echo "$report" | awk '$1=="필요"{print $2}')
if [ -n "$need" ]; then
  absent=$(for f in $need; do [ -f "$PKG_DIR/$f" ] || echo "$f"; done)
  # 팀원 전용 회사 드라이브 받기 도구. install-skill 이 exe 위치를 스킬 파일에 적어 둔다.
  NR="${NESTRESOURCES:-$(sed -n 's/^실행 명령: `"\(.*\)"`$/\1/p' ~/.claude/skills/nestresources/SKILL.md 2>/dev/null || true)}"
  if [ -n "$absent" ] && [ -n "$NR" ] && [ -f "$NR" ]; then
    ids=$("$NR" find --json --no-update | node -e 'const w=new Set(process.argv.slice(1));console.log(JSON.parse(require("fs").readFileSync(0,"utf8")).filter(p=>p.files.some(f=>w.has(f.name))).map(p=>p.id).join(" "))' $absent)
    echo "== NestResources 로 받기: $ids"
    "$NR" pull $ids --to "$PKG_DIR" --kinds unity ${NESTRESOURCES_YES:+--yes} || true
    # pull 은 <폴더>/<id>/<파일> 에 둔다. check.mjs·임포트는 <폴더>/<파일> 을 본다.
    for id in $ids; do [ -d "$PKG_DIR/$id" ] && mv "$PKG_DIR/$id"/*.unitypackage "$PKG_DIR/" && rmdir "$PKG_DIR/$id"; done
  fi
  for f in $need; do [ -f "$PKG_DIR/$f" ] || { echo "없음: $PKG_DIR/$f  (팀원은 NestResources 스킬을 깔면 저절로 받는다. 아니면 Synty 구매 계정에서 같은 판을 받아 이 폴더에 둔다)"; missing=1; }; done
  [ -z "${missing:-}" ] || exit 1
  bash "$V" launch
  for f in $need; do echo "== 임포트 $f"; bash tools/import-unitypackage.sh "$PKG_DIR/$f"; done
  node tools/assets/check.mjs "$PKG_DIR" || { echo "임포트 뒤에도 빠진 에셋이 있다. 패키지 판을 확인한다."; exit 1; }
fi

bash "$V" doctor
echo "== Sidekick 레시피 굽기 (/synty-reload)"
bash tools/assets/sidekick-bake.sh
echo "== 에디터 안에서 확인 (RequiredAssetsTests)"
result=$(bash "$V" test editor RequiredAssets)
echo "$result" | grep -oE '"Summary":\{[^}]*\}|"Message":"[^"]{1,400}'
echo "$result" | grep -q '"Failed":0' || exit 1
if ! git diff --quiet -- Packages ProjectSettings; then
  echo "주의: 임포트가 Packages/ 또는 ProjectSettings/ 를 바꿨다. 커밋하기 전에 확인한다:"
  git diff --stat -- Packages ProjectSettings
fi
echo "준비 끝. 다음: bash $V test editor  /  bash $V test playmode"
