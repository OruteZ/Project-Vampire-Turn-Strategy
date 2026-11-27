# Restart battle

During a campaign battle (not deploy, not the demo, not after it ends), the bottom-left corner row has a mirrored circular-arrow button after leave. The first press turns it into a red "처음부터 다시?" / "Restart battle?" button for 2.5 s; a second press restarts the battle with the same formation from turn one, like the result window's retry (docs/ui/battle-hud.md 6-2). Arming restart disarms leave and the other way round.

## Sub-features

- Press once: armed, runs out after 2.5 s.
- Press twice: turn 1, moon 0, the start formation, no rule cards.
- Leave armed, then restart: only restart is armed.

## How to get to it (user POV)

Start a campaign battle, press the corner's third button twice.

## Driving it with vu.sh

At 1920x1080 the corner buttons sit at about x 17 (rules), 41 (leave), 65 (restart), y 22 (origin bottom-left).

```bash
$V play
$V eval 'return VampireSpawn.Editor.UiAudit.Resize(1920,1080);'
$V eval 'VampireSpawn.Game.Screens.App.GoBattle(System.Linq.Enumerable.ElementAt(VampireSpawn.Core.Campaign.Stages, 0), false, System.Linq.Enumerable.ElementAt(VampireSpawn.Core.Campaign.Stages, 0).Sample, false); return 1;'   # setup
$V key Escape down; sleep 0.3; $V key Escape up      # close rule cards
$V key Space down; sleep 0.3; $V key Space up        # start, then once more to reach turn 2
$V click 65 22; $V shot restart-armed 1920 1080      # shoot at once, the arm lasts 2.5 s
$V click 65 22; $V click 65 22
$V eval 'UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.Screens.BattleFlow>().input.Session.Battle.Turn + ""'   # 1
```

## Gotchas

- Each `vu.sh` call takes about a second, so arm and confirm in back-to-back calls.
- The arm model has EditMode tests: `$V test editor RestartTests`.
- Rule cards write `seen` to the shared save; back up `pvs-campaign-v2` first (rule-cards.md).
