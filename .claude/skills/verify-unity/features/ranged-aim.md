# Ranged aim arrow

With a ranged ally (range 2 or more) selected on the player turn, hovering an enemy draws a curved arrow from the ally to that enemy (docs/art/ui-style.md 8-6). If the shot cannot be taken from where the ally stands (out of range, a `#` wall on the line, or the enemy is asleep), the arrow is dim and carries a red X at its middle. Melee allies, empty cells and allies show no arrow.

## Sub-features

- Clear shot: `enemy` arrow, no X, damage preview on the target.
- Blocked shot: dim arrow with X (wall, range, sleeping vampire).
- No arrow: melee ally selected, or execute/mend/trap mode, or the ally already acted.

## How to get to it (user POV)

Start a battle, select an archer, move the pointer over an enemy.

## Driving it with vu.sh

Stage index 3 (chapter 4) with its sample formation covers every case at the first player turn: archer at 4/9 → skeleton 3/6 is clear, peasant archer at 1/8 → skeleton 3/6 is wall-blocked, archer → the sleeping vampire is blocked, sword at 3/8 shows nothing.

```bash
$V play
$V eval 'VampireSpawn.Game.Screens.App.GoBattle(System.Linq.Enumerable.ElementAt(VampireSpawn.Core.Campaign.Stages, 3), false, System.Linq.Enumerable.ElementAt(VampireSpawn.Core.Campaign.Stages, 3).Sample, true)'   # setup
$V eval 'VampireSpawn.Editor.UiAudit.HexOnScreen(4,9)'        # "move x y" for a hex
$V click <x> <y> down; sleep 0.3; $V click <x> <y> up             # select the archer
$V click <x> <y> move                                              # hover the enemy
$V eval 'UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.Screens.BattleFlow>().input.overlay.AimShown + " " + UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.Screens.BattleFlow>().input.overlay.AimCrossed'
$V shot ranged-aim-clear
```

## Gotchas

- Board clicks read `isPressed` edges, so select with `down` … `up`, not a one-frame `click`.
- The arc bulges across the chord on screen, so it is rebuilt every frame in `OverlayView.LateUpdate`; a screenshot right after a camera turn already shows the new shape.
- The rule (`BattleSession.AimAt`) has EditMode tests: `$V test editor AimTests`.
