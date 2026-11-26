# Battle HUD size

The main menu's settings window has a "전투 HUD 크기" row: 작게 0.55 (default), 보통 0.7, 크게 0.85. The value is saved in PlayerPrefs (`battle.hudZoom`) and the next battle draws its whole HUD at that scale (`BattleFlow.HudZoom`, docs/ui/settings.md 7-1, docs/ui/battle-hud.md 1). The picked ally's numbers sit in the left of the action bar (battle-hud.md 3-4 8).

## Sub-features

- Picking a size highlights its chip and saves it.
- A battle started after that uses the new scale; the board frames into the smaller space left.
- All three sizes pass the layout sweep on battle screens.

## How to get to it (user POV)

Main menu, "설정" top right, "전투 HUD 크기" row, pick 크게, close, start any battle.

## Driving it with vu.sh

```bash
$V play
$V eval 'return VampireSpawn.Editor.UiAudit.Resize(1920,1080);'
# open the window: click "설정" top right (about x 1740, y 1040 at 1920x1080, origin bottom-left)
$V click 1200 444                                       # 크게 chip at 1920x1080
$V eval 'return VampireSpawn.Game.GameSettings.HudZoom;' # 0.85
$V eval 'VampireSpawn.Game.Screens.App.GoBattle(VampireSpawn.Core.Campaign.Stages[0], false); return 1;'   # setup
$V shot hud-large
$V eval 'VampireSpawn.Game.GameSettings.HudZoom = 0.55f; return 1;'   # put the default back
```

Sweep one size on battle screens: set the value with the eval above, then `ONLY='3-deploy|4|5-|6-|7-|8' VU_RUN=<id> bash tools/ui-audit/sweep.sh`.

## Gotchas

- PlayerPrefs outlive play mode and are shared by every run on this machine. Put 0.55 back after a test.
- The scale is read when the Battle scene starts, so change it before `GoBattle`.
