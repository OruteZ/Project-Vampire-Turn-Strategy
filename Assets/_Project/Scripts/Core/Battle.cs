using System;
using System.Collections.Generic;
using System.Linq;

namespace VampireSpawn.Core
{
    public enum Phase { Deploy, Player, Enemy, Over }
    public enum Role { Ally, Minion, Vampire, Spawn, Escort }
    public enum Ending { Slain, Held, Arrived, Wiped, Overrun, EscortFell }

    public sealed class Unit
    {
        public int Id { get; internal set; }
        public Role Role { get; internal set; }
        // Ally type key, or for a spawn the type it rose from. Null for minions and the vampire.
        public string Type { get; internal set; }
        // Minion type key; null for everything else.
        public string Minion { get; internal set; }
        public Message Name { get; internal set; }
        public Message Glyph { get; internal set; }
        public Message Origin { get; internal set; }
        public Hex Pos { get; internal set; }
        public int Hp { get; internal set; }
        public int MaxHp { get; internal set; }
        public int Atk { get; internal set; }
        public int Def { get; internal set; }
        public int Mov { get; internal set; }
        public int Rng { get; internal set; }
        public int Bane { get; internal set; }
        public int Heal { get; internal set; }
        public int MovedDist { get; internal set; }
        public bool Moved { get; internal set; }
        public bool Acted { get; internal set; }
        // The one extra action of Cleave or Twice was used this turn.
        public bool ActedAgain { get; internal set; }
        public bool Asleep { get; internal set; }
        public bool JustRisen { get; internal set; }
        public bool JustWoke { get; internal set; }
        public bool JustArrived { get; internal set; }
        public Stance Stance { get; internal set; }
        public int Alert { get; internal set; }
        internal readonly List<Ability> abil = new List<Ability>();
        public IReadOnlyList<Ability> Abil => abil;

        public Side Side => Role == Role.Ally || Role == Role.Escort ? Side.Ally : Side.Enemy;
        public string Kind => Role == Role.Ally ? Type : Role == Role.Minion ? Minion : Role.ToString().ToLowerInvariant();
        public bool IsUndead => Role == Role.Vampire || Role == Role.Spawn;
        public bool Has(Ability a) => abil.Contains(a);
    }

    public sealed class Corpse
    {
        public int Id { get; internal set; }
        public Hex Pos { get; internal set; }
        public Message Name { get; internal set; }
        public Message Glyph { get; internal set; }
        public string Type { get; internal set; }
        public int MaxHp { get; internal set; }
        public int Atk { get; internal set; }
        public int Def { get; internal set; }
        public int Mov { get; internal set; }
    }

    public sealed class BattleStats
    {
        public int Lost { get; internal set; }
        public int Executed { get; internal set; }
        public int Risen { get; internal set; }
        public int Drained { get; internal set; }
    }

    public readonly struct LogEntry
    {
        public readonly Message Text;
        public readonly string Style;
        public LogEntry(Message text, string style) { Text = text; Style = style; }
    }

    public sealed partial class Battle
    {
        const int LogCap = 80;

        public readonly Stage Stage;
        public readonly Board Board;
        public readonly BattleStats Stats = new BattleStats();
        public Phase Phase { get; private set; } = Phase.Deploy;
        public int Turn { get; private set; } = 1;
        public int Moon { get; private set; }
        public int? EatTarget { get; private set; }
        public bool? Won { get; private set; }
        public Ending? Ending { get; private set; }
        public IReadOnlyList<Placement> StartFormation { get; private set; } = Array.Empty<Placement>();

        readonly List<Unit> units = new List<Unit>();
        readonly List<Corpse> corpses = new List<Corpse>();
        readonly List<LogEntry> log = new List<LogEntry>();
        public IReadOnlyList<Unit> Units => units;
        public IReadOnlyList<Corpse> Corpses => corpses;
        public IReadOnlyList<LogEntry> Log => log;

