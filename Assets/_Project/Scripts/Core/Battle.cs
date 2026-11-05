using System;
using System.Collections.Generic;
using System.Linq;

namespace VampireSpawn.Core
{
    public enum Phase { Deploy, Player, Enemy, Over }

    public sealed class Unit
    {
        public int Id;
        public Side Side;
        public string Kind;
        public UnitKind? Type;
        public string Name, Glyph, Origin;
        public Hex Pos;
        public int Hp, MaxHp, Atk, Def, Mov, Rng, Bane, Heal, MovedDist;
        public readonly List<Ability> Abil = new List<Ability>();
        public bool Moved, Acted, Cleaved, Asleep, JustRisen, JustWoke;

        public bool IsUndead => Kind == "vampire" || Kind == "spawn";
        public bool Has(Ability a) => Abil.Contains(a);
    }

    public sealed class Corpse
    {
        public int Id;
        public Hex Pos;
        public string Name, Glyph;
        public UnitKind Type;
        public int MaxHp, Atk, Def, Mov;
    }

    public sealed class BattleStats
    {
        public int Lost, Executed, Risen, Drained;
    }

    public readonly struct LogEntry
    {
        public readonly string Text, Style;
        public LogEntry(string text, string style) { Text = text; Style = style; }
    }

    public sealed partial class Battle
    {
        const int LogCap = 80;

        public readonly Stage Stage;
        public readonly Board Board;
        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<Corpse> Corpses = new List<Corpse>();
        public readonly List<LogEntry> Log = new List<LogEntry>();
        public readonly BattleStats Stats = new BattleStats();
        public Phase Phase { get; private set; } = Phase.Deploy;
        public int Turn { get; private set; } = 1;
        public int Moon { get; private set; }
        public int? EatTarget { get; private set; }
        public bool? Won { get; private set; }

        readonly HashSet<string> upgrades;
        readonly List<BattleEvent> events = new List<BattleEvent>();
        int nextId = 1;

        public Battle(Stage stage, IEnumerable<string> ownedUpgrades = null, IEnumerable<Placement<UnitKind>> formation = null)
        {
            Stage = stage;
            Board = new Board(stage.Map);
            upgrades = new HashSet<string>(ownedUpgrades ?? Array.Empty<string>());
            foreach (var e in stage.Enemies) Units.Add(MakeEnemy(e.Kind, new Hex(e.Col, e.Row)));
            foreach (var a in formation ?? stage.Sample)
                if (stage.Units.Contains(a.Kind)) Units.Add(MakeAlly(a.Kind, new Hex(a.Col, a.Row)));
            TrimToCaps();
        }

        public IEnumerable<Unit> Allies => Units.Where(u => u.Side == Side.Ally);
        public IEnumerable<Unit> Enemies => Units.Where(u => u.Side == Side.Enemy);
        public Unit Vampire => Units.FirstOrDefault(u => u.Kind == "vampire");
        public Unit UnitAt(Hex h) => Units.FirstOrDefault(u => u.Pos == h);
        public Corpse CorpseAt(Hex h) => Corpses.FirstOrDefault(c => c.Pos == h);
        public Unit Find(int id) => Units.FirstOrDefault(u => u.Id == id);
        public int Used(Func<UnitType, int> cost) => Allies.Sum(u => cost(GameData.Type(u.Type.Value)));

        public Occupant OccupantAt(Hex h)
        {
            var u = UnitAt(h);
            if (u != null) return u.Side == Side.Ally ? Occupant.Ally : Occupant.Enemy;
            return CorpseAt(h) != null ? Occupant.Corpse : Occupant.Empty;
        }

        public Dictionary<Hex, int> Reach(Unit u) =>
            Board.Reachable(u.Pos, u.Mov, u.Side, OccupantAt).ToDictionary(r => r.hex, r => r.steps);

        public bool InDeploy(Hex h) => Board.InBounds(h) && Board.At(h) == Terrain.Deploy;

