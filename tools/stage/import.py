# Writes scratch/import.cs: a Unity eval script that pushes tools/stage/data.json into the data assets
# (rules, unit stats, upgrade numbers, minions, stages; new minions and stages are created by copying an existing asset).
# After running it, export.cs must reproduce data.json byte for byte. Looks (models) and texts are not handled here.
#   py -3 tools/stage/import.py && unity command --project-path <tree> script eval --file tools/stage/scratch/import.cs
import json, os
T = os.path.dirname(os.path.abspath(__file__))
d = json.load(open(os.path.join(T, 'data.json'), encoding='utf8'))
NS = 'VampireSpawn.Game.Data.'
q = lambda s: 'null' if s is None else '"' + s + '"'
V2 = lambda h: f'new UnityEngine.Vector2Int({h["Col"]},{h["Row"]})'
LV = lambda hs: 'new System.Collections.Generic.List<UnityEngine.Vector2Int>{' + ','.join(V2(h) for h in hs) + '}'
L = ['using System.Linq; using UnityEditor; using VampireSpawn.Game.Data;',
     'var gd = AssetDatabase.LoadAssetAtPath<GameDataAsset>("Assets/_Project/Data/GameData.asset"); var log = "";',
     'T Asset<T>(string path, string copyFrom) where T : UnityEngine.Object { if (AssetDatabase.LoadAssetAtPath<T>(path) == null) { AssetDatabase.CopyAsset(copyFrom, path); log += "new " + path + "\\n"; } return AssetDatabase.LoadAssetAtPath<T>(path); }']
r = d['Rules']
L.append(f'gd.rules.drainHeal = {r["DrainHeal"]}; gd.rules.moonMax = {r["MoonMax"]}; gd.rules.eatRange = {r["EatRange"]}; gd.rules.moonWake = {r["MoonWake"]}; gd.rules.mendRange = {r["MendRange"]}; EditorUtility.SetDirty(gd.rules);')
for u in d['Types']:
    L.append(f'{{ var u = gd.units.First(x => x.key == "{u["Key"]}"); u.pop = {u["Pop"]}; u.silver = {u["Silver"]}; u.hp = {u["Hp"]}; u.atk = {u["Atk"]}; u.def = {u["Def"]}; u.mov = {u["Mov"]}; u.rng = {u["Rng"]}; u.bane = {u["Bane"]}; u.heal = {u["Heal"]}; u.unlockAt = {u["UnlockAt"]};')
    for p in [x for x in d['Upgrades'] if x['Unit'] == u['Key']]:
        m = p['Mod']
        L.append(f'  {{ var p = u.upgrades.First(x => x.id == "{p["Id"]}"); p.cost = {p["Cost"]}; p.hp = {m["Hp"]}; p.atk = {m["Atk"]}; p.def = {m["Def"]}; p.mov = {m["Mov"]}; p.rng = {m["Rng"]}; p.bane = {m["Bane"]}; p.heal = {m["Heal"]}; }}')
    L.append('  EditorUtility.SetDirty(u); }')
for m in d['Minions']:
    name = m['Key'][0].upper() + m['Key'][1:]
    L.append(f'{{ var m = gd.minions.FirstOrDefault(x => x.key == "{m["Key"]}") ?? Asset<EnemyData>("Assets/_Project/Data/Enemies/{name}.asset", "Assets/_Project/Data/Enemies/Skeleton.asset"); if (!gd.minions.Contains(m)) gd.minions.Add(m);')
    L.append(f'  m.key = "{m["Key"]}"; m.hp = {m["Hp"]}; m.atk = {m["Atk"]}; m.def = {m["Def"]}; m.mov = {m["Mov"]}; m.rng = {m["Rng"]}; EditorUtility.SetDirty(m); }}')
