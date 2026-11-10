using System;
using System.Collections.Generic;
using System.Linq;

namespace VampireSpawn.Core
{
    public sealed class RuleSet
    {
        public readonly int DrainHeal, MoonMax, EatRange, MoonWake, MendRange;

        public RuleSet(int drainHeal, int moonMax, int eatRange, int moonWake, int mendRange = 2)
        {
            DrainHeal = drainHeal; MoonMax = moonMax; EatRange = eatRange; MoonWake = moonWake; MendRange = mendRange;
        }
    }

    // Every tunable number the rules read, built from the data assets (docs/guides/data-editor.md).
    public sealed class DataSet
    {
        public readonly RuleSet Rules;
        public readonly UnitType[] Types;
        public readonly Upgrade[] Upgrades;
        public readonly EnemyType[] Minions;
        public readonly Stage[] Stages;

        public DataSet(RuleSet rules, UnitType[] types, Upgrade[] upgrades, EnemyType[] minions, Stage[] stages)
        {
            Rules = rules; Types = types; Upgrades = upgrades; Minions = minions; Stages = stages;
        }

        public UnitType Type(string key) => Array.Find(Types, t => t.Key == key) ?? throw new ArgumentException("unknown unit type " + key);
        public EnemyType Minion(string key) => Array.Find(Minions, m => m.Key == key) ?? throw new ArgumentException("unknown minion " + key);
        // Branch order is list order (docs/systems/progression.md 7).
        public Upgrade[] Branches(string unit) => Upgrades.Where(u => u.Unit == unit).ToArray();

        const string Tiles = ".#T~Sd,";

        public List<string> Validate()
        {
            var p = new List<string>();
            foreach (var d in Types.GroupBy(t => t.Key).Where(g => g.Count() > 1)) p.Add($"병종 key {d.Key} 가 겹친다");
            foreach (var t in Types)
            {
                if (string.IsNullOrEmpty(t.Key)) p.Add("key 가 빈 병종이 있다");
                if (t.Hp <= 0) p.Add($"{t.Key}: 체력은 1 이상");
                if (t.Mov < 0 || t.Rng < 1) p.Add($"{t.Key}: 이동은 0 이상, 사거리는 1 이상");
            }
            foreach (var d in Minions.GroupBy(m => m.Key).Where(g => g.Count() > 1)) p.Add($"하수인 key {d.Key} 가 겹친다");
            foreach (var m in Minions)
            {
                if (string.IsNullOrEmpty(m.Key) || m.Key == EnemyType.Vampire) p.Add($"하수인 key '{m.Key}' 는 쓸 수 없다");
                if (m.Hp <= 0 || m.Rng < 1) p.Add($"{m.Key}: 체력·사거리는 1 이상");
            }
            foreach (var d in Upgrades.GroupBy(u => u.Id).Where(g => g.Count() > 1)) p.Add($"강화 id {d.Key} 가 겹친다 (캠페인 저장이 id 로 강화를 기억한다)");
            if (Rules.MoonWake < 1 || Rules.MoonWake > Rules.MoonMax) p.Add("관이 열리는 달은 1 이상, 보름달 이하");
            if (Rules.EatRange < 1) p.Add("흡혈 범위는 1 이상");
            if (Rules.MendRange < 1) p.Add("치료 거리는 1 이상");
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
            foreach (var k in s.Units) if (Types.All(t => t.Key != k)) p.Add($"{at}: 없는 병종 {k}");
            if (s.Enemies.Count(e => e.Kind == EnemyType.Vampire) != 1) p.Add(at + ": 뱀파이어는 하나여야 한다");
            var taken = new HashSet<(int, int)>();
            foreach (var e in s.Enemies)
            {
                if (e.Kind != EnemyType.Vampire && Minions.All(m => m.Key != e.Kind)) p.Add($"{at}: 없는 적 {e.Kind}");
                var t = Tile(e.Col, e.Row);
                if (t == '\0' || t == '#' || t == '~') p.Add($"{at}: 적 {e.Kind} 이(가) 설 수 없는 칸 {e.Col},{e.Row}");
                if (e.Arrive == 0 && !taken.Add((e.Col, e.Row))) p.Add($"{at}: {e.Col},{e.Row} 에 유닛이 겹친다");
                if (e.Arrive < 0 || (e.Arrive > 0 && e.Kind == EnemyType.Vampire)) p.Add($"{at}: 증원 턴 {e.Arrive} 은(는) 하수인만, 1 이상");
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
    }

    public static class GameData
    {
        static DataSet current;

        // Set by the Game side (App in play, an editor hook in the Editor) because Core cannot load assets.
        public static Func<DataSet> Loader;

        public static DataSet Current => current ??= Loader?.Invoke() ?? throw new InvalidOperationException("게임 데이터를 읽지 못했다 (GameData.Loader 가 없다)");

        // Null drops the cached set; the next read asks the Loader again.
        public static void Use(DataSet data) => current = data;

        public static UnitType[] Types => Current.Types;
        public static Upgrade[] Upgrades => Current.Upgrades;
        public static EnemyType[] Minions => Current.Minions;
        public static Stage[] Stages => Current.Stages;
        public static UnitType Type(string key) => Current.Type(key);
        public static EnemyType Minion(string key) => Current.Minion(key);
        public static Upgrade[] Branches(string unit) => Current.Branches(unit);
    }
}
