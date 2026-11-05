using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Data
{
    [Serializable]
    public sealed class UpgradeEntry
    {
        [Tooltip("캠페인 저장이 이 id 로 산 강화를 기억한다. 바꾸면 이미 산 강화가 사라진다")] public string id;
        public string displayName;
        [TextArea(1, 3)] public string desc;
        public int cost;
        [Tooltip("스폰이 되었을 때 물려받는지 (카드 표시)")] public bool inherit;
        public int hp, atk, def, mov, rng, bane;
        public Ability ability;
    }


    public sealed class UnitTypeData : ScriptableObject
    {
        public UnitKind kind;
        [Tooltip("코드와 저장에 쓰는 이름. 바꾸지 않는다")] public string key;
        public string displayName;
        public string glyph;
        public Tier tier;
        public int pop, silver, hp, atk, def, mov, rng;
        [Tooltip("사냥꾼 은탄처럼 언데드에게 더하는 공격")] public int bane;
        [Tooltip("몇 장에서 처음 쓸 수 있는지. 이 장을 클리어하면 병영 강화가 열린다")] public int unlockAt = 1;
        [TextArea(2, 4)] public string desc;
        public List<UpgradeEntry> upgrades = new List<UpgradeEntry>();

        public UnitType ToCore() => new UnitType(kind, key, displayName, glyph, tier, pop, silver, hp, atk, def, mov, rng, bane, unlockAt, desc);

        public IEnumerable<Upgrade> UpgradesToCore() =>
            upgrades.Select(u => new Upgrade(kind, u.id, u.displayName, u.desc, u.cost, u.inherit, new StatMod(u.hp, u.atk, u.def, u.mov, u.rng, u.bane), u.ability));

        public void Fill(UnitType t, IEnumerable<Upgrade> ups)
        {
            kind = t.Kind; key = t.Key; displayName = t.Name; glyph = t.Glyph; tier = t.Tier; pop = t.Pop; silver = t.Silver;
            hp = t.Hp; atk = t.Atk; def = t.Def; mov = t.Mov; rng = t.Rng; bane = t.Bane; unlockAt = t.UnlockAt; desc = t.Desc;
            upgrades = ups.Where(u => u.Unit == t.Kind).Select(u => new UpgradeEntry
            {
                id = u.Id, displayName = u.Name, desc = u.Desc, cost = u.Cost, inherit = u.Inherit,
                hp = u.Mod.Hp, atk = u.Mod.Atk, def = u.Mod.Def, mov = u.Mod.Mov, rng = u.Mod.Rng, bane = u.Mod.Bane, ability = u.Ability,
            }).ToList();
        }
    }
}
