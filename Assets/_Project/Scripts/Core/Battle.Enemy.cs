using System;
using System.Collections.Generic;
using System.Linq;

namespace VampireSpawn.Core
{
    public sealed partial class Battle
    {
        static readonly Role[] ActingOrder = { Role.Minion, Role.Spawn, Role.Vampire };

        public bool MoonNext()
        {
            var v = Vampire;
            return v != null && !v.Asleep && Moon >= Rules.MoonMax;
        }

        // docs/systems/waves.md 2-3: due reinforcements appear before anyone acts and wait one phase.
        void Arrive()
        {
            Fire(t => t.Kind == TriggerKind.Turn && t.Value <= Turn);
            foreach (var d in Stage.Doors)
            {
                if (!fired.Contains(d.Trigger)) continue;
                foreach (var h in d.Hexes.Where(h => Board.At(h) == Terrain.Rock))
                {
                    Board.Open(h);
                    Emit(BattleEvent.Opened(h));
                }
            }
            foreach (var p in pending.Where(p => p.Wave != null ? fired.Contains(p.Wave) : p.Arrive <= Turn).ToList())
            {
                var at = ArrivalHex(new Hex(p.Col, p.Row));
                if (at == null) continue;
                pending.Remove(p);
                var e = Placed(p, at.Value);
                e.JustArrived = true;
                units.Add(e);
                Emit(BattleEvent.Arrived(e.Id, at.Value));
                Say(new Message("log.arrive", e.Name), "k");
            }
        }

        Hex? ArrivalHex(Hex h)
        {
            var near = Board.Neighbors(h);
            foreach (var c in new[] { h }.Concat(near).Concat(near.SelectMany(Board.Neighbors)).Distinct().OrderBy(c => Hex.Distance(h, c)))
                if (Board.Walkable(c) && OccupantAt(c) == Occupant.Empty) return c;
            return null;
        }

        void EnemyPhase()
        {
            Phase = Phase.Enemy;
            Say(new Message("log.enemyTurn", Turn), "t");
            Emit(BattleEvent.Banner(BannerKind.EnemyTurn, new Message("banner.enemyTurn", Turn)));
            Arrive();
            foreach (var c in corpses.ToList()) Rise(c);

            var v = Vampire;
            var full = MoonNext() && EatTarget.HasValue;
            if (full) Say(new Message("log.fullMoon"), "k");
            if (v != null && v.Asleep && (Moon >= Rules.MoonWake || units.All(u => u == v || u.Side != Side.Enemy)))
            {
                v.Asleep = false;
                v.JustWoke = true;
                Emit(BattleEvent.Woke(v.Id));
                Say(new Message("log.woke", v.Name), "k");
                Emit(BattleEvent.Banner(BannerKind.Coffin, new Message("banner.coffin")));
            }
            if (full)
            {
                FullMoonAct(v);
                if (CheckEnd()) return;
            }

            foreach (var kind in ActingOrder)
            {
                foreach (var e in units.Where(x => x.Role == kind).ToList())
                {
                    if (!units.Contains(e) || e.Asleep || e.JustWoke || e.JustArrived) continue;
                    if (kind == Role.Vampire && full) continue;
                    MeleeAct(e);
                    if (CheckEnd()) return;
                }
            }

            foreach (var u in units)
            {
                if (u.Side == Side.Enemy) { u.JustRisen = false; u.JustWoke = false; u.JustArrived = false; }
                else { u.Moved = false; u.Acted = false; u.MovedDist = 0; u.ActedAgain = false; }
            }
            if (CheckEnd(dawn: true)) return;
            Turn++;
            Phase = Phase.Player;
            Say(new Message("log.playerTurn", Turn), "t");
            Emit(BattleEvent.Banner(BannerKind.PlayerTurn, new Message("banner.playerTurn", Turn)));
            FixEatTarget();
            OpenReinforcements();
        }

        void Rise(Corpse c)
        {
            corpses.Remove(c);
            var spawn = new Unit
            {
                Id = nextId++, Role = Role.Spawn, Type = c.Type, Name = new Message("name.spawn", c.Name), Glyph = c.Glyph,
                Pos = c.Pos, Hp = c.MaxHp, MaxHp = c.MaxHp, Atk = c.Atk, Def = 0, Mov = c.Mov, Rng = 1,
                JustRisen = true, Origin = c.Name,
            };
            units.Add(spawn);
            Emit(BattleEvent.Rose(spawn.Id, c.Id, spawn.Pos));
            Stats.Risen++;
            Say(new Message("log.rose", c.Name), "s");
        }

