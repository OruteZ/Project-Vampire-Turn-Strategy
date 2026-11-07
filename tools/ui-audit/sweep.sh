#!/usr/bin/env bash
# Every screen at every aspect ratio: UiAudit report + screenshot into verify-artifacts/$VU_RUN/. Usage: VU_RUN=<id> [LOCALE=en] [ONLY=1-menu] [TOOLKIT=1] bash tools/ui-audit/sweep.sh
set -u
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
export VU_PROJECT="${VU_PROJECT:-$ROOT}"
V="$ROOT/.claude/skills/verify-unity/scripts/vu.sh"
OUT="$ROOT/verify-artifacts/${VU_RUN:?set VU_RUN}"
mkdir -p "$OUT"
REPORT="$OUT/audit.txt"
: > "$REPORT"

ev() { unity --no-banner command --project-path "$VU_PROJECT" script eval --code "$1" --json | node -e 'let s="";process.stdin.on("data",d=>s+=d).on("end",()=>{const j=JSON.parse(s);const r=j.data&&j.data.result!==undefined?j.data.result:j.result;process.stdout.write(String(r??JSON.stringify(j.errors||j)))})'; }

GF='var gf=UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.Screens.GameFlow>(); var F=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance; System.Action<VampireSpawn.Core.CampInput> send = x => typeof(VampireSpawn.Game.Screens.GameFlow).GetMethod("Send",F).Invoke(gf, new object[]{x});'
declare -A SCREENS=(
  [1-menu]="$GF gf.ShowMenu(); return 1;"
  [2a-camp-l1]="$GF gf.ShowCamp(); return 1;"
  [2b-camp-l2-s1]="$GF gf.ShowCamp(); send(VampireSpawn.Core.CampInput.Pick(0, 0f)); return 1;"
  [2c-camp-l2-s8]="$GF gf.ShowCamp(); send(VampireSpawn.Core.CampInput.Pick(7, 0f)); return 1;"
  [2d-camp-l2-s10]="$GF gf.ShowCamp(); send(VampireSpawn.Core.CampInput.Pick(9, 0f)); return 1;"
  [2e-camp-l3]="$GF gf.ShowCamp(); send(VampireSpawn.Core.CampInput.Pick(0, 0f)); send(VampireSpawn.Core.CampInput.Of(VampireSpawn.Core.CampKey.Barracks, 0f)); return 1;"
  [3-deploy]="$GF gf.StartBattle(VampireSpawn.Core.Campaign.Stages[0], null, false); return 1;"
  [4-turn]="$GF gf.input.Run(s=>s.UseSample()); gf.input.Run(s=>s.Start()); var u=System.Linq.Enumerable.First(gf.input.Session.Battle.Allies); gf.input.Run(s=>s.Click(u.Pos)); return 1;"
  [5-rules]="$GF typeof(VampireSpawn.Game.Screens.GameFlow).GetField(\"rulesOpen\",F).SetValue(gf,true); typeof(VampireSpawn.Game.Screens.GameFlow).GetMethod(\"RenderBattle\",F).Invoke(gf,null); return 1;"
  [6-log]="$GF typeof(VampireSpawn.Game.Screens.GameFlow).GetField(\"rulesOpen\",F).SetValue(gf,false); typeof(VampireSpawn.Game.Screens.GameFlow).GetField(\"logOpen\",F).SetValue(gf,true); typeof(VampireSpawn.Game.Screens.GameFlow).GetMethod(\"RenderBattle\",F).Invoke(gf,null); return 1;"
  [7-over]="$GF typeof(VampireSpawn.Game.Screens.GameFlow).GetField(\"logOpen\",F).SetValue(gf,false); typeof(VampireSpawn.Game.Screens.GameFlow).GetMethod(\"Finish\",F).Invoke(gf,null); return 1;"
)
SIZES="${SIZES:-1920x1080 1280x720 1920x1200 1440x1080 1280x1024 2560x1080}"

bash "$V" play >/dev/null || exit 1
ev "UnityEngine.PlayerPrefs.SetInt(\"ui.toolkitMenu\", ${TOOLKIT:-0}); return 1;" >/dev/null
ev "UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale = UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale(\"${LOCALE:-ko}\"); return 1;" >/dev/null
sleep 1
for size in $SIZES; do
  w=${size%x*}; h=${size#*x}
  ev "return VampireSpawn.Editor.UiAudit.Resize($w,$h);" >/dev/null
  for key in $(printf '%s\n' "${!SCREENS[@]}" | sort | grep -E "${ONLY:-.}"); do
    ev "${SCREENS[$key]}" >/dev/null
    sleep 0.5
    { echo "## $key $size"; ev "return VampireSpawn.Editor.UiAudit.Report();"; echo; } >> "$REPORT"
    bash "$V" shot "$key-$size" "$w" "$h" >/dev/null
  done
done
bash "$V" stop >/dev/null
grep -c $'\t' "$REPORT" | xargs echo "total issue lines:"
grep '^## \|issues=' "$REPORT" | paste - - | awk '{print $2, $3, $5}'
[ "$(grep -c $'\t' "$REPORT")" -eq 0 ] || { echo "FAIL: layout issues in $REPORT"; exit 1; }
echo "PASS: no layout issues"