        readonly HashSet<string> upgrades;
        readonly List<Placement> pending = new List<Placement>();
        // Reinforcements not on the board yet, in stage order.
        public IReadOnlyList<Placement> Pending => pending;
        readonly HashSet<string> fired = new HashSet<string>();
        public bool Fired(string trigger) => fired.Contains(trigger);
        public int MinionsKilled { get; private set; }

        Unit Placed(Placement p, Hex at)
        {
            var u = MakeEnemy(p.Kind, at);
            if (u.Role == Role.Minion) { u.Stance = p.Stance; u.Alert = p.Alert; }
            return u;
        }

        // docs/systems/triggers.md 1-1..1-2. Effects wait for the next enemy phase start (Arrive).
        void Fire(Func<Trigger, bool> met)
        {
            foreach (var t in Stage.Triggers)
            {
                if (fired.Contains(t.Id) || !met(t)) continue;
                fired.Add(t.Id);
                Say(new Message("log.trigger", new Message("trigger.fired")), "k");
            }
        }
        readonly List<BattleEvent> events = new List<BattleEvent>();
        // Ids are never shown; only their order matters (creation order is acting order).
        int nextId = 1;

        public Battle(Stage stage, IEnumerable<string> ownedUpgrades = null, IEnumerable<Placement> formation = null)
        {
            Stage = stage;
            Board = new Board(stage.Map);
            upgrades = new HashSet<string>(ownedUpgrades ?? Array.Empty<string>());
            foreach (var id in upgrades)
                if (GameData.Upgrades.All(u => u.Id != id)) throw new ArgumentException("unknown upgrade " + id);
            foreach (var e in stage.Enemies)
                if (e.Reinforcement) pending.Add(e);
                else units.Add(Placed(e, new Hex(e.Col, e.Row)));
            if (stage.Escort is EscortStats es)
                units.Add(new Unit
                {
                    Id = nextId++, Role = Role.Escort, Name = stage.EscortName, Glyph = new Message("escort.glyph"), Pos = es.Start,
                    Hp = es.Hp, MaxHp = es.Hp, Def = es.Def, Mov = es.Mov,
                });
            foreach (var a in formation ?? stage.Sample)
                if (stage.Units.Contains(a.Kind) && UnitAt(new Hex(a.Col, a.Row)) == null) units.Add(MakeAlly(a.Kind, new Hex(a.Col, a.Row)));
            TrimToCaps();
        }

        // The allies the player placed; the escort is not one of them (docs/systems/goals.md 1-2).
        public IEnumerable<Unit> Allies => units.Where(u => u.Role == Role.Ally);
        public Unit Escort => units.FirstOrDefault(u => u.Role == Role.Escort);
        public IEnumerable<Unit> Enemies => units.Where(u => u.Side == Side.Enemy);
        public Unit Vampire => units.FirstOrDefault(u => u.Role == Role.Vampire);
        public Unit UnitAt(Hex h) => units.FirstOrDefault(u => u.Pos == h);
        public Corpse CorpseAt(Hex h) => corpses.FirstOrDefault(c => c.Pos == h);
        public Unit Find(int id) => units.FirstOrDefault(u => u.Id == id);
        public int Used(Func<UnitType, int> cost) => Allies.Sum(u => cost(GameData.Type(u.Type)));
        public bool BonusMet => Stage.Bonus.IsMet(Turn, Stats.Drained, units.Any(u => u.Role == Role.Minion) || pending.Count > 0);

        public Occupant OccupantAt(Hex h)
        {
            var u = UnitAt(h);
            if (u != null) return u.Side == Side.Ally ? Occupant.Ally : Occupant.Enemy;
            return CorpseAt(h) != null ? Occupant.Corpse : Occupant.Empty;
        }

        public Dictionary<Hex, int> Reach(Unit u) =>
            Board.Reachable(u.Pos, u.Mov, u.Side, OccupantAt).ToDictionary(r => r.hex, r => r.steps);

