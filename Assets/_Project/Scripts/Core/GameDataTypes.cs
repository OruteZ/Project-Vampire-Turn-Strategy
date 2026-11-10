using System;
using System.Linq;

namespace VampireSpawn.Core
{
    public enum Tier { Peasant, Regular, Knight, Hunter }
    // Stored as numbers in the unit assets: add new skills at the end only.
    public enum Ability { None, Pike, Ambush, Cleave, Charge, Formation, Ward, Rite, Twice, Bury, Mend }
    public enum BonusType { Turns, NoEat, AllMinions }
    // What wins a battle (docs/dev/battle-goals.md). Stored as numbers in the stage assets: add new goals at the end only.
    public enum Goal { Boss, Defense, Escort }

    public sealed class UnitType
    {
        public readonly string Key;
        public readonly Tier Tier;
        public readonly int Pop, Silver, Hp, Atk, Def, Mov, Rng, Bane, Heal, UnlockAt;
        public readonly Ability[] Skills;

        public Message Name => new Message("unit." + Key + ".name");
        public Message Glyph => new Message("unit." + Key + ".glyph");
        public Message Desc => new Message("unit." + Key + ".desc");

        public UnitType(string key, Tier tier, int pop, int silver, int hp, int atk, int def, int mov, int rng, int bane, int heal,
            int unlockAt, Ability[] skills = null)
        {
            Key = key; Tier = tier; Pop = pop; Silver = silver;
            Hp = hp; Atk = atk; Def = def; Mov = mov; Rng = rng; Bane = bane; Heal = heal; UnlockAt = unlockAt;
            Skills = skills ?? Array.Empty<Ability>();
        }
    }

    public readonly struct StatMod
    {
        public readonly int Hp, Atk, Def, Mov, Rng, Bane, Heal;

        public StatMod(int hp, int atk, int def, int mov, int rng, int bane, int heal = 0)
        {
            Hp = hp; Atk = atk; Def = def; Mov = mov; Rng = rng; Bane = bane; Heal = heal;
        }
    }

    public sealed class Upgrade
    {
        public readonly string Unit;
        public readonly string Id;
        public readonly int Cost;
        public Message Name => new Message("upgrade." + Id + ".name");
        public Message Desc => new Message("upgrade." + Id + ".desc");
        // docs/systems/progression.md 15: only hp, atk and mov carry over to a spawn.
        public bool Inherit => Ability == Ability.None && Mod.Def == 0 && Mod.Rng == 0 && Mod.Bane == 0 && Mod.Heal == 0;
        public readonly StatMod Mod;
        public readonly Ability Ability;

        public Upgrade(string unit, string id, int cost, StatMod mod, Ability ability)
        {
            Unit = unit; Id = id; Cost = cost; Mod = mod; Ability = ability;
        }
    }

    // A minion kind (docs/content/enemies.md 2). The vampire's numbers live on each stage.
    public sealed class EnemyType
    {
        public const string Vampire = "vampire";

        public readonly string Key;
        public readonly int Hp, Atk, Def, Mov, Rng;
        public Message Name => new Message(Key + ".name");
        public Message Glyph => new Message(Key + ".glyph");
        public Message Note => new Message("unit.card." + Key + "Note");

        public EnemyType(string key, int hp, int atk, int def, int mov, int rng)
        {
            Key = key; Hp = hp; Atk = atk; Def = def; Mov = mov; Rng = rng;
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

    // The unit to bring on an escort stage (docs/systems/goals.md 3).
    public sealed class EscortStats
    {
        public readonly int Hp, Def, Mov;
        public readonly Hex Start;

        public EscortStats(int hp, int def, int mov, Hex start)
        {
            Hp = hp; Def = def; Mov = mov; Start = start;
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

        public bool IsMet(int turn, int drained, bool minionsLeft) => Type switch
        {
            BonusType.Turns => turn <= Value,
            BonusType.NoEat => drained == 0,
            _ => !minionsLeft,
        };

        public Message Text => Type switch
        {
            BonusType.Turns => new Message("bonus.turns", Value),
            BonusType.NoEat => new Message("bonus.noEat"),
            _ => new Message("bonus.allMinions"),
        };
    }

    // Kind is a unit type key for allies, a minion key or EnemyType.Vampire for enemies.
    public readonly struct Placement
    {
        public readonly string Kind;
        public readonly int Col, Row;

        public Placement(string kind, int col, int row)
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
        public Message Place => new Message("stage." + Id + ".place");
        public Message EscortName => new Message("stage." + Id + ".escort");
        public readonly string[] Units;
        public readonly int Pop, Silver, Reward;
        public readonly Goal Goal;
        public readonly Bonus Bonus;
        public readonly Boss Boss;
        public readonly string[] Map;
        public readonly Placement[] Enemies;
        public readonly Placement[] Sample;
        // Defended hexes on a defence stage, destinations on an escort stage.
        public readonly Hex[] Spots;
        public readonly int Hold;
        public readonly EscortStats Escort;

        public Stage(string id, string[] units, int pop, int silver, int reward, Goal goal,
            Bonus bonus, Boss boss, string[] map, Placement[] enemies, Placement[] sample,
            Hex[] spots = null, int hold = 0, EscortStats escort = null)
        {
            Id = id; Units = units; Pop = pop; Silver = silver; Reward = reward; Goal = goal;
            Bonus = bonus; Boss = boss; Map = map; Enemies = enemies; Sample = sample;
            Spots = spots ?? Array.Empty<Hex>(); Hold = hold; Escort = escort;
        }

        public Placement[] Minions => Enemies.Where(e => e.Kind != EnemyType.Vampire).ToArray();

        public Message Objective => Goal switch
        {
            Goal.Boss => new Message("battle.defeatBoss", BossName),
            Goal.Defense => new Message("goal.defense", Place, Hold),
            Goal.Escort => new Message("goal.escort", EscortName, Place),
            _ => throw new ArgumentOutOfRangeException(nameof(Goal), Goal, null),
        };

        // docs/systems/goals.md 1-2: killing the vampire wins whatever the goal.
        public Message AlsoWins => Goal == Goal.Boss ? default : new Message("goal.alsoBoss", BossName);
    }
}
