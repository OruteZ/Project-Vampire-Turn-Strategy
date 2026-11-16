#!/usr/bin/env bash
# Every screen at every aspect ratio: UiAudit report + screenshot into verify-artifacts/$VU_RUN/. Usage: VU_RUN=<id> [LOCALE=en] [ONLY=1-menu] bash tools/ui-audit/sweep.sh
set -u
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
export VU_PROJECT="${VU_PROJECT:-$ROOT}"
V="$ROOT/.claude/skills/verify-unity/scripts/vu.sh"
OUT="$ROOT/verify-artifacts/${VU_RUN:?set VU_RUN}"
mkdir -p "$OUT"
REPORT="$OUT/audit.txt"
: > "$REPORT"

ev() { unity --no-banner command --project-path "$VU_PROJECT" script eval --code "$1" --json | node -e 'let s="";process.stdin.on("data",d=>s+=d).on("end",()=>{const j=JSON.parse(s);const r=j.data&&j.data.result!==undefined?j.data.result:j.result;process.stdout.write(String(r??JSON.stringify(j.errors||j)))})'; }

APP='var F=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance; var c=VampireSpawn.Game.Screens.App.Campaign; var all = new VampireSpawn.Core.Campaign(); foreach (var st in VampireSpawn.Core.Campaign.Stages) all.Finish(st, true, false); System.Func<VampireSpawn.Core.CampInput[], VampireSpawn.Core.CampNav> camp = xs => { var n = VampireSpawn.Core.CampNav.FromMenu(all, null); foreach (var x in xs) n = n.Step(all, x).next; return n; };'
BF='var F=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance; var bf=UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.Screens.BattleFlow>(); var T=typeof(VampireSpawn.Game.Screens.BattleFlow);'
PICK='VampireSpawn.Core.CampInput.Pick'
# A waiting reinforcement's card length: its trigger condition plus its note (#129). The sweep hovers the longest one.
WAVE='System.Func<VampireSpawn.Core.Stage, VampireSpawn.Core.Placement, int> W = (st, p) => { if (p.Wave == null) return -1; var t = st.Triggers == null ? null : System.Linq.Enumerable.FirstOrDefault(st.Triggers, x => x.Id == p.Wave); return (t == null ? "" : VampireSpawn.Game.Loc.Text(t.Condition) ?? "").Length + (VampireSpawn.Game.Loc.Text(VampireSpawn.Core.GameData.Minion(p.Kind).Note) ?? "").Length; };'
# Camp states are stepped on a fully cleared campaign so L2 opens on any stage (#48 keeps locked stages out of L2); the screen still draws the real save.
# Each screen is its own scene (docs/dev/scene-split.md); a step loads it and the next eval finds its flow. Battle steps run in order after 3-deploy.
declare -A SCREENS=(
  [1-menu]="VampireSpawn.Game.Screens.App.GoMenu(); return 1;"
  [2a-camp-l1]="$APP VampireSpawn.Game.Screens.App.GoCamp(camp(new VampireSpawn.Core.CampInput[0])); return 1;"
  [2b-camp-l2-s1]="$APP VampireSpawn.Game.Screens.App.GoCamp(camp(new[]{ $PICK(0, 0f) })); return 1;"
  [2c-camp-l2-s8]="$APP VampireSpawn.Game.Screens.App.GoCamp(camp(new[]{ $PICK(7, 0f) })); return 1;"
  [2d-camp-l2-s10]="$APP VampireSpawn.Game.Screens.App.GoCamp(camp(new[]{ $PICK(9, 0f) })); return 1;"
  [2e-camp-l3]="$APP VampireSpawn.Game.Screens.App.GoCamp(camp(new[]{ $PICK(0, 0f), VampireSpawn.Core.CampInput.Of(VampireSpawn.Core.CampKey.Barracks, 0f) })); return 1;"
  [2f-camp-l3-buy]="$APP VampireSpawn.Game.Screens.App.GoCamp(camp(new[]{ $PICK(0, 0f), VampireSpawn.Core.CampInput.Of(VampireSpawn.Core.CampKey.Barracks, 0f), VampireSpawn.Core.CampInput.Buy(VampireSpawn.Core.GameData.Upgrades[0].Id, 0f) })); return 1;"
  [3-deploy]="VampireSpawn.Game.Screens.App.GoBattle(VampireSpawn.Core.Campaign.Stages[0], false); return 1;"
  [4-turn]="$BF bf.input.Run(s=>s.UseSample()); bf.input.Run(s=>s.Start()); var u=System.Linq.Enumerable.First(bf.input.Session.Battle.Allies); bf.input.Run(s=>s.Click(u.Pos)); return 1;"
  [5-rules]="$BF T.GetField(\"rulesOpen\",F).SetValue(bf,true); T.GetMethod(\"RenderBattle\",F).Invoke(bf,null); return 1;"
  [6-log]="$BF T.GetField(\"rulesOpen\",F).SetValue(bf,false); T.GetField(\"logOpen\",F).SetValue(bf,true); T.GetMethod(\"RenderBattle\",F).Invoke(bf,null); return 1;"
  [8a-wave]="$WAVE var best = System.Linq.Enumerable.First(System.Linq.Enumerable.OrderByDescending(VampireSpawn.Core.Campaign.Stages, st => System.Linq.Enumerable.Max(System.Linq.Enumerable.Append(System.Linq.Enumerable.Select(st.Enemies, p => W(st, p)), -1)))); VampireSpawn.Game.Screens.App.GoBattle(best, false); return 1;"
  [8b-arrival]="$BF $WAVE var b = bf.input.Session.Battle; bf.input.Run(s=>s.UseSample()); bf.input.Run(s=>s.Start()); var p = System.Linq.Enumerable.First(System.Linq.Enumerable.OrderByDescending(b.Pending, x => W(b.Stage, x))); return VampireSpawn.Editor.UiAudit.HexOnScreen(p.Col, p.Row);"
  [8c-enemy]="$BF var u = System.Linq.Enumerable.First(System.Linq.Enumerable.OrderByDescending(System.Linq.Enumerable.Where(bf.input.Session.Battle.Units, x => x.Side == VampireSpawn.Core.Side.Enemy), x => x.Stance == VampireSpawn.Core.Stance.Guard ? 1 : 0)); return VampireSpawn.Editor.UiAudit.HexOnScreen(u.Pos.Col, u.Pos.Row);"
  [7-over]="$BF T.GetField(\"logOpen\",F).SetValue(bf,false); T.GetMethod(\"Finish\",F).Invoke(bf,null); return 1;"
)
# Hover cards exist only under the pointer: these steps return "move x y", the sweep moves the pointer there and checks the card appeared.
declare -A EXPECT=([8b-arrival]="Arrival " [8c-enemy]="Panel ")
SIZES="${SIZES:-1920x1080 1280x720 1920x1200 1440x1080 1280x1024 2560x1080}"