        Unit MakeAlly(UnitKind kind, Hex at)
        {
            var t = GameData.Type(kind);
            var u = new Unit
            {
                Id = nextId++, Side = Side.Ally, Kind = t.Key, Type = kind, Name = t.Name, Glyph = t.Glyph, Pos = at,
                Hp = t.Hp, MaxHp = t.Hp, Atk = t.Atk, Def = t.Def, Mov = t.Mov, Rng = t.Rng, Bane = t.Bane,
            };
            foreach (var up in GameData.Upgrades)
            {
                if (up.Unit != kind || !upgrades.Contains(up.Id)) continue;
                u.Hp += up.Mod.Hp; u.MaxHp += up.Mod.Hp; u.Atk += up.Mod.Atk; u.Def += up.Mod.Def;
                u.Mov += up.Mod.Mov; u.Rng += up.Mod.Rng; u.Bane += up.Mod.Bane;
                if (up.Ability != Ability.None) u.Abil.Add(up.Ability);
            }
            return u;
        }

        Unit MakeEnemy(EnemyKind kind, Hex at)
        {
            if (kind == EnemyKind.Vampire)
            {
                var b = Stage.Boss;
                return new Unit
                {
                    Id = nextId++, Side = Side.Enemy, Kind = "vampire", Name = b.Name, Glyph = "흡", Pos = at,
                    Hp = b.Hp, MaxHp = b.Hp, Atk = b.Atk, Def = b.Def, Mov = b.Mov, Rng = 1, Heal = b.Heal, Asleep = true,
                };
            }
            var s = GameData.Skeleton;
            return new Unit
            {
                Id = nextId++, Side = Side.Enemy, Kind = "skeleton", Name = s.Name, Glyph = s.Glyph, Pos = at,
                Hp = s.Hp, MaxHp = s.Hp, Atk = s.Atk, Def = s.Def, Mov = s.Mov, Rng = 1,
            };
        }

        void TrimToCaps()
        {
            while (Used(t => t.Pop) > Stage.Pop || Used(t => t.Silver) > Stage.Silver)
                Units.Remove(Allies.Last());
        }

        void Emit(BattleEvent e) => events.Add(e);

        void Say(string text, string style = "")
        {
            Log.Insert(0, new LogEntry(text, style));
            if (Log.Count > LogCap) Log.RemoveAt(Log.Count - 1);
            Emit(BattleEvent.Log(text, style));
        }

        CommandResult Begin(Phase phase)
        {
            events.Clear();
            return Phase == phase ? default : CommandResult.Rejected("지금은 " + phase + " 단계가 아니다");
        }

        CommandResult Done() => CommandResult.Done(events);

        public CommandResult Place(UnitKind kind, Hex at)
        {
            var bad = Begin(Phase.Deploy);
            if (bad.Reason != null) return bad;
            if (!Stage.Units.Contains(kind)) return CommandResult.Rejected("이 장에서 쓸 수 없는 병종이다");
            if (!InDeploy(at) || UnitAt(at) != null) return CommandResult.Rejected("배치할 수 없는 칸이다");
            var t = GameData.Type(kind);
            if (Used(x => x.Pop) + t.Pop > Stage.Pop) return CommandResult.Rejected("인구가 부족합니다. 배치한 유닛을 구역 밖으로 끌어내 회수하세요.");
            if (Used(x => x.Silver) + t.Silver > Stage.Silver) return CommandResult.Rejected("은이 부족합니다.");
            Units.Add(MakeAlly(kind, at));
            return Done();
        }

        public CommandResult Recall(int id)
        {
            var bad = Begin(Phase.Deploy);
            if (bad.Reason != null) return bad;
            var u = Find(id);
            if (u == null || u.Side != Side.Ally) return CommandResult.Rejected("회수할 아군이 없다");
            Units.Remove(u);
            return Done();
        }

        public CommandResult Relocate(int id, Hex to)
        {
            var bad = Begin(Phase.Deploy);
            if (bad.Reason != null) return bad;
            var u = Find(id);
            if (u == null || u.Side != Side.Ally) return CommandResult.Rejected("옮길 아군이 없다");
            if (!InDeploy(to)) { Units.Remove(u); return Done(); }
            var o = UnitAt(to);
            if (o == null) u.Pos = to;
            else if (o.Side == Side.Ally && o != u) { var p = u.Pos; u.Pos = o.Pos; o.Pos = p; }
            return Done();
        }

        public CommandResult ClearAllies()
        {
            var bad = Begin(Phase.Deploy);
            if (bad.Reason != null) return bad;
            Units.RemoveAll(u => u.Side == Side.Ally);
            return Done();
        }