        Unit PickEat(Unit v)
        {
            Unit best = null;
            var bestScore = 0;
            foreach (var u in Targets)
            {
                var d = Hex.Distance(v.Pos, u.Pos);
                if (d > Rules.EatRange || Board.Lit(u.Pos) || u.Has(Ability.Ward)) continue;
                var score = d * 100 - Pop(u);
                if (best == null || score < bestScore) { best = u; bestScore = score; }
            }
            return best;
        }

        // docs/systems/full-moon.md 5: also right after a kill fills the moon mid-turn.
        void FixEatTarget()
        {
            if (Phase != Phase.Player || EatTarget.HasValue || !MoonNext()) return;
            var v = Vampire;
            var t = PickEat(v);
            if (t == null)
            {
                if (Moon >= Rules.MoonMax)
                    Say(new Message("log.moonNoTarget", v.Name), "sys");
                return;
            }
            EatTarget = t.Id;
            Say(new Message("log.moonTarget", v.Name, t.Name), "k");
        }

        void FullMoonAct(Unit v)
        {
            var t = EatTarget.HasValue ? Find(EatTarget.Value) : null;
            void MoonSet() { Moon = 0; EatTarget = null; Say(new Message("log.moonSet"), "sys"); }
            Emit(BattleEvent.Banner(BannerKind.FullMoon, new Message("banner.fullMoon")));
            if (t != null && Hex.Distance(v.Pos, t.Pos) <= Rules.EatRange && !Board.Lit(t.Pos))
            {
                Say(new Message("log.drained", v.Name, t.Name), "k");
                Emit(BattleEvent.Popup(t.Id, PopupKind.Drain, new Message("popup.drain")));
                Emit(BattleEvent.Hit(t.Id, v.Id));
                Stats.Drained++;
                Kill(t, v, DeathCause.Drain);
                v.Hp = Math.Min(v.MaxHp, v.Hp + v.Heal);
                Emit(BattleEvent.HpChanged(v.Id, v.Hp));
                Emit(BattleEvent.Popup(v.Id, PopupKind.Feed, new Message("popup.heal", v.Heal)));
                MoonSet();
                return;
            }
            var why = t == null ? new Message("why.gone")
                : Board.Lit(t.Pos) ? new Message("why.lit", t.Name)
                : new Message("why.far", t.Name);
            Say(new Message("log.drainFailed", why, v.Name), "h");
            Emit(BattleEvent.Popup(v.Id, PopupKind.DrainFailed, new Message("popup.drainFailed")));
            MoonSet();
            MeleeAct(v);
        }

        double TargetScore(Unit att, Unit t)
        {
            var d = Damage(att, t);
            return (d >= t.Hp ? 100 : 0) + Pop(t) * 3 - t.Hp * 0.5 + d;
        }

        // docs/systems/goals.md 4-3: the escort is a target like any ally, worth no population.
        IEnumerable<Unit> Targets => units.Where(u => u.Side == Side.Ally);

        static int Pop(Unit u) => u.Role == Role.Ally ? GameData.Type(u.Type).Pop : 0;

        Unit PickTarget(Unit att, List<Unit> list)
        {
            // docs/content/units.md: an enemy that can reach a taunting unit strikes it and nothing else.
            var wall = list.Where(t => t.Has(Ability.Taunt)).ToList();
            if (wall.Count > 0) list = wall;
            var best = list[0];
            foreach (var t in list.Skip(1))
                if (TargetScore(att, t) > TargetScore(att, best)) best = t;
            return best;
        }

        void HitBest(Unit e, List<Unit> adj)
        {
            var t = PickTarget(e, adj);
            ApplyHit(e, t, Why(e, adj, t));
        }

        // docs/systems/ai.md 6: why t was picked, for the log line only. It never feeds back into PickTarget.
        Message Why(Unit att, List<Unit> list, Unit t)
        {
            if (list.Count < 2) return default;
            var wall = list.Where(x => x.Has(Ability.Taunt)).ToList();
            if (wall.Count > 0 && wall.Count < list.Count) return new Message("tag.taunt");
            var others = list.Where(x => x != t).ToList();
            if (Damage(att, t) >= t.Hp) return new Message("tag.lethal");
            if (others.All(x => Pop(x) < Pop(t))) return new Message("tag.highValue");
            if (others.All(x => x.Hp > t.Hp)) return new Message("tag.wounded");
            return default;
        }

