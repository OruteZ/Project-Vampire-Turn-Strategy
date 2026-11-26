# Rule cards and rules window

A campaign battle's deploy opens with one card per thing the stage is the first to bring (docs/ui/rule-cards.md): stage 1 shows Turning, Light and Moon; later stages show the unit type unlocked there, a new enemy kind, a new goal kind. Click, Space or Enter goes to the next card, Esc closes them all. Seen cards are kept in the campaign save (`seen`) and never show again. The `?` button opens the rules window as a list of ten terms (battle-hud.md 6-1).

## Sub-features

- Cards on a first visit, none on a retry or in the demo.
- Space, Enter and clicks advance; Esc closes all; Space does not start the battle while a card is up.
- The rules window term list.

## How to get to it (user POV)

From the camp, march into a stage whose cards you have not seen. Press `?` in the battle's bottom-left corner for the rules window.

## Driving it with vu.sh

The editor save is shared with the user's play: back it up before and write it back after (`PlayerPrefs` key `pvs-campaign-v2`).

```bash
$V eval 'UnityEngine.PlayerPrefs.GetString("pvs-campaign-v2")'          # back up
$V play
$V eval 'VampireSpawn.Game.Screens.App.GoBattle(System.Linq.Enumerable.ElementAt(VampireSpawn.Core.Campaign.Stages, 0), false)'
$V eval 'UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.Screens.BattleFlow>().CardOpen'   # true if unseen
$V key Space down; sleep 0.3; $V key Space up                            # next card
$V key Escape down; sleep 0.3; $V key Escape up                          # close all
$V eval 'string.Join(",", VampireSpawn.Game.Screens.App.Campaign.SeenRules)'
$V eval 'UnityEngine.Object.FindFirstObjectByType<VampireSpawn.Game.Screens.BattleFlow>().ShowCards(VampireSpawn.Core.Campaign.NewRules(3))'   # setup: any stage's cards
```

## Gotchas

- Keys are read from `isPressed` edges, so send `down` … `up`.
- `ShowCards` is setup, not proof; it does not check the save.
- The rules have EditMode tests: `$V test editor RuleCardTests`.
