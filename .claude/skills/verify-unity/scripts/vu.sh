#!/usr/bin/env bash
set -euo pipefail

PROJECT="${VU_PROJECT:-$(git rev-parse --show-toplevel 2>/dev/null || pwd)}"
PROJECT="$(cd "$PROJECT" && pwd -W 2>/dev/null || pwd)"
RUN_ID="${VU_RUN:-default}"
ART="$PROJECT/verify-artifacts/$RUN_ID"
STATE="$PROJECT/verify-artifacts/.state"
mkdir -p "$ART" "$STATE"

u() { unity --no-banner command --project-path "$PROJECT" "$@"; }
uj() { u "$@" --json; }
field() { grep -oE "\"$1\":(\"[^\"]*\"|[^,}]*)" | head -1 | cut -d: -f2- | tr -d '"'; }
die() { echo "FAIL $*" >&2; exit 1; }

editor_line() { unity --no-banner status --format tsv --no-header 2>/dev/null | awk -F'\t' -v p="$PROJECT" '{q=$3; gsub(/\\/,"/",q); gsub(/\\/,"/",p)} tolower(q)==tolower(p)'; }

wait_ready() {
  for _ in $(seq 1 "${1:-180}"); do
    [ "$(editor_line | cut -f2)" = ready ] && [ "$(uj editor status | field compiling)" = false ] && return 0
    sleep 2
  done
  die "editor for $PROJECT not ready"
}

frames() { uj script eval --code 'UnityEngine.Time.frameCount' | field result; }

background_mode() {
  uj editor set_auto_tick --enabled true --interval-ms 16 >/dev/null
  uj editor.playmode set_run_in_background --enabled true >/dev/null
}

# Machine-wide editor slots shared by every session, worktree and project set up from this one (max 2 editors). A slot is a file naming a project.
# The folder name has no game name so unity-project-setup's rename keeps every project in one pool.
SLOTS="$(cygpath -u "${LOCALAPPDATA:-$HOME/AppData/Local}")/unity-editor-slots"
MAX_EDITORS="${VU_MAX_EDITORS:-2}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
norm() { tr '\\' '/' | tr '[:upper:]' '[:lower:]' | sed 's:/*$::'; }
ME="$(echo "$PROJECT" | norm)"
SLOT_FILE="$SLOTS/$(echo "$ME" | sed 's/[^a-z0-9]/_/g')"
# cleanup leaves a vu-opened editor running and marks it idle here; a session that finds the slots full closes the oldest idle one.
IDLE="$SLOTS/idle"
IDLE_FILE="$IDLE/$(basename "$SLOT_FILE")"

running_projects() { powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$HERE/editors.ps1")" 2>/dev/null | tr -d '\r' | norm || true; }
age() { echo $(( $(date +%s) - $(stat -c %Y "$1") )); }

lock_slots() {
  mkdir -p "$SLOTS"
  for _ in $(seq 1 120); do
    mkdir "$SLOTS/.mutex" 2>/dev/null && return 0
    [ -d "$SLOTS/.mutex" ] && [ "$(age "$SLOTS/.mutex")" -gt 60 ] && rmdir "$SLOTS/.mutex" 2>/dev/null
    sleep 0.5
  done
  die "slot mutex stuck: $SLOTS/.mutex"
}
unlock_slots() { rmdir "$SLOTS/.mutex" 2>/dev/null || true; }

