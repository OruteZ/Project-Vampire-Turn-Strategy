using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    // What the unit detail window shows (docs/ui/unit-detail.md), as text and tokens; UnitDetail draws it.
    public sealed class DetailModel
    {
        public sealed class Stat { public string Icon, Label, Value, Tip; public bool Changed, Low; }
        // A state is a term: the window shows the name, the description opens on hover (unit-detail.md 7).
        public sealed class State { public string Id, Name, Desc; public Color Color; }
        public sealed class Skill { public string Icon, Name, Tag, Key, Desc; public Color Color; }
        public sealed class Up { public string Name, Effect; public bool Own, Inherit; }

        public Color Edge;
        public bool Enemy;
        // Icon is the unit key whose look names the sprite; enemies have no look icon, so Fallback is the sprite drawn instead.
        public string Icon, Fallback, Name, RoleText;
        public int Pop, Silver;
        public List<Stat> Stats = new List<Stat>();
        public List<State> States = new List<State>();
        // hp, atk, def, mov, rng of the spawn this ally would rise as; null for everything else.
        public int[] Spawn;
        public string SkillsTitle;
        public List<Skill> Skills = new List<Skill>();
        public List<Up> Upgrades;

        // A unit on the board: its values right now.
        public static DetailModel Of(Battle b, Unit u, IEnumerable<string> upgrades) => Build(u, b, upgrades);

        // A unit type from a list: its values with the owned upgrades.
        public static DetailModel OfType(string kind, IEnumerable<string> upgrades) => Build(Battle.Preview(kind, upgrades), null, upgrades);

        static DetailModel Build(Unit u, Battle b, IEnumerable<string> upgrades)
        {
            var owned = new HashSet<string>(upgrades ?? Enumerable.Empty<string>());
            var type = u.Role == Role.Ally && u.Type != null ? GameData.Type(u.Type) : null;
            var spawn = u.Role == Role.Spawn;
            var m = new DetailModel
            {
                Edge = u.Side == Side.Ally ? Ui.Steel : spawn ? Ui.Spawn : Ui.Blood,
                Enemy = u.Side == Side.Enemy && !spawn,
                Icon = u.Type ?? u.Minion ?? EnemyType.Vampire,
                Fallback = spawn ? Kit.Icon.Fang : u.Role == Role.Vampire ? Kit.Icon.Boss : u.Role == Role.Minion ? Kit.Icon.Skulls : Kit.Icon.Pop,
                Name = spawn ? Loc.T("hud.spawnOf", u.Origin) : Loc.Text(u.Name),
                RoleText = RoleOf(u, type),
                Pop = type?.Pop ?? 0,
                Silver = type?.Silver ?? 0,
                SkillsTitle = Loc.T(u.Side == Side.Ally ? "detail.sec.skills" : "detail.sec.traits"),
            };
            var mine = type == null ? new Upgrade[0] : GameData.Branches(type.Key).Where(x => owned.Contains(x.Id)).ToArray();
            AddStats(m, u, type, mine, b != null);
            if (b != null) AddStates(m, b, u);
            if (type != null) m.Spawn = new[] { u.MaxHp, u.Atk, 0, u.Mov, 1 };
            if (u.Side == Side.Ally) AddSkills(m, u, type);
            else AddTraits(m, u);
            if (u.Rng >= 2) m.Skills.Add(Trait("ranged", Kit.Icon.Rng, m.Edge, u.Rng));
            if (type != null)
                m.Upgrades = GameData.Branches(type.Key).Select(x => new Up
                    { Name = Loc.Text(x.Name), Effect = Loc.Text(x.Desc), Own = owned.Contains(x.Id), Inherit = x.Inherit }).ToList();
            return m;
        }

        static string RoleOf(Unit u, UnitType type)
        {
            switch (u.Role)
            {
                case Role.Ally: return type == null ? Loc.T("unit.card.ally") : Loc.T("role." + type.Key) + " · " + Loc.T("unit.card.ally");
                case Role.Escort: return Loc.T("unit.card.escort");
                case Role.Minion: return Loc.T("unit.card.minion");
                case Role.Vampire: return Loc.T("unit.card.boss");
            }
            return Loc.T("unit.card.spawn");
        }

        static void AddStats(DetailModel m, Unit u, UnitType type, Upgrade[] mine, bool live)
        {
            void Add(string icon, string label, string value, int now, int? was, System.Func<StatMod, int> mod, bool low = false)
            {
                var changed = was.HasValue && was.Value != now;
                var tip = !changed ? null : string.Join("\n", new[] { Loc.T("detail.base", was.Value) }
                    .Concat(mine.Where(x => mod(x.Mod) != 0).Select(x => Loc.Text(x.Name) + " " + mod(x.Mod).ToString("+0;-0"))));
                m.Stats.Add(new Stat { Icon = icon, Label = label, Value = value, Changed = changed, Tip = tip, Low = low });
            }
            Add(Kit.Icon.Hp, Loc.T("unit.card.hp"), live ? u.Hp + " / " + u.MaxHp : u.MaxHp.ToString(), u.MaxHp, type?.Hp, x => x.Hp,
                live && u.Side == Side.Ally && u.Hp * 3 <= u.MaxHp);
            Add(Kit.Icon.Atk, Loc.T("unit.card.atk"), u.Atk.ToString(), u.Atk, type?.Atk, x => x.Atk);
            if (u.Bane > 0) Add(Kit.Icon.Fang, Loc.T("detail.bane"), "+" + u.Bane, u.Bane, type?.Bane, x => x.Bane);
            Add(Kit.Icon.Def, Loc.T("unit.card.def"), u.Def.ToString(), u.Def, type?.Def, x => x.Def);
            Add(Kit.Icon.Mov, Loc.T("unit.card.mov"), u.Mov.ToString(), u.Mov, type?.Mov, x => x.Mov);
            Add(Kit.Icon.Rng, Loc.T("unit.card.rng"), u.Rng.ToString(), u.Rng, type?.Rng, x => x.Rng);
            if (u.Heal > 0 && u.Side == Side.Ally) Add(Kit.Icon.Heal, Loc.T("skill.mend"), u.Heal.ToString(), u.Heal, type?.Heal, x => x.Heal);
        }

        static void AddStates(DetailModel m, Battle b, Unit u)
        {
            void Add(string id, Color c, params object[] args) =>
                m.States.Add(new State { Id = id, Name = Loc.T("state." + id, args), Desc = Loc.T("state." + id + ".desc", args), Color = c });
            var ally = u.Side == Side.Ally;
            var lit = b.Board.Lit(u.Pos);
            if (ally && b.EatTarget == u.Id)
            {
                Add("target", Ui.Blood, Rules.EatRange);
                var v = b.Vampire;
                if (v != null && (Hex.Distance(v.Pos, u.Pos) > Rules.EatRange || lit)) Add("safe", Ui.Muted);
            }
            if (ally && lit) Add("lit", Ui.Candle);
            if (u.JustRisen) Add("risen", Ui.Spawn);
            if (u.Role == Role.Vampire && u.Asleep) Add("coffin", Ui.Candle, Rules.MoonWake);
            if (!ally && u.Stance == Stance.Guard)
                m.States.Add(new State { Id = "guard", Name = Loc.T("state.guard", u.Alert), Desc = Loc.T("unit.card.stance.guard", u.Alert), Color = Ui.Candle });
            if (!ally && u.Stance == Stance.Hold)
                m.States.Add(new State { Id = "hold", Name = Loc.T("state.hold"), Desc = Loc.T("unit.card.stance.hold"), Color = Ui.Muted });
            if (u.Has(Ability.Trap)) Add("traps", Ui.Steel, b.TrapsLeft(u), u.Has(Ability.Satchel) ? Battle.SatchelHeld : Battle.TrapsHeld);
        }

        static readonly Dictionary<Ability, (string icon, Color color)> Acts = new Dictionary<Ability, (string, Color)>
        {
            [Ability.Mend] = (Kit.Icon.Heal, Ui.Steel), [Ability.Trap] = (Kit.Icon.Target, Ui.Steel), [Ability.Bury] = (Kit.Icon.Fang, Ui.Spawn),
        };

        static void AddSkills(DetailModel m, Unit u, UnitType type)
        {
            var innate = type?.Skills ?? new Ability[0];
            foreach (var a in u.Abil.Where(a => a != Ability.None).Distinct().OrderBy(a => innate.Contains(a) ? 0 : 1))
            {
                var act = Acts.TryGetValue(a, out var look);
                var id = SkillId(a);
                m.Skills.Add(new Skill
                {
                    Icon = act ? look.icon : Kit.Icon.Up, Color = act ? look.color : Ui.Muted, Name = BattleScreen.SkillName(a), Key = act ? "5" : null,
                    Tag = Loc.T(act ? "detail.kind.action" : "detail.kind.passive") + " · " + Loc.T(innate.Contains(a) ? "detail.src.base" : "detail.src.upgrade"),
                    Desc = Loc.T("skill." + id + ".desc", SkillArgs(a, u)),
                });
            }
        }

        public static string SkillId(Ability a) => char.ToLowerInvariant(a.ToString()[0]) + a.ToString().Substring(1);

        // The numbers a skill's text shows come from the rules and the unit, not from the sentence (unit-detail.md 9).
        static object[] SkillArgs(Ability a, Unit u)
        {
            switch (a)
            {
                case Ability.Bury: return new object[] { Battle.BuryRange };
                case Ability.Mend: return new object[] { Rules.MendRange, u.Heal };
                case Ability.Trap: return new object[] { Battle.TrapDamage + (u.Has(Ability.Barbs) ? Battle.BarbsDamage : 0), u.Has(Ability.Satchel) ? Battle.SatchelHeld : Battle.TrapsHeld };
                case Ability.Barbs: return new object[] { Battle.BarbsDamage };
                case Ability.Satchel: return new object[] { Battle.SatchelHeld };
            }
            return new object[0];
        }

        static Skill Trait(string id, string icon, Color c, params object[] args) =>
            new Skill { Icon = icon, Color = c, Name = Loc.T("detail.trait." + id), Desc = Loc.T("detail.trait." + id + ".desc", args) };

        static void AddTraits(DetailModel m, Unit u)
        {
            switch (u.Role)
            {
                case Role.Minion:
                    m.Skills.Add(Trait("minion", Kit.Icon.Skulls, Ui.Blood));
                    break;
                case Role.Vampire:
                    m.Skills.Add(Trait("coffin", Kit.Icon.Turn, Ui.Blood, Rules.MoonWake));
                    m.Skills.Add(Trait("feed", Kit.Icon.Target, Ui.Blood, Rules.MoonMax, Rules.EatRange, u.Heal));
                    m.Skills.Add(Trait("turn", Kit.Icon.Fang, Ui.Spawn));
                    break;
                default:
                    m.Skills.Add(Trait("origin", Kit.Icon.Fang, Ui.Spawn, u.Origin));
                    m.Skills.Add(Trait("turn", Kit.Icon.Fang, Ui.Spawn));
                    break;
            }
        }
    }
}
