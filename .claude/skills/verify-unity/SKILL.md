---
name: verify-unity
description: Drive the Project Vampire Spawn Unity game the way a player does, through the Unity CLI, while the Unity Editor window stays in the background. Use to prove a change works in the real game (play mode, input, screenshots, console, tests) before reporting done, and for every live lane in docs/systems/unity-port-plan.md.
---

# verify-unity

The game runs inside the Unity Editor (6000.3.27f2, URP, Input System). Everything here goes through `unity command` (the `com.unity.pipeline` package). Never use Unity MCP, never hand-edit scene or asset YAML, never focus or click the Editor window (CLAUDE.md). The Editor may sit behind other windows the whole time.

Helper is `.claude/skills/verify-unity/scripts/vu.sh`, run from Git Bash. It targets the repo it lives in, or `VU_PROJECT=<path>` (a worktree). `VU_RUN=<id>` names the evidence folder `verify-artifacts/<id>/` (gitignored, survives cleanup).

```bash
V=.claude/skills/verify-unity/scripts/vu.sh
export VU_RUN=p8-lane1
```

## Why background needs setup

Measured on this machine with the Editor unfocused, 2026-11-05.

- Play mode with defaults runs about 1 frame per second. Coroutines, animation and enemy turns stall.
- `editor.playmode set_run_in_background --enabled true` set before `editor.playmode start` gives 340 to 400 frames per second unfocused. It overrides only the Editor session, not `ProjectSettings` or builds. Setting it during play mode does not survive the stop, so set it before every start. `vu.sh play` does.
- `editor set_auto_tick` keeps edit-mode commands (eval, capture, tests) responsive unfocused. `vu.sh` turns it on in launch, doctor and play.
- Simulated key and pointer events reach the game in the background with default Input System settings. Read them from the game's frame update, not with a bare `script eval` (see Gotchas).

## Launch

- `$V launch` uses the Editor already open for this project, or opens one with `unity open` and records that it did. It waits for `unity status` state `ready` and no compile, then enables background mode. Prints `READY <path>`.
- First open of a fresh worktree imports Synty (about 390 MB). Before it, link the assets, because they are not in git: `cmd //c mklink //J "<worktree>\Assets\Synty" "C:\Unity Projects\Project Vampire Spawn\Assets\Synty"`.
- One Editor per project folder. Two agents never drive the same Editor. A second agent works in its own worktree with `VU_PROJECT`.

## Doctor

`$V doctor` is read-only apart from re-asserting background mode. It fails when no Editor is open for the project, the Editor version differs from `ProjectSettings/ProjectVersion.txt`, the state is not `ready`, or `console status` reports compile errors. On success it prints `OK project=... version=6000.3.27f2 playMode=... runInBackground=true pid=...`. Run it first, and again whenever a command misbehaves.

## Drive

| Need | Command | Observable result |
|---|---|---|
| Enter play mode | `$V play` | clears the console, then `PLAYING frames/s=<n>`. Fails if n < 20 (throttled) |
| Leave play mode | `$V stop` | `STOPPED` |
| Press a key | `$V key Space` (`down`/`up` to hold) | `true` from the CLI, then game state |
| Pointer | `$V click <x> <y>` (`move`/`down`/`up`) | screen pixels, origin bottom-left, game view 1280x720 by default |
| See what the game receives | `$V input-probe` once per play session, then `$V input-read` | `vu-input-probe pos=(x, y) left=True space=False` |
| Read game state | `$V eval '<C# expression>'` | the expression value. Read-only expressions only |
| Console | `$V console error 50` | JSON entries, also saved to the evidence folder |
| Tests | `$V test editor <filter>` or `$V test playmode <filter>` | JSON summary with Passed and Failed |
| Recompile check | `unity --no-banner command --project-path "$PROJECT" script recompile` then `$V doctor` | doctor OK |

Build Settings scene 0 is what play mode starts in only when it is the open scene. Open it first with `unity --no-banner command --project-path "<path>" scene open --path <scene>`.

## Evidence

- Screenshot of the game view: `$V shot <slug>` writes `verify-artifacts/$VU_RUN/<slug>.png` and prints the path. `$V window Console <slug>` captures an Editor window.
- Every `eval` result is appended to `verify-artifacts/$VU_RUN/eval.log`. Console and test JSON land next to it.
- Prove the player path. Drive with `key` and `click`, then show the resulting state with a screenshot and a read-only `eval` of the game model. Setting game state through `eval` is setup, never proof.
- Capture the action and the result (before and after shots), and the console showing no new errors.

## Cleanup

`$V cleanup` stops play mode, closes the Editor only if `vu.sh launch` opened it (`unity close`, never killing processes), and prints the evidence folder, which it leaves in place. A user's own Editor stays open. Delete old `verify-artifacts/` folders by hand when they are no longer needed.

## Gotchas

- `script eval` runs in the Editor update, which has its own Input System state. It reads `(0.00, 0.00)` for a pointer the game did receive. Use `input-probe` and `input-read`, or read the game's own model.
- Human-format CLI output prints `Error:` with nothing after it on success. Use `--json` (as `vu.sh` does) and check `Success`.
- PlayMode tests cannot run synchronously over the CLI (entering play mode reloads the domain and drops the request). `$V test playmode <filter>` starts them with `--async-tests` and polls `test status`; its JSON uses lower-case `summary.passed`/`failed`.
- `unity test` and `unity build` in batchmode fail while an Editor has the project open. Send tests to the open Editor with `$V test`.
- An `eval` whose code throws or fails to compile makes `vu.sh` exit 1 and print nothing. Rerun it as `unity --no-banner command --project-path "<path>" script eval --code '<code>' --json` to see the error.
- Feature recipes live in `features/`. Read `features/README.md` before driving.
- `asset find --type AnimationClip` (or `Sprite`) returns 0, because it matches only an asset's main type and clips and sprites are sub-assets of FBX and texture files. Count them with `$V eval 'UnityEditor.AssetDatabase.FindAssets("t:AnimationClip", new[]{"Assets/Synty/<folder>"}).Length'`.
- `$V window Console <slug>` fails when the Console window is not open in that Editor. The error lists the open windows. Use `$V console` for console evidence instead.
- `vu.sh eval` uses the CLI's 30 s default. A long editor call (for example `AssetDatabase.Refresh()` after a checkout) needs `unity --no-banner command --project-path "<path>" script eval --timeout 600 --code '...'`.
- Checking out another commit in a worktree while its Editor is open makes Unity reimport, and SIDEKICK's scripts re-add `com.unity.formats.fbx` to `Packages/manifest.json` on commits that lack it. Run `git checkout -- Packages` before switching back.
- A fresh clone upgrades `Assets/Settings/Mobile_RPAsset.asset` (URP asset version 12 to 13) on first import. P1 commits that upgrade, so a later diff there is new.