        public CommandResult UseSample()
        {
            var bad = Begin(Phase.Deploy);
            if (bad.Reason != null) return bad;
            Units.RemoveAll(u => u.Side == Side.Ally);
            foreach (var a in Stage.Sample) Units.Add(MakeAlly(a.Kind, new Hex(a.Col, a.Row)));
            TrimToCaps();
            return Done();
        }

        public CommandResult Start()
        {
            var bad = Begin(Phase.Deploy);
            if (bad.Reason != null) return bad;
            if (!Allies.Any()) return CommandResult.Rejected("배치한 아군이 없다");
            var count = Allies.GroupBy(u => u.Kind).ToDictionary(g => g.Key, g => g.Count());
            var index = new Dictionary<string, int>();
            foreach (var u in Allies)
            {
                if (count[u.Kind] <= 1) continue;
                index[u.Kind] = index.TryGetValue(u.Kind, out var n) ? n + 1 : 1;
                u.Name = GameData.Type(u.Type.Value).Name + " " + index[u.Kind];
            }
            Phase = Phase.Player;
            Say("전투 개시. 목표: " + Korean.Josa(Stage.Boss.Name, "을", "를") + " 처치하라.", "t");
            Emit(BattleEvent.Banner("1턴 · 아군 페이즈", "player"));
            return Done();
        }

        Unit Ready(int id)
        {
            var u = Find(id);
            return u != null && u.Side == Side.Ally && !u.Acted ? u : null;
        }

        public CommandResult Move(int id, Hex to)
        {
            var bad = Begin(Phase.Player);
            if (bad.Reason != null) return bad;
            var u = Ready(id);
            if (u == null || u.Moved) return CommandResult.Rejected("움직일 수 없는 유닛이다");
            var reach = Reach(u);
            if (to == u.Pos || OccupantAt(to) != Occupant.Empty || !reach.TryGetValue(to, out var steps))
                return CommandResult.Rejected("갈 수 없는 칸이다");
            u.MovedDist = steps;
            u.Moved = true;
            Walk(u, to);
            return Done();
        }

        public CommandResult Attack(int id, int targetId)
        {
            var bad = Begin(Phase.Player);
            if (bad.Reason != null) return bad;
            var u = Ready(id);
            var t = Find(targetId);
            if (u == null || t == null || t.Side != Side.Enemy || t.Asleep || Hex.Distance(u.Pos, t.Pos) > u.Rng)
                return CommandResult.Rejected("공격할 수 없다");
            ApplyHit(u, t);
            u.Acted = u.Moved = true;
            if (!Units.Contains(t) && u.Has(Ability.Cleave) && !u.Cleaved && Vampire != null)
            {
                u.Cleaved = true;
                u.Acted = false;
                Say(Korean.Josa(u.Name, "이", "가") + " 베어 넘기고 한 번 더 공격할 수 있다.", "h");
            }
            CheckEnd();
            return Done();
        }

        public CommandResult Execute(int id, int targetId)
        {
            var bad = Begin(Phase.Player);
            if (bad.Reason != null) return bad;
            var u = Ready(id);
            var t = Find(targetId);
            if (u == null || t == null || t.Side != Side.Ally || t == u || Hex.Distance(u.Pos, t.Pos) > u.Rng)
                return CommandResult.Rejected("처치할 수 없다");
            Emit(BattleEvent.Popup(t.Id, "처치", "heal"));
            Kill(t, u, DeathCause.Execute);
            u.Acted = u.Moved = true;
            CheckEnd();
            return Done();
        }

        public CommandResult Wait(int id)
        {
            var bad = Begin(Phase.Player);
            if (bad.Reason != null) return bad;
            var u = Ready(id);
            if (u == null) return CommandResult.Rejected("대기할 유닛이 없다");
            u.Acted = u.Moved = true;
            return Done();
        }

        public CommandResult EndTurn()
        {
            var bad = Begin(Phase.Player);
            if (bad.Reason != null) return bad;
            EnemyPhase();
            return Done();
        }

        void Walk(Unit u, Hex to)
        {
            var path = Board.PathTo(u.Pos, to, u.Side, OccupantAt);
            Emit(BattleEvent.Walk(u.Id, path));
            if (path.Count > 0) u.Pos = path[path.Count - 1];
        }