        public bool InDeploy(Hex h) => Board.InBounds(h) && Board.At(h) == Terrain.Deploy;

        Unit MakeAlly(string kind, Hex at)
        {
            var t = GameData.Type(kind);
            var u = new Unit
            {
                Id = nextId++, Role = Role.Ally, Type = kind, Name = t.Name, Glyph = t.Glyph, Pos = at,
                Hp = t.Hp, MaxHp = t.Hp, Atk = t.Atk, Def = t.Def, Mov = t.Mov, Rng = t.Rng, Bane = t.Bane, Heal = t.Heal,
            };
            u.abil.AddRange(t.Skills);
            foreach (var up in GameData.Branches(kind))
            {
                if (!upgrades.Contains(up.Id)) continue;
                u.Hp += up.Mod.Hp; u.MaxHp += up.Mod.Hp; u.Atk += up.Mod.Atk; u.Def += up.Mod.Def;
                u.Mov += up.Mod.Mov; u.Rng += up.Mod.Rng; u.Bane += up.Mod.Bane; u.Heal += up.Mod.Heal;
                if (up.Ability != Ability.None) u.abil.Add(up.Ability);
            }
            return u;
        }

        Unit MakeEnemy(string kind, Hex at)
        {
            if (kind == EnemyType.Vampire)
            {
                var b = Stage.Boss;
                return new Unit
                {
                    Id = nextId++, Role = Role.Vampire, Name = Stage.BossName, Glyph = new Message("vampire.glyph"), Pos = at,
                    Hp = b.Hp, MaxHp = b.Hp, Atk = b.Atk, Def = b.Def, Mov = b.Mov, Rng = 1, Heal = b.Heal, Asleep = true,
                };
            }
            var s = GameData.Minion(kind);
            return new Unit
            {
                Id = nextId++, Role = Role.Minion, Minion = kind, Name = s.Name, Glyph = s.Glyph, Pos = at,
                Hp = s.Hp, MaxHp = s.Hp, Atk = s.Atk, Def = s.Def, Mov = s.Mov, Rng = s.Rng,
            };
        }

        // Base type plus every owned upgrade, used by the roster and armory lines.
        public static Unit Preview(string kind, IEnumerable<string> ownedUpgrades) =>
            new Battle(GameData.Stages[0], ownedUpgrades).MakeAlly(kind, default);

        void TrimToCaps()
        {
            while (Used(t => t.Pop) > Stage.Pop || Used(t => t.Silver) > Stage.Silver)
                units.Remove(Allies.Last());
        }

        void Emit(BattleEvent e) => events.Add(e);

        void Say(Message text, string style = "")
        {
            log.Insert(0, new LogEntry(text, style));
            if (log.Count > LogCap) log.RemoveAt(log.Count - 1);
            Emit(BattleEvent.Log(text, style));
        }

        static Message PhaseName(Phase p) => new Message("phase." + p.ToString().ToLowerInvariant());

        bool Begin(Phase phase, out CommandResult rejected)
        {
            events.Clear();
            rejected = Phase == phase ? default : CommandResult.Rejected(new Message("reason.notPhase", PhaseName(phase)));
            return Phase == phase;
        }

        CommandResult Done() => CommandResult.Done(events);

        public CommandResult Place(string kind, Hex at)
        {
            if (!Begin(Phase.Deploy, out var no)) return no;
            if (!Stage.Units.Contains(kind)) return CommandResult.Rejected(new Message("reason.typeNotAllowed"));
            if (!InDeploy(at) || UnitAt(at) != null) return CommandResult.Rejected(new Message("reason.cannotPlace"));
            var t = GameData.Type(kind);
            if (Used(x => x.Pop) + t.Pop > Stage.Pop) return CommandResult.Rejected(new Message("reason.noPop"));
            if (Used(x => x.Silver) + t.Silver > Stage.Silver) return CommandResult.Rejected(new Message("reason.noSilver"));
            units.Add(MakeAlly(kind, at));
            return Done();
        }

