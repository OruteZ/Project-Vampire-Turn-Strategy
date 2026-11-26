# Undo move

On the player turn, `Z` or the action bar's sixth slot (refresh icon, key label "Z") takes back the latest ally move: the unit is put back on its previous hex at once, selected, with its move, triggers and log as before (docs/ui/battle-hud.md 3-8). Moves undo newest first. An attack, skill, wait or end turn clears them; a move that ended the battle (escort arrived) cannot be undone.

## Sub-features

- One move undone: unit back, `Moved` false, slot dim.
- Several moves: each `Z` takes back one, latest first.
- Nothing to undo: slot dim, `Z` does nothing.

## How to get to it (user POV)

Start a battle, click an ally, click a move hex, press `Z`.

## Driving it with vu.sh

Chapter 1 (stage index 0) sample formation: spearman id 8 at 2,6 can step to 2,5.

```bash
$V play
$V eval 'VampireSpawn.Game.Screens.App.GoBattle(System.Linq.Enumerable.ElementAt(VampireSpawn.Core.Campaign.Stages, 0), false, System.Linq.Enumerable.ElementAt(VampireSpawn.Core.Campaign.Stages, 0).Sample, false)'   # setup
$V key Space down; sleep 0.3; $V key Space up                       # start the battle
$V eval 'VampireSpawn.Editor.UiAudit.HexOnScreen(2,6)'              # then 2,5
$V click <x> <y> down; sleep 0.3; $V click <x> <y> up               # select, then the same for the move hex
$V key Z down; sleep 0.3; $V key Z up
$V eval 'UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.Screens.BattleFlow>().input.Session.Battle.Find(8).Pos + ""'
$V shot undo-move
```

## Gotchas

- `Z` is read from `isPressed` edges, so send `down` … `up`.
- The rule has EditMode tests: `$V test editor UndoMoveTests`.
