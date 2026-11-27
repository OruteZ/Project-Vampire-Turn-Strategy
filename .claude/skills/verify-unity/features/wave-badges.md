# Reinforcement badges

Every enemy reinforcement that is not on the board yet carries a small badge on its cell, from deploy on, without hovering: a skull and the ally turns left before it comes (this turn included), a star when it waits on the marked hexes, "done/needed" when it waits on kills. At 1 the badge fills red. A shut door carries the same badge with a lock, an unopened ally reinforcement zone one with an up arrow (battle only). The deploy screen's brief lists the waves as chips (docs/art/ui-style.md 8-7, docs/ui/battle-hud.md 2-4, docs/systems/waves.md 4).

## Sub-features

- Deploy: badges on every waiting cell and door, one chip per wave in the brief (top right).
- Battle: the numbers drop by one each turn; ally zone badges appear.
- Hover a waiting cell: its card's tag reads "남은 턴 n" / "다음 적 차례", and the badges of the same wave light up.

## How to get to it (user POV)

Open a stage with reinforcements (9장 has turn waves, a reach trigger with a door, and a kills trigger for allies) and look at the red dashed cells.

## Driving it with vu.sh

```bash
$V play
$V eval 'return VampireSpawn.Editor.UiAudit.Resize(1920,1080);'
$V eval 'VampireSpawn.Game.Screens.App.GoBattle(System.Linq.Enumerable.First(VampireSpawn.Core.Campaign.Stages, s => s.Id == "s9"), false); return 1;'   # setup
BF='var bf=UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.Screens.BattleFlow>();'
$V eval "$BF bf.ShowCards(new string[0]); return 1;"     # setup: no rule cards over the board
$V shot wave-deploy 1920 1080
$V eval "$BF return string.Join(\" \", System.Linq.Enumerable.Select(VampireSpawn.Game.Screens.BattleScreen.Waves(bf.input.Session.Battle), w => w.Key + \"=\" + w.When + w.Left + \"x\" + w.Who.Count));"
# turn 4=Turn4x2 turn... wave late=Turn7x2 wave deep=Reach0x3
$V eval "$BF bf.input.Run(s=>s.UseSample()); bf.input.Run(s=>s.Start()); return 1;"   # setup
$V shot wave-turn1 1920 1080
```

The sweep's `8a-wave` and `8b-arrival` steps show the same screens at every aspect ratio.

## Gotchas

- The model is `BattleScreen.When`/`Waves`; `$V test editor WavesTerrainTests` covers the counting.
- Badges follow their cell every frame (`BattleHud.Tick`), so they are hidden until the first tick after a rebuild.
- The floor skull icon on these cells is gone on purpose: the badge carries it.