        public CommandResult Recall(int id)
        {
            if (!Begin(Phase.Deploy, out var no)) return no;
            var u = Find(id);
            if (u == null || u.Role != Role.Ally) return CommandResult.Rejected(new Message("reason.noRecall"));
            units.Remove(u);
            return Done();
        }

        public CommandResult Relocate(int id, Hex to)
        {
            if (!Begin(Phase.Deploy, out var no)) return no;
            var u = Find(id);
            if (u == null || u.Role != Role.Ally) return CommandResult.Rejected(new Message("reason.noRelocate"));
            if (!InDeploy(to))
            {
                units.Remove(u);
                Emit(BattleEvent.Hint(new Message("hint.recalled", u.Name)));
                return Done();
            }
            var o = UnitAt(to);
            if (o == null) u.Pos = to;
            else if (o.Role == Role.Ally && o != u) { var p = u.Pos; u.Pos = o.Pos; o.Pos = p; }
            return Done();
        }

        public CommandResult ClearAllies()
        {
            if (!Begin(Phase.Deploy, out var no)) return no;
            units.RemoveAll(u => u.Role == Role.Ally);
            return Done();
        }

        // Unlike the constructor this does not filter by stage.Units: the sample is the stage's own, so it is trusted.
        public CommandResult UseSample()
        {
            if (!Begin(Phase.Deploy, out var no)) return no;
            units.RemoveAll(u => u.Role == Role.Ally);
            foreach (var a in Stage.Sample)
                if (UnitAt(new Hex(a.Col, a.Row)) == null) units.Add(MakeAlly(a.Kind, new Hex(a.Col, a.Row)));
            TrimToCaps();
            return Done();
        }

        public CommandResult Start()
        {
            if (!Begin(Phase.Deploy, out var no)) return no;
            if (!Allies.Any()) return CommandResult.Rejected(new Message("reason.noAllies"));
            var count = Allies.GroupBy(u => u.Type).ToDictionary(g => g.Key, g => g.Count());
            var index = new Dictionary<string, int>();
            foreach (var u in Allies)
            {
                if (count[u.Type] <= 1) continue;
                index[u.Type] = index.TryGetValue(u.Type, out var n) ? n + 1 : 1;
                u.Name = new Message("name.numbered", GameData.Type(u.Type).Name, index[u.Type]);
            }
            StartFormation = Allies.Select(u => new Placement(u.Type, u.Pos.Col, u.Pos.Row)).ToArray();
            Phase = Phase.Player;
            Say(Stage.Goal == Goal.Boss ? new Message("log.start", Stage.BossName) : new Message("log.startGoal", Stage.Objective), "t");
            Emit(BattleEvent.Banner(BannerKind.PlayerTurn, new Message("banner.playerTurn", 1)));
            return Done();
        }

        Unit Ready(int id)
        {
            var u = Find(id);
            return u != null && u.Side == Side.Ally && !u.Acted ? u : null;
        }

        public CommandResult Move(int id, Hex to)
        {
            if (!Begin(Phase.Player, out var no)) return no;
            var u = Ready(id);
            if (u == null || u.Moved) return CommandResult.Rejected(new Message("reason.cannotMove"));
            var reach = Reach(u);
            if (to == u.Pos || OccupantAt(to) != Occupant.Empty || !reach.TryGetValue(to, out var steps))
                return CommandResult.Rejected(new Message("reason.cannotReach"));
            u.MovedDist = steps;
            u.Moved = true;
            Walk(u, to);
            Fire(t => t.Kind == TriggerKind.Reach && t.Hexes.Contains(to));
            CheckEnd();
            return Done();
        }

