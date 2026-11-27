# Barracks studio

Opening the barracks (L3) leaves the expedition map: the picked tab's unit stands large on a round stand in a dark set lit by a cool key light and a warm rim light, about 59% of the screen's height, centred in the area left of the barracks panel. The camera cuts there under a black cover that clears in 0.2 s. Dragging the empty area with the left button turns the unit (docs/ui/upgrade.md 9).

## Sub-features

- Enter and leave: a cut with a 0.2 s black cover, the map's lights and fog back on leaving.
- Tab change: only the unit changes, facing its first direction again.
- Drag off the UI: the unit turns 0.4 degrees per screen px (at 1920 wide) and stays there.
- A hidden (locked) tab shows the empty stand.

## How to get to it (user POV)

On the camp screen press B or the barracks button, then drag anywhere left of the panel.

## Driving it with vu.sh

```bash
$V play
$V eval 'return VampireSpawn.Editor.UiAudit.Resize(1920,1080);'
SIZES=1920x1080 ONLY='2e-camp-l3' bash tools/ui-audit/sweep.sh        # opens L3 and shoots it; stops play at its end
```

Driven by hand (the camp reads `isPressed` edges, so hold keys and the button about 0.3 s):

```bash
$V key B down; sleep 0.3; $V key B up
$V shot studio 1920 1080
$V click 500 500 down; sleep 0.2; $V click 800 500 move; sleep 0.2; $V click 800 500 up
$V eval 'UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.CampaignMapView>().transform.Find("Studio/Unit -1").eulerAngles.y + ""'   # no longer 0
$V shot studio-turned 1920 1080
```

## Gotchas

- The set is built at run time 2000 units under the map layer (`CampaignMapView.BuildStudio`); nothing of it is in the Camp scene.
- In the studio the World scene's directional lights are off, the fog is the dark's colour and the ambient light is flat; leaving puts the World scene's values back. A studio screenshot with sky or a horizon line means `CampaignMapView.Studio` did not run; a map that stays dark after leaving means the restore did not.
- The scene still has a `Shot Camp` camera and a `Camp/Unit` point; the barracks no longer uses them.
