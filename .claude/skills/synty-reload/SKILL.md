---
name: synty-reload
description: Re-import the paid Synty packages this repo needs from UPackage/ and re-bake every Sidekick character recipe, then prove the result with the asset tests. Run by hand with /synty-reload after a fresh clone, after adding a .unitypackage, or after adding or changing a Sidekick recipe (.sk).
disable-model-invocation: true
---

# /synty-reload

Synty assets are paid and never go into git (CLAUDE.md). Characters made with the Sidekick tool are paid-asset derivatives too, so the repo keeps only their **recipes** (`Assets/_Project/Art/Characters/<Name>.sk`, part names, colours and body sliders, no meshes) and every clone bakes them again. This skill runs that whole pipeline. Teammates without the Unity CLI run the same pipeline from the Editor: VampireSpawn > 도구 모음 (Tools) > 유료 에셋 > "Synty 다시 불러오기" (`Assets/_Project/Scripts/Editor/SyntyReload.cs`). Design: `docs/art/unit-visuals.md` 3. Guide for people: `docs/guides/asset-setup.md`.

## What it touches

| Path | In git | Made by |
|---|---|---|
| `UPackage/*.unitypackage` | no | the user, from their Synty account |
| `Assets/Synty/` | no | step 2 (import) |
| `Assets/_Project/Art/Characters/*.sk` | **yes** | the user, Sidekick tool "Save Character" |
| `Assets/_Generated/Resources/Characters/<Name>/` | no | step 3 (bake): prefab, mesh, material, colour texture, `recipe.sha1` |
| `Assets/_Project/Art/Units/*.asset` (`UnitLook`) | **yes** | the user. `recipe` names the baked character, `model` is the fallback for an unbaked clone |

`UnitLook` loads a baked character by recipe name through `Resources`, never by asset reference, because a fresh bake gets new GUIDs and fileIDs.

## Steps

Run from the repo root (or the worktree) in Git Bash. Follow CLAUDE.md for the Editor: Unity CLI only, never close a user's Editor.

1. **Editor.** `bash .claude/skills/verify-unity/scripts/vu.sh launch` (a worktree sets `VU_PROJECT=<worktree>` and junctions `Assets/Synty` and `UPackage` from the main tree first, per the verify-unity skill). Then `vu.sh stop` so play mode is off.
2. **Import what is missing.** `bash tools/assets/setup.sh`. It imports only packages that hold a required asset (`node tools/assets/check.mjs` decides), bakes recipes, and runs `RequiredAssetsTests`. If the user added a package that no asset references yet, import it directly: `bash tools/import-unitypackage.sh UPackage/<file>.unitypackage`, then list its folder in `docs/areas/art-assets.md`.
3. **Bake recipes.** `bash tools/assets/sidekick-bake.sh` bakes every recipe whose `recipe.sha1` is stale or missing. `bash tools/assets/sidekick-bake.sh <Name>` bakes one. `SIDEKICK_BAKE_FORCE=1` rebakes all. A line with `FAIL` or `MISSING <part>` means the recipe names a part this Synty install lacks: report the part names and which Sidekick pack they come from.
4. **Refresh the asset list** if any Synty reference changed: `node tools/assets/scan.mjs`, and commit `tools/assets/*.tsv` with the change.
5. **Prove it.**
   - `bash .claude/skills/verify-unity/scripts/vu.sh test editor` must pass, in particular `SidekickRecipeTests` (every recipe baked and up to date, every `UnitLook` resolves) and `RequiredAssetsTests`.
   - For a new or changed character, look at it in the game: start the demo battle with the verify-unity skill and screenshot the unit (`verify-artifacts/`, path in the task comment).
6. **Report** what was imported, what was baked (name and part count), any missing parts, and the test summary.

## Adding a character made with Sidekick

1. In the Sidekick tool, Save Character to `Assets/_Project/Art/Characters/<Name>.sk`. Use a plain ASCII name; it becomes the folder and prefab name.
2. In that unit's `Assets/_Project/Art/Units/<Unit>.asset`, set `recipe` to `<Name>`. Keep `model` as the Synty preset it used before: it is what an unbaked clone shows.
3. Run /synty-reload. Weapons stay on `prop_r`/`prop_l`, so grips carry over. Fix a grip offset in the `UnitLook` if needed.
4. Commit the `.sk`, the `UnitLook`, and `tools/assets/*.tsv` if changed. Never commit `Assets/_Generated/`.

## Gotchas

- The bake code is `tools/assets/SidekickBaker.cs`. It references Synty assemblies, so it is not a project script: `SyntyReload.InstallBaker` copies it to `Assets/Synty/_VampireSpawn/Editor/` (gitignored, exists only with Synty), Unity compiles it there, and `SyntyReload.Bake` calls it by name. Edit the file in `tools/assets/`, never the copy. Both the Tools button and `sidekick-bake.sh` go through `SyntyReload`.
- `script eval` runs inside the Editor process, so shell environment variables do not reach it. `sidekick-bake.sh` passes the name and force flag as call arguments.
- The Tools button reports in the Tools window and the Console, never in a modal dialog, so it also works in a background Editor driven by the CLI.
- The Sidekick part database (`Side_Kick_Data.db`) and the tool's own files are not in the SIDEKICK packages: the SIDEKICK scripts download them to `Assets/DownloadCache/SidekicksDatabase.unitypackage` and `Sidekicks.unitypackage`. If the bake fails with "Could not open database file" or "tool data", re-import those two (`bash tools/import-unitypackage.sh "$PWD/Assets/DownloadCache/<name>.unitypackage"`). `check.mjs` does not see these files.
- Only the colour map is written per character. Metallic and the other maps stay the base material's unless a recipe row sets them.
- `Assets/_Project/Art/Characters/SampleKnight.sk` is a pipeline sample (generated from Sidekick parts, not used by any unit). It keeps `SidekickRecipeTests` exercising a real bake. Delete it once a real recipe exists.
