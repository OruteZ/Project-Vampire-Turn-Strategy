# Unit detail window

The player opens one fixed window that shows a unit in full: stats, state chips (hover for the description), the spawn it would rise as, skills with their text, and its upgrades. The same window opens in battle, deploy, the stage info roster and the barracks, and it is read-only (docs/ui/unit-detail.md).

## Sub-features

- Battle and deploy: the I key on the hovered, selected or inspected unit; the info button on a unit panel that stays (selected ally, inspected unit).
- Deploy roster: the info button on a roster row, or I while the pointer is on the row; left and right walk the stage's unit types.
- Camp: a click on a roster chip in the stage info panel; the info button or I in the barracks, where left and right also move the tab.
- Closing: Esc, I, the close button, a click on the dark veil. While it is open the board, hotkeys and battle camera keys do nothing.

## How to get to it (user POV)

Start a campaign stage or the demo battle. In deploy, press I: the picked unit type opens. Start the battle, select an ally and press I, or hover an enemy and press I. In camp, open a stage and click a unit chip, or open the barracks and press I.

## Driving it with vu.sh

```bash
$V play
$V eval 'VampireSpawn.Game.Screens.App.GoBattle(VampireSpawn.Core.Campaign.Stages[0], false); return 1;'   # setup
sleep 8
$V key I down; sleep 0.3; $V key I up        # keys are polled as edges: hold, then release
$V eval 'return VampireSpawn.Game.Screens.UnitDetail.IsOpen;'   # True
$V shot unit-detail-kind
$V key Escape down; sleep 0.3; $V key Escape up
$V eval 'return VampireSpawn.Game.Screens.UnitDetail.IsOpen;'   # False
```

For a unit on the board, hover it first with `$V click <x> <y> move` (coordinates from `VampireSpawn.Editor.UiAudit.HexOnScreen(col, row)`), then press I. `tools/ui-audit/sweep.sh` opens the three variants at every aspect ratio (`ONLY=detail`).

## Gotchas

- The I, Esc and arrow keys are read as `isPressed` edges, so a one-frame `$V key I` is not seen. Send `down`, wait, then `up`.
- A hovered unit's panel disappears when the pointer leaves its hex, so its info button cannot be clicked; only I opens a hovered enemy.
- The window does not open during enemy playback, over the result or rules window, or while the purchase dialog is up.
- The tooltip of a state chip opens after 0.5 s of hover and is not covered by the layout audit.