        public CommandResult Attack(int id, int targetId)
        {
            if (!Begin(Phase.Player, out var no)) return no;
            var u = Ready(id);
            var t = Find(targetId);
            if (u == null || t == null || t.Side != Side.Enemy || t.Asleep || Hex.Distance(u.Pos, t.Pos) > u.Rng)
                return CommandResult.Rejected(new Message("reason.cannotAttack"));
            ApplyHit(u, t);
            u.Acted = u.Moved = true;
            if (CheckEnd()) return Done();
            if (!units.Contains(t) && u.Has(Ability.Cleave) && !u.ActedAgain)
            {
                u.ActedAgain = true;
                u.Acted = false;
                Say(new Message("log.cleave", u.Name), "h");
            }
            return Done();
        }

        public CommandResult Execute(int id, int targetId)
        {
            if (!Begin(Phase.Player, out var no)) return no;
            var u = Ready(id);
            var t = Find(targetId);
            if (u == null || t == null || t.Role != Role.Ally || t == u || Hex.Distance(u.Pos, t.Pos) > u.Rng)
                return CommandResult.Rejected(new Message("reason.cannotExecute"));
            Emit(BattleEvent.Popup(t.Id, PopupKind.Execute, new Message("popup.execute")));
            Kill(t, u, DeathCause.Execute);
            u.Acted = u.Moved = true;
            CheckEnd();
            return Done();
        }

        public bool CanBury(Unit u, Corpse c) => u != null && c != null && u.Side == Side.Ally && !u.Acted && u.Has(Ability.Bury) && Hex.Distance(u.Pos, c.Pos) == 1;

        // docs/content/units.md 4-3.
        public CommandResult Bury(int id, int corpseId)
        {
            if (!Begin(Phase.Player, out var no)) return no;
            var u = Ready(id);
            var c = corpses.FirstOrDefault(x => x.Id == corpseId);
            if (!CanBury(u, c)) return CommandResult.Rejected(new Message("reason.cannotBury"));
            corpses.Remove(c);
            Emit(BattleEvent.Buried(c.Id, c.Pos));
            Emit(BattleEvent.Popup(u.Id, PopupKind.Bury, new Message("popup.bury")));
            Say(new Message("log.bury", u.Name, c.Name), "h");
            u.Acted = u.Moved = true;
            if (u.Has(Ability.Rite) && Moon > 0)
            {
                Moon--;
                Emit(BattleEvent.MoonChanged(Moon));
                Say(new Message("log.rite", Moon, Rules.MoonMax), "h");
                if (!MoonNext()) EatTarget = null;
            }
            if (u.Has(Ability.Twice) && !u.ActedAgain)
            {
                u.ActedAgain = true;
                u.Acted = false;
                Say(new Message("log.buryAgain", u.Name), "h");
            }
            return Done();
        }

        public bool CanMend(Unit u, Unit t) => u != null && t != null && u != t && u.Side == Side.Ally && t.Side == Side.Ally && !u.Acted
            && u.Has(Ability.Mend) && t.Hp < t.MaxHp && Hex.Distance(u.Pos, t.Pos) <= Rules.MendRange;

        public int MendAmount(Unit u, Unit t) => Math.Min(u.Heal, t.MaxHp - t.Hp);

        public CommandResult Mend(int id, int targetId)
        {
            if (!Begin(Phase.Player, out var no)) return no;
            var u = Ready(id);
            var t = Find(targetId);
            if (!CanMend(u, t)) return CommandResult.Rejected(new Message("reason.cannotMend"));
            var heal = MendAmount(u, t);
            t.Hp += heal;
            Emit(BattleEvent.HpChanged(t.Id, t.Hp));
            Emit(BattleEvent.Popup(t.Id, PopupKind.Heal, new Message("popup.heal", heal)));
            Say(new Message("log.mend", u.Name, t.Name, heal), "h");
            u.Acted = u.Moved = true;
            return Done();
        }

