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
            return v != null && !v.Asleep && Moon + 1 >= Rules.MoonMax;
        }

        void EnemyPhase()
        {
            Phase = Phase.Enemy;
            Say(new Message("log.enemyTurn", Turn), "t");
            Emit(BattleEvent.Banner(BannerKind.EnemyTurn, new Message("banner.enemyTurn", Turn)));
            foreach (var c in corpses.ToList()) Rise(c);

            var v = Vampire;
            var wasAwake = v != null && !v.Asleep;
            Moon = Math.Min(Rules.MoonMax, Moon + 1);
            var full = wasAwake && Moon >= Rules.MoonMax && EatTarget.HasValue;
            if (full) Say(new Message("log.fullMoon"), "k");
            if (v != null && v.Asleep && Moon >= Rules.MoonWake)
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
                    if (!units.Contains(e) || e.JustRisen || e.Asleep || e.JustWoke) continue;
                    if (kind == Role.Vampire && full) continue;
                    MeleeAct(e);
                    if (CheckEnd()) return;
                }
            }

            foreach (var u in units)
            {
                if (u.Side == Side.Enemy) { u.JustRisen = false; u.JustWoke = false; }
                else { u.Moved = false; u.Acted = false; u.MovedDist = 0; u.ActedAgain = false; }
            }
            if (CheckEnd()) return;
            Turn++;
            Phase = Phase.Player;
            Say(new Message("log.playerTurn", Turn), "t");
            Emit(BattleEvent.Banner(BannerKind.PlayerTurn, new Message("banner.playerTurn", Turn)));
            FixEatTarget();
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
            foreach (var u in Allies)
            {
                var d = Hex.Distance(v.Pos, u.Pos);
                if (d > Rules.EatRange || Board.Lit(u.Pos) || u.Has(Ability.Ward)) continue;
                var score = d * 100 - GameData.Type(u.Type).Pop;
                if (best == null || score < bestScore) { best = u; bestScore = score; }
            }
            return best;
        }

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
                Moon = 0;
                EatTarget = null;
                Say(new Message("log.moonSet"), "sys");
                return;
            }
            var why = t == null ? new Message("why.gone")
                : Board.Lit(t.Pos) ? new Message("why.lit", t.Name)
                : new Message("why.far", t.Name);
            Say(new Message("log.drainFailed", why, v.Name), "h");
            Emit(BattleEvent.Popup(v.Id, PopupKind.DrainFailed, new Message("popup.drainFailed")));
            Moon = 0;
            EatTarget = null;
            Say(new Message("log.moonSet"), "sys");
            MeleeAct(v);
        }

        double TargetScore(Unit att, Unit t)
        {
            var d = Damage(att, t);
            return (d >= t.Hp ? 100 : 0) + GameData.Type(t.Type).Pop * 3 - t.Hp * 0.5 + d;
        }

        Unit PickTarget(Unit att, List<Unit> list)
        {
            var best = list[0];
            foreach (var t in list.Skip(1))
                if (TargetScore(att, t) > TargetScore(att, best)) best = t;
            return best;
        }

        // docs/systems/ai.md 2: allies within range, no line of sight.
        List<Unit> AlliesInReach(Unit e, Hex h) => Allies.Where(a => Hex.Distance(h, a.Pos) <= e.Rng).ToList();

        void MeleeAct(Unit e)
        {
            if (!Allies.Any()) return;
            var adj = AlliesInReach(e, e.Pos);
            if (adj.Count == 0)
            {
                var reach = Board.Reachable(e.Pos, e.Mov, e.Side, OccupantAt);
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
                    foreach (var a in Allies)
                        foreach (var n in Board.Neighbors(a.Pos))
                            if (!Board.Blocked(n, Side.Enemy, OccupantAt) && UnitAt(n) == null) goals.Add(n);
                    var field = Board.DistField(goals, Side.Enemy, OccupantAt);
                    var bestDist = field.TryGetValue(e.Pos, out var here) ? here : 1_000_000_000;
                    var bestSteps = 0;
                    foreach (var (h, steps) in reach)
                    {
                        if (!field.TryGetValue(h, out var d)) continue;
                        if (d < bestDist || (d == bestDist && steps < bestSteps)) { bestDist = d; bestSteps = steps; best = h; }
                    }
                }
                if (best.HasValue && best.Value != e.Pos) MoveEnemy(e, best.Value);
                if (!units.Contains(e)) return;
                adj = AlliesInReach(e, e.Pos);
            }
            if (adj.Count > 0) ApplyHit(e, PickTarget(e, adj));
        }

        void MoveEnemy(Unit e, Hex to)
        {
            Walk(e, to);
            foreach (var a in Allies.ToList())
            {
                if (!units.Contains(e)) break;
                if (!a.Has(Ability.Pike) || Hex.Distance(a.Pos, e.Pos) != 1) continue;
                e.Hp -= 1;
                Emit(BattleEvent.HpChanged(e.Id, e.Hp));
                Emit(BattleEvent.Popup(e.Id, PopupKind.Damage, new Message("popup.damage", 1)));
                Say(new Message("log.pike", a.Name, e.Name), "h");
                if (e.Hp <= 0) { Kill(e, a, DeathCause.Blow); break; }
            }
        }
    }
}