        public (int damage, List<string> tags) DamageParts(Unit att, Unit def)
        {
            var d = att.Atk;
            var tags = new List<string>();
            if (att.Bane > 0 && def.IsUndead) { d += att.Bane; tags.Add("대뱀파이어"); }
            if (att.Has(Ability.Ambush) && !att.Moved) { d += 1; tags.Add("매복"); }
            if (att.Has(Ability.Charge) && att.MovedDist >= 2) { d += 2; tags.Add("돌격"); }
            return (Math.Max(1, d - def.Def), tags);
        }

        public int Damage(Unit att, Unit def) => DamageParts(att, def).damage;

        void ApplyHit(Unit att, Unit def)
        {
            var (d, tags) = DamageParts(att, def);
            def.Hp -= d;
            Emit(BattleEvent.Hit(def.Id));
            Emit(BattleEvent.Popup(def.Id, "-" + d, "dmg"));
            Say(att.Name + " → " + def.Name + " " + d + " 피해" + (tags.Count > 0 ? " (" + string.Join(", ", tags) + ")" : ""),
                att.Side == Side.Enemy ? "k" : "");
            if (def.Hp <= 0) Kill(def, att, DeathCause.Blow);
        }

        enum DeathCause { Blow, Execute, Drain }

        void Kill(Unit u, Unit killer, DeathCause cause)
        {
            Moon = Math.Min(Rules.MoonMax, Moon + 1);
            Units.Remove(u);
            if (u.Side == Side.Enemy)
            {
                Say(Korean.Josa(u.Name, "이", "가") + " 쓰러졌다.", u.Kind == "vampire" ? "t" : "sys");
                return;
            }
            if (cause == DeathCause.Execute)
            {
                Stats.Executed++;
                Say(Korean.Josa(killer.Name, "이", "가") + " " + Korean.Josa(u.Name, "을", "를") + " 처치했다. 변이 없이 잠든다.", "h");
                return;
            }
            Stats.Lost++;
            if (!killer.IsUndead) { Say(u.Name + " 전사. 하수인에게 쓰러져 변이하지 않는다.", "sys"); return; }
            if (Board.Lit(u.Pos)) { Say(u.Name + " 전사. 빛 속에서 쓰러져 변이하지 않는다.", "h"); return; }
            Corpses.Add(new Corpse
            {
                Id = nextId++, Pos = u.Pos, Name = u.Name, Type = u.Type.Value, Glyph = u.Glyph,
                MaxHp = u.MaxHp, Atk = u.Atk, Def = u.Def, Mov = u.Mov,
            });
            Say(Korean.Josa(u.Name, "이", "가") + " 쓰러졌다. 다음 적 페이즈에 스폰으로 일어난다!", "s");
        }

        bool CheckEnd()
        {
            if (Vampire == null) { Finish(true); return true; }
            if (!Allies.Any()) { Finish(false); return true; }
            return false;
        }

        void Finish(bool win)
        {
            Phase = Phase.Over;
            Won = win;
        }

        public string Snapshot()
        {
            static int B(bool v) => v ? 1 : 0;
            var lines = new List<string>
            {
                $"t={Turn} ph={Phase.ToString().ToLowerInvariant()} moon={Moon} eat={(EatTarget.HasValue ? EatTarget.Value.ToString() : "-")} st={Stats.Lost},{Stats.Executed},{Stats.Risen},{Stats.Drained}",
            };
            foreach (var u in Units)
            {
                var flags = u.Side == Side.Ally
                    ? $"m{B(u.Moved)}a{B(u.Acted)}d{u.MovedDist}c{B(u.Cleaved)}"
                    : $"s{B(u.Asleep)}r{B(u.JustRisen)}w{B(u.JustWoke)}";
                var side = u.Side == Side.Ally ? "ally" : "enemy";
                var abil = string.Join("+", u.Abil.Select(a => a.ToString().ToLowerInvariant()));
                lines.Add($"U {u.Id}:{side}:{u.Kind}:{u.Name}:{u.Pos}:{u.Hp}/{u.MaxHp}:{u.Atk},{u.Def},{u.Mov},{u.Rng},{u.Bane},{u.Heal}:{abil}:{flags}");
            }
            foreach (var c in Corpses)
                lines.Add($"C {c.Id}:{GameData.Type(c.Type).Key}:{c.Name}:{c.Pos}:{c.MaxHp},{c.Atk},{c.Def},{c.Mov}");
            return string.Join("\n", lines);
        }
    }
}
