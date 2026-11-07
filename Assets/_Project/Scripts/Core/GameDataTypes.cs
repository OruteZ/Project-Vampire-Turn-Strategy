namespace VampireSpawn.Core
{
    public enum Tier { Peasant, Regular, Knight, Hunter }
    public enum Ability { None, Pike, Ambush, Cleave, Charge }
    public enum BonusType { Turns, NoEat, AllMinions }
    public enum EnemyKind { Skeleton, Vampire }

    public sealed class UnitType
    {
        public readonly UnitKind Kind;
        public readonly string Key;
        public readonly Tier Tier;
        public readonly int Pop, Silver, Hp, Atk, Def, Mov, Rng, Bane, UnlockAt;

        public Message Name => new Message("unit." + Key + ".name");
        public Message Glyph => new Message("unit." + Key + ".glyph");
        public Message Desc => new Message("unit." + Key + ".desc");

        public UnitType(UnitKind kind, string key, Tier tier, int pop, int silver,
            int hp, int atk, int def, int mov, int rng, int bane, int unlockAt)
        {
            Kind = kind; Key = key; Tier = tier; Pop = pop; Silver = silver;
            Hp = hp; Atk = atk; Def = def; Mov = mov; Rng = rng; Bane = bane; UnlockAt = unlockAt;
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
        public readonly string Id;
        public readonly int Cost;
        public Message Name => new Message("upgrade." + Id + ".name");
        public Message Desc => new Message("upgrade." + Id + ".desc");
        // docs/systems/progression.md 15: only hp, atk and mov carry over to a spawn.
        public bool Inherit => Ability == Ability.None && Mod.Def == 0 && Mod.Rng == 0 && Mod.Bane == 0;
        public readonly StatMod Mod;
        public readonly Ability Ability;

        public Upgrade(UnitKind unit, string id, int cost, StatMod mod, Ability ability)
        {
            Unit = unit; Id = id; Cost = cost; Mod = mod; Ability = ability;
        }
    }

    public sealed class EnemyStats
    {
        public readonly int Hp, Atk, Def, Mov, Rng;
        public Message Name => new Message("skeleton.name");
        public Message Glyph => new Message("skeleton.glyph");

        public EnemyStats(int hp, int atk, int def, int mov, int rng)
        {
            Hp = hp; Atk = atk; Def = def; Mov = mov; Rng = rng;
        }
    }

    public sealed class Boss
    {
        public readonly int Hp, Atk, Def, Mov, Heal;

        public Boss(int hp, int atk, int def, int mov, int heal)
        {
            Hp = hp; Atk = atk; Def = def; Mov = mov; Heal = heal;
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

        public Message Text => Type switch
        {
            BonusType.Turns => new Message("bonus.turns", Value),
            BonusType.NoEat => new Message("bonus.noEat"),
            _ => new Message("bonus.allMinions"),
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
        public readonly string Id;
        public Message Label => new Message("stage." + Id + ".label");
        public Message Title => new Message("stage." + Id + ".title");
        public Message Brief => new Message("stage." + Id + ".brief");
        public Message BossName => new Message("stage." + Id + ".boss");
        public readonly UnitKind[] Units;
        public readonly int Pop, Silver, Reward;
        public readonly Bonus Bonus;
        public readonly Boss Boss;
        public readonly string[] Map;
        public readonly Placement<EnemyKind>[] Enemies;
        public readonly Placement<UnitKind>[] Sample;

        public Stage(string id, UnitKind[] units, int pop, int silver, int reward,
            Bonus bonus, Boss boss, string[] map, Placement<EnemyKind>[] enemies, Placement<UnitKind>[] sample)
        {
            Id = id; Units = units; Pop = pop; Silver = silver; Reward = reward;
            Bonus = bonus; Boss = boss; Map = map; Enemies = enemies; Sample = sample;
        }
    }
}