        // docs/systems/ai.md 2: allies within range and in line of sight.
        List<Unit> AlliesInReach(Unit e, Hex h) => Targets.Where(a => InRange(e, h, a.Pos)).ToList();

        void MeleeAct(Unit e)
        {
            if (!Targets.Any()) return;
            // docs/systems/triggers.md 2: a guard wakes for good once an ally comes within its alert range.
            if (e.Stance == Stance.Guard)
            {
                if (!Targets.Any(a => Hex.Distance(a.Pos, e.Pos) <= e.Alert)) return;
                e.Stance = Stance.Charge;
            }
            var adj = AlliesInReach(e, e.Pos);
            if (e.Stance == Stance.Hold)
            {
                if (adj.Count > 0) HitBest(e, adj);
                return;
            }
            var reach = Board.Reachable(e.Pos, e.Mov, e.Side, OccupantAt);
            // docs/systems/goals.md 4-1: on a defence stage an enemy first takes a defended hex it can reach.
            var spot = Stage.Goal != Goal.Defense ? null : reach.Where(r => Stage.Spots.Contains(r.hex) && UnitAt(r.hex) == null)
                .OrderBy(r => r.steps).Select(r => (Hex?)r.hex).FirstOrDefault();
            if (spot.HasValue)
            {
                if (MoveEnemy(e, spot.Value) || !units.Contains(e)) return;
                adj = AlliesInReach(e, e.Pos);
            }
            else if (adj.Count == 0)
            {
                Hex? best = null;
                var bestScore = -1e9;
                foreach (var (h, steps) in reach)
                {
                    var near = AlliesInReach(e, h);
                    if (near.Count == 0) continue;
                    var s = TargetScore(e, PickTarget(e, near)) * 10 - steps;
                    if (s > bestScore) { bestScore = s; best = h; }
                }
                if (best == null)
                {
                    var goals = new List<Hex>();
                    foreach (var a in Targets)
                        foreach (var n in Board.Neighbors(a.Pos))
                            if (!Board.Blocked(n, Side.Enemy, OccupantAt) && UnitAt(n) == null) goals.Add(n);
                    if (Stage.Goal == Goal.Defense)
                        goals.AddRange(Stage.Spots.Where(h => !Board.Blocked(h, Side.Enemy, OccupantAt) && UnitAt(h) == null));
                    var field = Board.DistField(goals, Side.Enemy, OccupantAt);
                    var bestDist = field.TryGetValue(e.Pos, out var here) ? here : 1_000_000_000;
                    var bestSteps = 0;
                    foreach (var (h, steps) in reach)
                    {
                        if (!field.TryGetValue(h, out var d)) continue;
                        if (d < bestDist || (d == bestDist && steps < bestSteps)) { bestDist = d; bestSteps = steps; best = h; }
                    }
                }
                if (best.HasValue && best.Value != e.Pos && MoveEnemy(e, best.Value)) return;
                if (!units.Contains(e)) return;
                adj = AlliesInReach(e, e.Pos);
            }
            if (adj.Count > 0) HitBest(e, adj);
        }

        // True when a trap on the way stopped it (it then does not attack this phase).
        bool MoveEnemy(Unit e, Hex to)
        {
            var trap = Board.PathTo(e.Pos, to, e.Side, OccupantAt).Where(h => h != e.Pos && UnitAt(h) == null).Select(TrapAt).FirstOrDefault(t => t != null);
            Walk(e, trap != null ? trap.Pos : to);
            if (trap != null) Spring(trap, e);
            foreach (var a in Allies.ToList())
            {
                if (!units.Contains(e)) break;
                if (!a.Has(Ability.Pike) || Hex.Distance(a.Pos, e.Pos) != 1) continue;
                Hurt(e, 1, null);
                Say(new Message("log.pike", a.Name, e.Name), "h");
                if (e.Hp <= 0) { Kill(e, a, DeathCause.Blow); break; }
            }
            return trap != null;
        }
    }
}
