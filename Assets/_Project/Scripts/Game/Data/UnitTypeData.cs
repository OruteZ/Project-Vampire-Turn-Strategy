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
        public int cost;
        public int hp, atk, def, mov, rng, bane, heal;
        public Ability ability;
    }

    public sealed class UnitTypeData : ScriptableObject
    {
        [Tooltip("코드·저장·장 에셋이 병종을 이 이름으로 찾는다. 바꾸지 않는다")] public string key;
        public Tier tier;
        public int pop, silver, hp, atk, def, mov, rng;
        [Tooltip("사냥꾼 은탄처럼 언데드에게 더하는 공격")] public int bane;
        [Tooltip("치료 기술이 되돌리는 체력")] public int heal;
        [Tooltip("강화 없이 처음부터 가진 기술")] public List<Ability> skills = new List<Ability>();
        [Tooltip("몇 장에서 처음 쓸 수 있는지. 이 장을 클리어하면 병영 강화가 열린다")] public int unlockAt = 1;
        [Tooltip("갈래 순서 = 목록 순서. 셋 중 하나만 산다 (docs/systems/progression.md)")] public List<UpgradeEntry> upgrades = new List<UpgradeEntry>();

        public UnitType ToCore() => new UnitType(key, tier, pop, silver, hp, atk, def, mov, rng, bane, heal, unlockAt, skills.ToArray());

        public IEnumerable<Upgrade> UpgradesToCore() =>
            upgrades.Select(u => new Upgrade(key, u.id, u.cost, new StatMod(u.hp, u.atk, u.def, u.mov, u.rng, u.bane, u.heal), u.ability));
    }
}
