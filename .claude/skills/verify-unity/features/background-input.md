# Background input

A player's keyboard presses and mouse moves and clicks reach the game. Under verification they are simulated through the Input System while the Editor stays unfocused.

## Sub-features

- `input-key` holds and releases a key.
- `input-pointer-move` moves the pointer to a game-view position.
- `input-pointer-click` presses and releases the left button.

## How to get to it (user POV)

- Press a key or move and click the mouse over the game view.

## Driving it with vu.sh

Preconditions:

- Play mode is running through `$V play`.
- `$V input-probe` has run once in this play session. It prints `probe installed`.

- **Key down.** Run `$V key Space down`, then `$V input-read`. Prints `space=True`.
- **Key up.** Run `$V key Space up`, then `$V input-read`. Prints `space=False`.
- **Pointer move.** Run `$V click 321 181 move`, then `$V input-read`. Prints `pos=(321.00, 181.00)`.
- **Pointer press.** Run `$V click 50 60 down`, then `$V input-read`. Prints `pos=(50.00, 60.00) left=True`.
- **Pointer release.** Run `$V click 50 60 up`, then `$V input-read`. Prints `left=False`.
- **Proof.** Run `$V shot input-after` and `$V console warn 20`. The console has no new errors.

## Gotchas

- With the Editor unfocused, the Input System default (`ResetAndDisableNonBackgroundDevices`) disables the mouse and keyboard, and every simulated event is dropped while the CLI still prints `true`. `$V play` switches play mode to `IgnoreFocus` and re-enables devices. Play mode resets that setting, so a play started outside `$V play` needs `$V play` again.

- `$V eval 'UnityEngine.InputSystem.Mouse.current.position.ReadValue()'` prints `(0.00, 0.00)` even when the game received the move, because eval reads the Editor's input state. Only `input-read` or game state shows what the game saw.
- The probe lives only for one play session. Run `input-probe` again after `$V stop` and `$V play`.
- `runtime simulate_key --action press` sends down and up in one frame, so game code that watches `isPressed` never sees it. Send `down`, wait about 0.3 s, then `up`.
- `runtime simulate_pointer --action move` sends a mouse state with no buttons held, so a move while the button is down releases it and a drag cannot be simulated with the CLI. Queue the states instead: `$V eval 'UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current, new UnityEngine.InputSystem.LowLevel.MouseState { position = new UnityEngine.Vector2(x, y), buttons = 1 }); 1'` for each point (buttons 1 while held, 0 to release), about 0.25 s apart.
- uGUI buttons take `$V click x y down`, a 0.3 s wait, then `$V click x y up` (P9). Find the centre from the button's `RectTransform.GetWorldCorners`: on a Screen Space Overlay canvas the corners are already game-view pixels, origin bottom-left.
- `script eval` returns the value of the last expression only. An `if ... else` block as the last statement returns nothing, so end with a ternary.
