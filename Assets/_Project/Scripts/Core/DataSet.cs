using System;
using System.Collections.Generic;
using System.Linq;

namespace VampireSpawn.Core
{
    public sealed class RuleSet
    {
        public readonly int DrainHeal, MoonMax, EatRange, MoonWake;

        public RuleSet(int drainHeal, int moonMax, int eatRange, int moonWake)
        {
            DrainHeal = drainHeal; MoonMax = moonMax; EatRange = eatRange; MoonWake = moonWake;
        }
    }

    // Every tunable number and text the rules read. PrototypeData builds the HTML one; the game builds one from the data assets.
    public sealed class DataSet
    {
        public readonly RuleSet Rules;
        public readonly UnitType[] Types;
        public readonly Upgrade[] Upgrades;
        public readonly EnemyStats Skeleton;
        public readonly Stage[] Stages;

        public DataSet(RuleSet rules, UnitType[] types, Upgrade[] upgrades, EnemyStats skeleton, Stage[] stages)
        {
            Rules = rules; Types = types; Upgrades = upgrades; Skeleton = skeleton; Stages = stages;
        }

        const string Tiles = ".#T~Sd";

        public List<string> Validate()
        {
            var p = new List<string>();
            var kinds = (UnitKind[])Enum.GetValues(typeof(UnitKind));
            if (Types.Length != kinds.Length) p.Add($"병종은 {kinds.Length}개여야 한다 (지금 {Types.Length})");
            for (var i = 0; i < Types.Length && i < kinds.Length; i++)
                if (Types[i].Kind != kinds[i]) p.Add($"병종 {i}번은 {kinds[i]} 이어야 한다 (지금 {Types[i].Kind})");
            foreach (var t in Types)
            {
                if (t.Hp <= 0) p.Add($"{t.Key}: 체력은 1 이상");
                if (t.Mov < 0 || t.Rng < 1) p.Add($"{t.Key}: 이동은 0 이상, 사거리는 1 이상");
            }
            foreach (var d in Upgrades.GroupBy(u => u.Id).Where(g => g.Count() > 1)) p.Add($"강화 id {d.Key} 가 겹친다 (캠페인 저장이 id 로 강화를 기억한다)");
            if (Rules.MoonWake < 1 || Rules.MoonWake > Rules.MoonMax) p.Add("관이 열리는 달은 1 이상, 보름달 이하");
            if (Rules.EatRange < 1) p.Add("흡혈 범위는 1 이상");
            if (Stages.Length == 0 || Stages[0].Id != "demo") p.Add("첫 장은 데모(id demo)여야 한다");
            foreach (var d in Stages.GroupBy(s => s.Id).Where(g => g.Count() > 1)) p.Add($"장 id {d.Key} 가 겹친다");
            foreach (var s in Stages) ValidateStage(s, p);
            return p;
        }

