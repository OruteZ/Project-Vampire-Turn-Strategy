namespace VampireSpawn.Core
{
    public enum Tier { Peasant, Regular, Knight, Hunter }
    public enum Ability { None, Pike, Ambush, Cleave, Charge }
    public enum BonusType { Turns, NoEat, AllMinions }
    public enum EnemyKind { Skeleton, Vampire }

    public sealed class UnitType
    {
        public readonly UnitKind Kind;
        public readonly string Key, Name, Glyph, Desc;
        public readonly Tier Tier;
        public readonly int Pop, Silver, Hp, Atk, Def, Mov, Rng, Bane, UnlockAt;

        public UnitType(UnitKind kind, string key, string name, string glyph, Tier tier, int pop, int silver,
            int hp, int atk, int def, int mov, int rng, int bane, int unlockAt, string desc)
        {
            Kind = kind; Key = key; Name = name; Glyph = glyph; Tier = tier; Pop = pop; Silver = silver;
            Hp = hp; Atk = atk; Def = def; Mov = mov; Rng = rng; Bane = bane; UnlockAt = unlockAt; Desc = desc;
        }
    }

    public readonly struct StatMod
    {
        public readonly int Hp, Atk, Def, Mov, Rng, Bane;

        public StatMod(int hp, int atk, int def, int mov, int rng, int bane)
        {
            Hp = hp; Atk = atk; Def = def; Mov = mov; Rng = rng; Bane = bane;
        }
    }

    public sealed class Upgrade
    {
        public readonly UnitKind Unit;
        public readonly string Id, Name, Desc;
        public readonly int Cost;
        public readonly bool Inherit;
        public readonly StatMod Mod;
        public readonly Ability Ability;

        public Upgrade(UnitKind unit, string id, string name, string desc, int cost, bool inherit, StatMod mod, Ability ability)
        {
            Unit = unit; Id = id; Name = name; Desc = desc; Cost = cost; Inherit = inherit; Mod = mod; Ability = ability;
        }
    }

    public sealed class EnemyStats
    {
        public readonly string Name, Glyph;
        public readonly int Hp, Atk, Def, Mov, Rng;

        public EnemyStats(string name, string glyph, int hp, int atk, int def, int mov, int rng)
        {
            Name = name; Glyph = glyph; Hp = hp; Atk = atk; Def = def; Mov = mov; Rng = rng;
        }
    }

    public sealed class Boss
    {
        public readonly string Name;
        public readonly int Hp, Atk, Def, Mov, Heal;

        public Boss(string name, int hp, int atk, int def, int mov, int heal)
        {
            Name = name; Hp = hp; Atk = atk; Def = def; Mov = mov; Heal = heal;
        }
    }

    public sealed class Bonus
    {
        public readonly BonusType Type;
        public readonly int Value, Gold;

        public Bonus(BonusType type, int value, int gold)
        {
            Type = type; Value = value; Gold = gold;
        }

        public bool IsMet(int turn, int drained, bool skeletonsLeft) => Type switch
        {
            BonusType.Turns => turn <= Value,
            BonusType.NoEat => drained == 0,
            _ => !skeletonsLeft,
        };

        public string Text => Type switch
        {
            BonusType.Turns => $"{Value}턴 이내 승리",
            BonusType.NoEat => "보름달 흡혈을 한 번도 허용하지 않기",
            _ => "스켈레톤을 모두 쓰러뜨리고 승리",
        };
    }

    public readonly struct Placement<TKind>
    {
        public readonly TKind Kind;
        public readonly int Col, Row;

        public Placement(TKind kind, int col, int row)
        {
            Kind = kind; Col = col; Row = row;
        }
    }

    public sealed class Stage
    {
        public readonly string Id, Label, Title, Brief;
        public readonly UnitKind[] Units;
        public readonly int Pop, Silver, Reward;
        public readonly Bonus Bonus;
        public readonly Boss Boss;
        public readonly string[] Map;
        public readonly Placement<EnemyKind>[] Enemies;
        public readonly Placement<UnitKind>[] Sample;

        public Stage(string id, string label, string title, string brief, UnitKind[] units, int pop, int silver, int reward,
            Bonus bonus, Boss boss, string[] map, Placement<EnemyKind>[] enemies, Placement<UnitKind>[] sample)
        {
            Id = id; Label = label; Title = title; Brief = brief; Units = units; Pop = pop; Silver = silver; Reward = reward;
            Bonus = bonus; Boss = boss; Map = map; Enemies = enemies; Sample = sample;
        }
    }

    public static partial class GameData
    {
        public static UnitType Type(UnitKind kind) => Types[(int)kind];
    }
}