        public CommandResult Wait(int id)
        {
            if (!Begin(Phase.Player, out var no)) return no;
            var u = Ready(id);
            if (u == null) return CommandResult.Rejected(new Message("reason.noWait"));
            u.Acted = u.Moved = true;
            return Done();
        }

        public CommandResult EndTurn()
        {
            if (!Begin(Phase.Player, out var no)) return no;
            EnemyPhase();
            return Done();
        }

        void Walk(Unit u, Hex to)
        {
            var path = Board.PathTo(u.Pos, to, u.Side, OccupantAt);
            Emit(BattleEvent.Walk(u.Id, path));
            if (path.Count > 0) u.Pos = path[path.Count - 1];
        }

        public (int damage, List<Message> tags) DamageParts(Unit att, Unit def)
        {
            var d = att.Atk;
            var tags = new List<Message>();
            if (att.Bane > 0 && def.IsUndead) { d += att.Bane; tags.Add(new Message("tag.bane")); }
            if (att.Has(Ability.Ambush) && !att.Moved) { d += 1; tags.Add(new Message("tag.ambush")); }
            if (att.Has(Ability.Charge) && att.MovedDist >= 2) { d += 2; tags.Add(new Message("tag.charge")); }
            var armor = def.Def;
            if (InFormation(def)) { armor += 1; tags.Add(new Message("tag.formation")); }
            return (Math.Max(1, d - armor), tags);
        }

        bool InFormation(Unit u) => u.Has(Ability.Formation)
            && units.Any(o => o != u && o.Role == Role.Ally && o.Type == u.Type && Hex.Distance(o.Pos, u.Pos) == 1);

        public int Damage(Unit att, Unit def) => DamageParts(att, def).damage;

        void ApplyHit(Unit att, Unit def)
        {
            var (d, tags) = DamageParts(att, def);
            def.Hp -= d;
            if (def.Stance == Stance.Guard) def.Stance = Stance.Charge;
            Emit(BattleEvent.Hit(def.Id, att.Id, d));
            Emit(BattleEvent.HpChanged(def.Id, def.Hp));
            Emit(BattleEvent.Popup(def.Id, PopupKind.Damage, new Message("popup.damage", d)));
            var line = tags.Count == 0
                ? new Message("log.hit", att.Name, def.Name, d)
                : new Message("log.hitTags", att.Name, def.Name, d, new Message("tags." + tags.Count, tags.Cast<object>().ToArray()));
            Say(line, att.Side == Side.Enemy ? "k" : "");
            if (def.Hp <= 0) Kill(def, att, DeathCause.Blow);
        }

        enum DeathCause { Blow, Execute, Drain }

        void Kill(Unit u, Unit killer, DeathCause cause)
        {
            Moon = Math.Min(Rules.MoonMax, Moon + 1);
            units.Remove(u);
            Emit(BattleEvent.Died(u.Id, u.Pos));
            if (u.Side == Side.Enemy)
            {
                if (u.Role == Role.Minion)
                {
                    MinionsKilled++;
                    Fire(t => t.Kind == TriggerKind.Kills && MinionsKilled >= t.Value);
                }
                Say(new Message("log.enemyFell", u.Name), u.Role == Role.Vampire ? "t" : "sys");
                return;
            }
            if (u.Role == Role.Escort)
            {
                Say(new Message("log.escortFell", u.Name), "k");
                return;
            }
            if (cause == DeathCause.Execute)
            {
                Stats.Executed++;
                Say(new Message("log.executed", killer.Name, u.Name), "h");
                return;
            }
            Stats.Lost++;
            if (!killer.IsUndead) { Say(new Message("log.diedToMinion", u.Name), "sys"); return; }
            if (Board.Lit(u.Pos)) { Say(new Message("log.diedInLight", u.Name), "h"); return; }
            var corpse = new Corpse
            {
                Id = nextId++, Pos = u.Pos, Name = u.Name, Type = u.Type, Glyph = u.Glyph,
                MaxHp = u.MaxHp, Atk = u.Atk, Def = u.Def, Mov = u.Mov,
            };
            corpses.Add(corpse);
            Emit(BattleEvent.CorpseLeft(corpse.Id, corpse.Pos));
            Say(new Message("log.willRise", u.Name), "s");
        }