for i, s in enumerate(d['Stages']):
    L.append(f'{{ var s = gd.stages.FirstOrDefault(x => x != null && x.id == "{s["Id"]}") ?? Asset<StageData>("Assets/_Project/Data/Stages/{i:02d}_{s["Id"]}.asset", "Assets/_Project/Data/Stages/10_s10.asset"); if (!gd.stages.Contains(s)) gd.stages.Add(s);')
    L.append(f'  s.id = "{s["Id"]}"; s.units = new System.Collections.Generic.List<string>{{{",".join(q(x) for x in s["Units"])}}}; s.pop = {s["Pop"]}; s.silver = {s["Silver"]}; s.reward = {s["Reward"]};')
    L.append(f'  s.goal = (VampireSpawn.Core.Goal){s["Goal"]}; s.spots = {LV(s["Spots"])}; s.hold = {s["Hold"]};')
    e = s['Escort']
    if e: L.append(f'  s.escortHp = {e["Hp"]}; s.escortDef = {e["Def"]}; s.escortMov = {e["Mov"]}; s.escortAt = {V2(e["Start"])};')
    b, k = s['Bonus'], s['Boss']
    L.append(f'  s.bonusType = (VampireSpawn.Core.BonusType){b["Type"]}; s.bonusValue = {b["Value"]}; s.bonusGold = {b["Gold"]}; s.bossHp = {k["Hp"]}; s.bossAtk = {k["Atk"]}; s.bossDef = {k["Def"]}; s.bossMov = {k["Mov"]}; s.bossHeal = {k["Heal"]};')
    L.append('  s.map = new System.Collections.Generic.List<string>{' + ','.join(q(x) for x in s['Map']) + '};')
    L.append(f'  s.enemies = new System.Collections.Generic.List<EnemySpot>{{' + ','.join(f'new EnemySpot{{kind="{x["Kind"]}",col={x["Col"]},row={x["Row"]},arrive={x["Arrive"]},wave={q(x["Wave"])},stance=(VampireSpawn.Core.Stance){x["Stance"]},alert={x["Alert"]}}}' for x in s['Enemies']) + '};')
    L.append(f'  s.sample = new System.Collections.Generic.List<UnitSpot>{{' + ','.join(f'new UnitSpot{{kind="{x["Kind"]}",col={x["Col"]},row={x["Row"]}}}' for x in s['Sample']) + '};')
    L.append(f'  s.triggers = new System.Collections.Generic.List<TriggerSpot>{{' + ','.join(f'new TriggerSpot{{id="{x["Id"]}",kind=(VampireSpawn.Core.TriggerKind){x["Kind"]},value={x["Value"]},hexes={LV(x["Hexes"])}}}' for x in s['Triggers']) + '};')
    L.append(f'  s.doors = new System.Collections.Generic.List<DoorSpot>{{' + ','.join(f'new DoorSpot{{hexes={LV(x["Hexes"])},trigger="{x["Trigger"]}"}}' for x in s['Doors']) + '};')
    L.append(f'  s.reinforcements = new System.Collections.Generic.List<ReinforceSpot>{{' + ','.join(f'new ReinforceSpot{{trigger="{x["Trigger"]}",pop={x["Pop"]},silver={x["Silver"]},hexes={LV(x["Hexes"])}}}' for x in s['Reinforcements']) + '};')
    L.append('  EditorUtility.SetDirty(s); }')
ids = ','.join(q(s['Id']) for s in d['Stages'])
L.append(f'{{ var order = new[]{{{ids}}}; gd.stages = order.Select(id => gd.stages.First(x => x.id == id)).ToList(); }}')
L.append('EditorUtility.SetDirty(gd); AssetDatabase.SaveAssets(); VampireSpawn.Core.GameData.Use(null);')
L.append('var v = VampireSpawn.Core.GameData.Current.Validate(); return log + (v.Count == 0 ? "valid" : string.Join("\\n", v));')
os.makedirs(os.path.join(T, 'scratch'), exist_ok=True)
open(os.path.join(T, 'scratch', 'import.cs'), 'w', encoding='utf8').write('\n'.join(L))
print('ok', len(d['Stages']), 'stages')