# Prints "running" (editor for this project already up or starting), "taken" (new slot), or the busy project list.
try_take_slot() {
  local running busy f
  running="$(running_projects)"
  # A slot whose editor never appeared within 10 min belongs to a dead session.
  for f in "$SLOTS"/*; do
    [ -f "$f" ] || continue
    grep -qxF "$(cat "$f")" <<<"$running" || [ "$(age "$f")" -le 600 ] || rm -f "$f"
  done
  busy="$( { echo "$running"; cat "$SLOTS"/* 2>/dev/null || true; } | grep -v '^$' | sort -u || true)"
  if grep -qxF "$ME" <<<"$busy"; then echo running; return; fi
  if [ "$(grep -c . <<<"$busy" || true)" -ge "$MAX_EDITORS" ]; then echo "$busy" | tr '\n' ' '; return; fi
  echo "$ME" > "$SLOT_FILE"; echo taken
}

# Under the slot lock: claims the oldest idle editor and prints "<slot name> <project path>".
claim_idle() {
  local f; f="$(ls -tr "$IDLE"/* 2>/dev/null | head -1)"
  [ -n "$f" ] || return 1
  echo "$(basename "$f") $(cat "$f")"; rm -f "$f"
}

# ponytail: an owner that resumes between the claim and the close loses its editor; it reopens with vu.sh launch.
acquire_slot() {
  local deadline=$(( $(date +%s) + ${VU_WAIT:-0} )) v victim now
  while :; do
    lock_slots; v="$(try_take_slot)"; victim=""
    case "$v" in running|taken) ;; *) victim="$(claim_idle || true)" ;; esac
    unlock_slots
    case "$v" in running|taken) echo "$v"; return 0 ;; esac
    if [ -n "$victim" ]; then
      echo "closing idle editor ${victim#* } to free a slot" >&2
      unity --no-banner close "${victim#* }" >/dev/null 2>&1 || true
      rm -f "$SLOTS/${victim%% *}" "${victim#* }/verify-artifacts/.state/opened-by-vu"
      now=$(date +%s); [ "$deadline" -ge $(( now + 120 )) ] || deadline=$(( now + 120 ))
      sleep 5; continue
    fi
    if [ "$(date +%s)" -ge "$deadline" ]; then
      echo "WAIT editor slots full ($MAX_EDITORS): $v" >&2
      echo "Do not open another editor or close someone else's. Wait: run 'VU_WAIT=1800 vu.sh $CMD' in the background and do editor-free work meanwhile, or report '에디터 대기(2개 한도)'." >&2
      exit 3
    fi
    sleep 15
  done
}
release_slot() { rm -f "$SLOT_FILE"; }

cmd_batch() {
  local s; s="$(acquire_slot)"
  [ "$s" = taken ] || die "an editor or batch run already holds $PROJECT; batchmode would hit Unity's project lock. Use vu.sh test against the open editor"
  trap release_slot EXIT
  unity "$@"
}

cmd_launch() {
  local s=open
  [ -n "$(editor_line)" ] || s="$(acquire_slot)"
  if [ "$s" = open ]; then echo "editor already open for $PROJECT (not owned by this run)"
  elif [ "$s" = running ]; then echo "editor for $PROJECT is starting in another run"
  else
    unity --no-banner open "$PROJECT" >/dev/null 2>&1 &
    echo "$PROJECT" > "$STATE/opened-by-vu"
    echo "opening editor for $PROJECT"
  fi
  wait_ready 300
  background_mode
  echo "READY $PROJECT"
}

cmd_doctor() {
  local line; line="$(editor_line)"
  [ -n "$line" ] || die "no editor open for $PROJECT. Run: vu.sh launch"
  local want got st
  want="$(grep m_EditorVersion: "$PROJECT/ProjectSettings/ProjectVersion.txt" | awk '{print $2}')"
  got="$(echo "$line" | cut -f4)"; st="$(echo "$line" | cut -f2)"
  [ "$want" = "$got" ] || die "editor version $got, project wants $want"
  [ "$st" = ready ] || die "editor state $st"
  local cs; cs="$(uj console status)"
  [ "$(echo "$cs" | field compilationFailed)" = false ] || die "compile errors present. Run: vu.sh console error"
  background_mode
  local es; es="$(uj editor status)"
  echo "OK project=$PROJECT version=$got playMode=$(echo "$es" | field playMode) runInBackground=$(echo "$es" | field runInBackground) pid=$(echo "$line" | cut -f5)"
}

cmd_play() {
  background_mode
  if [ "$(uj editor status | field playMode)" != playing ]; then
    uj console clear >/dev/null
    uj editor.playmode start >/dev/null
  fi
  for _ in $(seq 1 60); do [ "$(uj editor status | field playMode)" = playing ] && break; sleep 1; done
  [ "$(uj editor status | field playMode)" = playing ] || die "play mode did not start"
  local a b; a="$(frames)"; sleep 1; b="$(frames)"
  [ $((b - a)) -ge 20 ] || die "play mode throttled ($((b - a)) frames in 1s). Background mode not effective"
  input_ignore_focus
  echo "PLAYING frames/s=$((b - a))"
}

# Unfocused, the Input System disables devices and drops simulated events; play mode resets the settings, so apply per session.
input_ignore_focus() {
  uj script eval --code 'var s=UnityEngine.ScriptableObject.Instantiate(UnityEngine.InputSystem.InputSystem.settings); s.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus; s.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; UnityEngine.InputSystem.InputSystem.settings=s; foreach (var d in UnityEngine.InputSystem.InputSystem.devices) UnityEngine.InputSystem.InputSystem.EnableDevice(d); 1' >/dev/null
}

cmd_stop() { uj editor.playmode stop >/dev/null; echo STOPPED; }

cmd_shot() {
  local slug="${1:?slug}" out="$ART/${1}.png"
  uj capture game_view --output-path "$out" --width "${2:-1280}" --height "${3:-720}" >/dev/null
  [ -s "$out" ] || die "no screenshot at $out"
  echo "$out"
}

cmd_window() {
  local win="${1:?window title}" slug="${2:?slug}" out="$ART/${2}.png"
  uj capture editor_window --window "$win" --output-path "$out" >/dev/null
  [ -s "$out" ] || die "no screenshot at $out"
  echo "$out"
}

cmd_eval() { uj script eval --code "${1:?code}" | tee -a "$ART/eval.log" | field result; }

cmd_key() { uj runtime simulate_key --key "${1:?key}" --action "${2:-press}" | field Success; }

cmd_click() { uj runtime simulate_pointer --x "${1:?x}" --y "${2:?y}" --action "${3:-click}" | field Success; }

cmd_input_probe() {
  cmd_eval 'var go=new UnityEngine.GameObject("vu-input-probe"); UnityEngine.InputSystem.InputSystem.onAfterUpdate += () => { if (go != null && UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType == UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic) go.name = "vu-input-probe pos=" + UnityEngine.InputSystem.Mouse.current.position.ReadValue() + " left=" + UnityEngine.InputSystem.Mouse.current.leftButton.isPressed + " space=" + UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed; }; "probe installed"'
}

cmd_input_read() {
  sleep 0.2
  cmd_eval 'System.Linq.Enumerable.First(System.Linq.Enumerable.Where(UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None), t => t.name.StartsWith("vu-input-probe"))).name'
}

cmd_console() { uj console get --level "${1:-warn}" --tail "${2:-50}" | tee "$ART/console-${1:-warn}.json"; }

cmd_test() {
  local mode="${1:-editor}" filter="${2:-}"
  if [ "$mode" != playmode ]; then uj test run --mode "$mode" --filter "$filter" | tee "$ART/test-$mode.json"; return; fi
  uj test run --mode playmode --filter "$filter" --async-tests true >/dev/null
  for _ in $(seq 1 360); do
    local st; st="$(uj test status 2>/dev/null || true)"
    if echo "$st" | grep -q '"status":"completed"'; then echo "$st" | tee "$ART/test-playmode.json"; return; fi
    sleep 5
  done
  die "playmode tests did not finish in 30 min"
}

cmd_cleanup() {
  [ "$(uj editor status 2>/dev/null | field playMode)" = playing ] && uj editor.playmode stop >/dev/null || true
  if [ -f "$STATE/opened-by-vu" ] && [ "$(cat "$STATE/opened-by-vu")" = "$PROJECT" ]; then
    mkdir -p "$IDLE" && echo "$PROJECT" > "$IDLE_FILE" && echo "left editor open as idle (closed when another session needs the slot, or now with vu.sh close)"
  else
    echo "left user's editor open"
  fi
  echo "artifacts kept in $ART"
}

cmd_close() {
  [ "$(uj editor status 2>/dev/null | field playMode)" = playing ] && uj editor.playmode stop >/dev/null || true
  rm -f "$IDLE_FILE"
  if [ -f "$STATE/opened-by-vu" ] && [ "$(cat "$STATE/opened-by-vu")" = "$PROJECT" ]; then
    unity --no-banner close "$PROJECT" >/dev/null && rm -f "$STATE/opened-by-vu" && release_slot && echo "closed editor opened by vu"
  else
    echo "left user's editor open"
  fi
}

CMD="${1:-}"
case "$CMD" in cleanup|close|"") ;; *) rm -f "$IDLE_FILE" ;; esac
case "$CMD" in
  launch|batch|doctor|play|stop|shot|window|eval|key|click|input-probe|input-read|console|test|cleanup|close) c="${1/-/_}"; shift; "cmd_$c" "$@" ;;
  *) echo "usage: vu.sh launch|batch <unity args>|doctor|play|stop|shot <slug> [w h]|window <title> <slug>|eval <code>|key <Key> [press|down|up]|click <x> <y> [click|down|up|move]|input-probe|input-read|console [log|warn|error] [n]|test [editor|playmode] [filter]|cleanup|close" >&2; exit 2 ;;
esac