        // The one place a battle is won or lost (docs/dev/battle-goals.md). Null while it goes on.
        // dawn: the enemy phase has just ended. A win is checked before a loss (docs/systems/goals.md 1-4).
        Ending? Outcome(bool dawn = false)
        {
            if (Vampire == null) return Core.Ending.Slain;
            var escort = Escort;
            var goal = Stage.Goal switch
            {
                Goal.Boss => null,
                Goal.Defense => dawn && Turn >= Stage.Hold ? Core.Ending.Held
                    : Stage.Spots.Any(h => UnitAt(h)?.Side == Side.Enemy) ? Core.Ending.Overrun : (Ending?)null,
                Goal.Escort => escort == null ? Core.Ending.EscortFell
                    : Stage.Spots.Contains(escort.Pos) ? Core.Ending.Arrived : (Ending?)null,
                _ => throw new ArgumentOutOfRangeException(nameof(Stage.Goal), Stage.Goal, null),
            };
            return goal ?? (Allies.Any() ? (Ending?)null : Core.Ending.Wiped);
        }

        bool CheckEnd(bool dawn = false)
        {
            if (!(Outcome(dawn) is Ending end)) return false;
            Phase = Phase.Over;
            Ending = end;
            Won = end == Core.Ending.Slain || end == Core.Ending.Held || end == Core.Ending.Arrived;
            return true;
        }

        public Message EndLine => Ending switch
        {
            Core.Ending.Slain => new Message("over.wonLine", Turn, Stage.BossName),
            Core.Ending.Held => new Message("over.held", Stage.Hold, Stage.Place),
            Core.Ending.Arrived => new Message("over.arrived", Turn, Stage.EscortName, Stage.Place),
            Core.Ending.Overrun => new Message("over.overrun", Stage.Place),
            Core.Ending.EscortFell => new Message("over.escortFell", Stage.EscortName),
            Core.Ending.Wiped => new Message("over.lostLine"),
            _ => default,
        };

        public string Snapshot(Func<Message, string> text)
        {
            static int B(bool v) => v ? 1 : 0;
            var won = Won.HasValue ? B(Won.Value).ToString() : "-";
            var lines = new List<string>
            {
                $"t={Turn} ph={Phase.ToString().ToLowerInvariant()} moon={Moon} eat={(EatTarget.HasValue ? EatTarget.Value.ToString() : "-")} won={won} st={Stats.Lost},{Stats.Executed},{Stats.Risen},{Stats.Drained}",
            };
            foreach (var u in units)
            {
                var flags = u.Side == Side.Ally
                    ? $"m{B(u.Moved)}a{B(u.Acted)}d{u.MovedDist}c{B(u.ActedAgain)}"
                    : $"s{B(u.Asleep)}r{B(u.JustRisen)}w{B(u.JustWoke)}";
                var side = u.Side == Side.Ally ? "ally" : "enemy";
                var abil = string.Join("+", u.abil.Select(a => a.ToString().ToLowerInvariant()));
                lines.Add($"U {u.Id}:{side}:{u.Kind}:{text(u.Name)}:{text(u.Glyph)}:{text(u.Origin)}:{u.Pos}:{u.Hp}/{u.MaxHp}:{u.Atk},{u.Def},{u.Mov},{u.Rng},{u.Bane},{u.Heal}:{abil}:{flags}");
            }
            foreach (var c in corpses)
                lines.Add($"C {c.Id}:{c.Type}:{text(c.Name)}:{text(c.Glyph)}:{c.Pos}:{c.MaxHp},{c.Atk},{c.Def},{c.Mov}");
            return string.Join("\n", lines);
        }
    }
}
