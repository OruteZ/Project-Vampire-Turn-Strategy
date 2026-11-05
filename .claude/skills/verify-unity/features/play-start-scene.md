# Play the start scene

A player who starts the game sees the Build Settings scene 0 render and run. Frames keep advancing even when the Editor window is behind other windows.

## Sub-features

- `play-enter` enters play mode from edit mode.
- `play-frames` keeps advancing frames while the Editor is unfocused.
- `play-render` shows the scene in the game view.
- `play-clean` produces no errors or warnings on start.
- `play-exit` returns to edit mode.

## How to get to it (user POV)

- Start the game (Editor play button, here `vu.sh play`).

## Driving it with vu.sh

Preconditions:

- `vu.sh doctor` prints `OK`.
- The open scene is Build Settings scene 0. Check with `unity --no-banner command --project-path "<path>" scene list_open`.

- **Enter.** Run `$V play`. Prints `PLAYING frames/s=<n>` with n of 20 or more.
- **Frames advance.** Run `$V eval 'UnityEngine.Time.frameCount'` twice two seconds apart. The second value is larger by 40 or more.
- **Render.** Run `$V shot play-render`. The PNG shows the scene, not a grey or black frame.
- **Scene.** Run `$V eval 'UnityEngine.SceneManagement.SceneManager.GetActiveScene().path'`. Prints the scene 0 path.
- **Clean.** Run `$V console warn 50`. `counts.error` and `counts.warn` are 0.
- **Exit.** Run `$V stop`. `unity --no-banner command --project-path "<path>" editor status --json` shows `"playMode":"stopped"`.

## Gotchas

- Without `vu.sh play` the background override is off and the game runs about 1 frame per second. Starting play with the raw CLI command skips it.
- Play mode starts whatever scene is open, not Build Settings 0. Open scene 0 first.
