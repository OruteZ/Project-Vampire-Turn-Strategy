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

cmd_launch() {
  if [ -n "$(editor_line)" ]; then echo "editor already open for $PROJECT (not owned by this run)"; else
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
  echo "PLAYING frames/s=$((b - a))"
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
  uj test run --mode "$mode" --filter "$filter" | tee "$ART/test-$mode.json"
}

cmd_cleanup() {
  [ "$(uj editor status 2>/dev/null | field playMode)" = playing ] && uj editor.playmode stop >/dev/null || true
  if [ -f "$STATE/opened-by-vu" ] && [ "$(cat "$STATE/opened-by-vu")" = "$PROJECT" ]; then
    unity --no-banner close "$PROJECT" >/dev/null && rm -f "$STATE/opened-by-vu" && echo "closed editor opened by vu"
  else
    echo "left user's editor open"
  fi
  echo "artifacts kept in $ART"
}

case "${1:-}" in
  launch|doctor|play|stop|shot|window|eval|key|click|input-probe|input-read|console|test|cleanup) c="${1/-/_}"; shift; "cmd_$c" "$@" ;;
  *) echo "usage: vu.sh launch|doctor|play|stop|shot <slug> [w h]|window <title> <slug>|eval <code>|key <Key> [press|down|up]|click <x> <y> [click|down|up|move]|input-probe|input-read|console [log|warn|error] [n]|test [editor|playmode] [filter]|cleanup" >&2; exit 2 ;;
esac