for size in $SIZES; do
  w=${size%x*}; h=${size#*x}
  # After a few Resize calls simulated pointer moves stop reaching the game (verify-unity Gotchas); a fresh play session per size keeps the hover steps working.
  bash "$V" stop >/dev/null 2>&1
  bash "$V" play >/dev/null || exit 1
  ev "UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale = UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale(\"${LOCALE:-ko}\"); return 1;" >/dev/null
  sleep 1
  ev "return VampireSpawn.Editor.UiAudit.Resize($w,$h);" >/dev/null
  for key in $(printf '%s\n' "${!SCREENS[@]}" | sort | grep -E "${ONLY:-.}"); do
    read -r act x y <<<"$(ev "${SCREENS[$key]}")"
    [ "$act" = move ] && bash "$V" click "$x" "$y" move >/dev/null
    sleep 1
    # A step that hovers must show its card, or the sweep checked nothing.
    if [ -n "${EXPECT[$key]:-}" ] && [ "$(ev "return VampireSpawn.Editor.UiAudit.Has(\"${EXPECT[$key]}\") ? 1 : 0;")" != 1 ]; then
      printf '## %s %s\n%s issues=1\nno-card\t%s\thover did not open the card\n\n' "$key" "$size" "$size" "${EXPECT[$key]}" >> "$REPORT"; continue
    fi
    # A fresh battle opens with the start camera sweep (BattleCamera); the board is framed only after it settles (~6 s).
    case "$key" in 3-deploy|8a-wave) sleep 6;; esac
    { echo "## $key $size"; ev "return VampireSpawn.Editor.UiAudit.Report();"; echo; } >> "$REPORT"
    bash "$V" shot "$key-$size" "$w" "$h" >/dev/null
  done
done
bash "$V" stop >/dev/null
grep -c $'\t' "$REPORT" | xargs echo "total issue lines:"
grep '^## \|issues=' "$REPORT" | paste - - | awk '{print $2, $3, $5}'
[ "$(grep -c $'\t' "$REPORT")" -eq 0 ] || { echo "FAIL: layout issues in $REPORT"; exit 1; }
echo "PASS: no layout issues"
