#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // Editor-only (Tools window > 디버그). Edits go through the save format so Campaign keeps its own invariants.
    public static class DebugCheats
    {
        public const int NoCap = 999;

        public static Campaign With(Campaign c, int? gold = null, IEnumerable<string> cleared = null, IEnumerable<string> bonus = null, IEnumerable<string> ups = null) =>
            Campaign.Parse("{\"gold\":" + Math.Max(0, gold ?? c.Gold) + ",\"cleared\":" + Set(cleared ?? c.Cleared) + ",\"bonus\":" + Set(bonus ?? c.BonusDone) + ",\"ups\":" + Set(ups ?? c.Upgrades) + "}");

        static string Set(IEnumerable<string> ids) => "{" + string.Join(",", ids.Select(id => "\"" + id + "\":true")) + "}";

        public static string[] FirstStages(int count) => Campaign.Stages.Take(count).Select(s => s.Id).ToArray();

        // progression.md 19: every unit type on its n-th branch (1-based); types without that branch get none.
        public static string[] Branch(int n) => GameData.Types.Select(t => GameData.Branches(t.Key).ElementAtOrDefault(n - 1)?.Id).Where(id => id != null).ToArray();

        // Same id as the base stage so its labels and boss name still resolve; played as a demo, so nothing is saved.
        public static Stage TestStage(Stage from, bool deployAnywhere) => new Stage(from.Id,
            GameData.Types.Select(t => t.Key).ToArray(), NoCap, NoCap, 0, from.Goal, from.Bonus, from.Boss,
            deployAnywhere ? from.Map.Select(row => row.Replace('.', 'd')).ToArray() : from.Map,
            from.Enemies, from.Sample, from.Spots, from.Hold, from.Escort, from.Triggers, from.Doors, from.Reinforcements);
    }
}
#endif