        void ValidateStage(Stage s, List<string> p)
        {
            var at = s.Id;
            if (s.Map.Length == 0) { p.Add(at + ": 지도가 비었다"); return; }
            var cols = s.Map[0].Length;
            for (var r = 0; r < s.Map.Length; r++)
            {
                if (s.Map[r].Length != cols) p.Add($"{at}: 지도 {r}행 길이 {s.Map[r].Length}, 0행은 {cols}");
                foreach (var ch in s.Map[r]) if (Tiles.IndexOf(ch) < 0) p.Add($"{at}: 지도 {r}행에 모르는 칸 '{ch}' (쓸 수 있는 칸 {Tiles})");
            }
            char Tile(int c, int r) => r >= 0 && r < s.Map.Length && c >= 0 && c < s.Map[r].Length ? s.Map[r][c] : '\0';
            if (s.Enemies.Count(e => e.Kind == EnemyKind.Vampire) != 1) p.Add(at + ": 뱀파이어는 하나여야 한다");
            var taken = new HashSet<(int, int)>();
            foreach (var e in s.Enemies)
            {
                var t = Tile(e.Col, e.Row);
                if (t == '\0' || t == '#' || t == '~') p.Add($"{at}: 적 {e.Kind} 이(가) 설 수 없는 칸 {e.Col},{e.Row}");
                if (!taken.Add((e.Col, e.Row))) p.Add($"{at}: {e.Col},{e.Row} 에 유닛이 겹친다");
            }
            foreach (var a in s.Sample)
            {
                if (Tile(a.Col, a.Row) != 'd') p.Add($"{at}: 예시 편성 {a.Kind} 이(가) 배치 구역 밖 {a.Col},{a.Row}");
                if (!taken.Add((a.Col, a.Row))) p.Add($"{at}: {a.Col},{a.Row} 에 유닛이 겹친다");
                if (!s.Units.Contains(a.Kind)) p.Add($"{at}: 예시 편성의 {a.Kind} 은(는) 이 장에서 쓸 수 없는 병종");
            }
            if (s.Units.Length == 0) p.Add(at + ": 쓸 수 있는 병종이 없다");
            if (s.Pop < 1) p.Add(at + ": 인구는 1 이상");
        }

        // One line per value, in a fixed order, so two data sets can be diffed.
        public List<string> Describe()
        {
            var o = new List<string>
            {
                $"rules drainHeal={Rules.DrainHeal} moonMax={Rules.MoonMax} eatRange={Rules.EatRange} moonWake={Rules.MoonWake}",
                $"skeleton hp={Skeleton.Hp} atk={Skeleton.Atk} def={Skeleton.Def} mov={Skeleton.Mov} rng={Skeleton.Rng}",
            };
            foreach (var t in Types)
                o.Add($"type {t.Key} tier={t.Tier} pop={t.Pop} silver={t.Silver} hp={t.Hp} atk={t.Atk} def={t.Def} mov={t.Mov} rng={t.Rng} bane={t.Bane} unlock={t.UnlockAt}");
            foreach (var u in Upgrades)
                o.Add($"upgrade {u.Id} unit={u.Unit} cost={u.Cost} inherit={u.Inherit} hp={u.Mod.Hp} atk={u.Mod.Atk} def={u.Mod.Def} mov={u.Mod.Mov} rng={u.Mod.Rng} bane={u.Mod.Bane} abil={u.Ability}");
            foreach (var s in Stages)
            {
                o.Add($"stage {s.Id} units={string.Join("+", s.Units)} pop={s.Pop} silver={s.Silver} reward={s.Reward} bonus={s.Bonus.Type}:{s.Bonus.Value}:{s.Bonus.Gold}");
                o.Add($"stage {s.Id} boss hp={s.Boss.Hp} atk={s.Boss.Atk} def={s.Boss.Def} mov={s.Boss.Mov} heal={s.Boss.Heal}");
                for (var r = 0; r < s.Map.Length; r++) o.Add($"stage {s.Id} map{r}={s.Map[r]}");
                o.Add($"stage {s.Id} enemies={string.Join(" ", s.Enemies.Select(e => e.Kind + "@" + e.Col + "," + e.Row))}");
                o.Add($"stage {s.Id} sample={string.Join(" ", s.Sample.Select(e => e.Kind + "@" + e.Col + "," + e.Row))}");
            }
            return o;
        }
    }

    public static class GameData
    {
        static DataSet current;

        public static DataSet Current => current ??= PrototypeData.Create();

        // Null goes back to the prototype values; the parity tests rely on that.
        public static void Use(DataSet data) => current = data;

        public static UnitType[] Types => Current.Types;
        public static Upgrade[] Upgrades => Current.Upgrades;
        public static EnemyStats Skeleton => Current.Skeleton;
        public static Stage[] Stages => Current.Stages;
        public static UnitType Type(UnitKind kind) => Current.Types[(int)kind];
    }
}
