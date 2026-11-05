# Project Vampire Spawn verification map

This directory is the maintained source for verifying what a player can do in the game. Read this index before driving, then use the matching feature file as the recipe. The game is being ported from `docs/Prototype.html` (plan in `docs/dev/unity-port-plan.md`). Add a feature file when a port PR adds something a player can see or do.

## Baseline preconditions

- An Editor is open for the project folder being verified (`vu.sh launch`), and `vu.sh doctor` prints `OK`.
- `VU_RUN` is set to a fresh id so evidence from different runs does not mix.
- The scene under test is open. Today that is `Assets/Scenes/SampleScene.unity` (Build Settings 0).
- Never drive an Editor that another agent is driving.

## Driving conventions

- Every recipe starts from edit mode with no play session running.
- Drive the player path with `vu.sh key` and `vu.sh click`. Use `vu.sh eval` only to read state or to set up a precondition, and say which.
- Pointer coordinates are game-view pixels, origin bottom-left, at the capture size (1280x720 unless the recipe says otherwise).
- Leave edit mode as you found it. Do not save scenes changed during play.

## Proof and skip reporting

- Capture the action and the resulting state, not only the final screen.
- Every proof has a screenshot from `vu.sh shot`, the console at warn level, and a read-only `eval` or `input-read` value.
- Record the feature ID and entry point with every artifact name (`<feature-id>-<step>.png`).
- Report an unreachable path with the attempted command and the unmet precondition.
- Do not report a skipped entry point as verified through a different path.

## Feature entry contract

Each feature file starts with an H1 title and one paragraph describing the player-visible behavior. It then uses exactly four H2 sections in this order. `Sub-features`, `How to get to it (user POV)`, `Driving it with vu.sh`, `Gotchas`.

## Features

- [Play the start scene](./play-start-scene.md) covers entering play mode in the background, the rendered frame, and a clean console.
- [Background input](./background-input.md) covers keyboard and pointer input reaching the game while the Editor is unfocused.
- [Editor tests](./editor-tests.md) covers running EditMode and PlayMode tests in the open Editor.
