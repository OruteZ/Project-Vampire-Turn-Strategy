using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VampireSpawn.Core
{
    public readonly struct StageResult
    {
        public readonly bool FirstClear, BonusMet;
        public readonly int ClearGold, BonusGold;

        public StageResult(bool firstClear, bool bonusMet, int clearGold, int bonusGold)
        {
            FirstClear = firstClear; BonusMet = bonusMet; ClearGold = clearGold; BonusGold = bonusGold;
        }
    }

    public sealed class Campaign
    {
        public const string SaveKey = "pvs-campaign-v2";

        public static Stage[] Stages => GameData.Stages.Where(s => s.Id != "demo").ToArray();

        public int Gold { get; private set; }
        readonly List<string> cleared = new List<string>();
        readonly List<string> bonus = new List<string>();
        readonly List<string> upgrades = new List<string>();

        public IReadOnlyList<string> Cleared => cleared;
        public IReadOnlyList<string> BonusDone => bonus;
        public IReadOnlyList<string> Upgrades => upgrades;

        public bool IsCleared(Stage s) => cleared.Contains(s.Id);
        public bool StageOpen(int index) => index == 0 || cleared.Contains(Stages[index - 1].Id);

        public bool UpgradesOpen(string kind)
        {
            var i = GameData.Type(kind).UnlockAt - 1;
            return i >= 0 && i < Stages.Length && cleared.Contains(Stages[i].Id);
        }

        public int UnlockedTier
        {
            get
            {
                var n = 1;
                for (var i = 0; i < Stages.Length; i++) if (StageOpen(i)) n = i + 1;
                return n;
            }
        }

        public int SpentGold => upgrades.Sum(id => GameData.Upgrades.First(u => u.Id == id).Cost);

        public Battle NewBattle(Stage stage, IEnumerable<Placement> formation = null) =>
            new Battle(stage, stage.Id == "demo" ? null : upgrades, formation);

        // docs/systems/progression.md 5-6: one branch per unit type; buying it locks the other two.
        public Upgrade Chosen(string unit) => GameData.Branches(unit).FirstOrDefault(u => upgrades.Contains(u.Id));

        public bool CanBuy(Upgrade up) => up != null && UpgradesOpen(up.Unit) && Chosen(up.Unit) == null && Gold >= up.Cost;

        public bool Buy(string upgradeId)
        {
            var up = GameData.Upgrades.FirstOrDefault(u => u.Id == upgradeId);
            if (!CanBuy(up)) return false;
            Gold -= up.Cost;
            upgrades.Add(up.Id);
            return true;
        }

        public StageResult Finish(Stage stage, bool won, bool bonusMet)
        {
            if (!won) return default;
            var ok = bonusMet;
            var first = !cleared.Contains(stage.Id);
            var clearGold = first ? stage.Reward : 0;
            var bonusGold = ok && !bonus.Contains(stage.Id) ? stage.Bonus.Gold : 0;
            if (first) cleared.Add(stage.Id);
            if (ok && !bonus.Contains(stage.Id)) bonus.Add(stage.Id);
            Gold += clearGold + bonusGold;
            return new StageResult(first, ok, clearGold, bonusGold);
        }

        static string Set(List<string> ids) => "{" + string.Join(",", ids.Select(id => "\"" + id + "\":true")) + "}";

        public string ToJson() =>
            "{\"gold\":" + Gold + ",\"cleared\":" + Set(cleared) + ",\"bonus\":" + Set(bonus) + ",\"ups\":" + Set(upgrades) + "}";

        // True when Parse had to fix the save (progression.md 17); the caller saves the fixed one.
        public bool Repaired { get; private set; }

        // Anything without a numeric gold field starts a fresh campaign.
        public static Campaign Parse(string raw)
        {
            var c = new Campaign();
            if (string.IsNullOrEmpty(raw)) return c;
            var gold = Regex.Match(raw, "\"gold\"\\s*:\\s*(-?\\d+)\\s*[,}]");
            if (!raw.TrimStart().StartsWith("{") || !gold.Success) return c;
            c.Gold = int.Parse(gold.Groups[1].Value);
            Read(raw, "cleared", c.cleared);
            Read(raw, "bonus", c.bonus);
            Read(raw, "ups", c.upgrades);
            c.KeepOneBranchEach();
            return c;
        }

        void KeepOneBranchEach()
        {
            var kept = new List<Upgrade>();
            foreach (var id in upgrades)
            {
                var up = GameData.Upgrades.FirstOrDefault(u => u.Id == id);
                if (up == null) continue;
                if (kept.Any(k => k.Unit == up.Unit)) { Gold += up.Cost; continue; }
                kept.Add(up);
            }
            Repaired = kept.Count != upgrades.Count;
            upgrades.Clear();
            upgrades.AddRange(kept.Select(u => u.Id));
        }

        static void Read(string raw, string field, List<string> into)
        {
            var section = Regex.Match(raw, "\"" + field + "\"\\s*:\\s*\\{([^{}]*)\\}");
            if (!section.Success) return;
            foreach (Match m in Regex.Matches(section.Groups[1].Value, "\"([^\"]+)\"\\s*:\\s*true"))
                if (!into.Contains(m.Groups[1].Value)) into.Add(m.Groups[1].Value);
        }
    }
}
