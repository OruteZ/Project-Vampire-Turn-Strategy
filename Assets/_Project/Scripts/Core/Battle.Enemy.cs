using System;
using System.Collections.Generic;
using System.Linq;

namespace VampireSpawn.Core
{
    public sealed partial class Battle
    {
        static readonly Role[] ActingOrder = { Role.Skeleton, Role.Spawn, Role.Vampire };

        public bool MoonNext()
        {
            var v = Vampire;
            return v != null && !v.Asleep && Moon + 1 >= Rules.MoonMax;
        }

        void EnemyPhase()
        {
            Phase = Phase.Enemy;
            Say("— " + Turn + "턴 적 페이즈 —", "t");
            Emit(BattleEvent.Banner(Turn + "턴 · 적 페이즈", "enemy"));
            foreach (var c in corpses.ToList()) Rise(c);

            var v = Vampire;
            var wasAwake = v != null && !v.Asleep;
            Moon = Math.Min(Rules.MoonMax, Moon + 1);
            var full = wasAwake && Moon >= Rules.MoonMax && EatTarget.HasValue;
            if (full) Say("보름달이 떴다!", "k");
            if (v != null && v.Asleep && Moon >= Rules.MoonWake)
            {
                v.Asleep = false;
                v.JustWoke = true;
                Emit(BattleEvent.Woke(v.Id));
                Say("달빛에 관이 열렸다. " + Korean.Josa(v.Name, "이", "가") + " 깨어났다!", "k");
                Emit(BattleEvent.Banner("관이 열렸다", "enemy"));
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
                else { u.Moved = false; u.Acted = false; u.MovedDist = 0; u.Cleaved = false; }
            }
            Turn++;
            Phase = Phase.Player;
            Say("— " + Turn + "턴 아군 페이즈 —", "t");
            Emit(BattleEvent.Banner(Turn + "턴 · 아군 페이즈", "player"));
            FixEatTarget();
        }

        void Rise(Corpse c)
        {
            corpses.Remove(c);
            var spawn = new Unit
            {
                Id = nextId++, Role = Role.Spawn, Type = c.Type, Name = "스폰(" + c.Name + ")", Glyph = c.Glyph,
                Pos = c.Pos, Hp = c.MaxHp, MaxHp = c.MaxHp, Atk = c.Atk, Def = c.Def, Mov = c.Mov, Rng = 1,
                JustRisen = true, Origin = c.Name,
            };
            units.Add(spawn);
            Emit(BattleEvent.Rose(spawn.Id, c.Id, spawn.Pos));
            Stats.Risen++;
            Say(Korean.Josa(c.Name, "이", "가") + " 뱀파이어 스폰으로 일어났다.", "s");
        }

        Unit PickEat(Unit v)
        {
            Unit best = null;
            var bestScore = 0;
            foreach (var u in Allies)
            {
                var d = Hex.Distance(v.Pos, u.Pos);
                if (d > Rules.EatRange || Board.Lit(u.Pos)) continue;
                var score = d * 100 - GameData.Type(u.Type.Value).Pop;
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
                    Say("보름달이 차 있다. " + Korean.Josa(v.Name, "은", "는") + " 곁에 아무도 없어 굶주린 채 다가온다.", "sys");
                return;
            }
            EatTarget = t.Id;
            Say("보름달이 다가온다. 다음 적 턴에 " + Korean.Josa(v.Name, "은", "는") + " " + Korean.Josa(t.Name, "을", "를")
                + " 노린다. 범위 밖·빛 속으로 빼거나 처치하면 흡혈은 실패한다.", "k");
        }

        void FullMoonAct(Unit v)
        {
            var t = EatTarget.HasValue ? Find(EatTarget.Value) : null;
            Emit(BattleEvent.Banner("보름달", "enemy"));
            if (t != null && Hex.Distance(v.Pos, t.Pos) <= Rules.EatRange && !Board.Lit(t.Pos))
            {
                Say("보름달 아래 " + Korean.Josa(v.Name, "이", "가") + " " + Korean.Josa(t.Name, "을", "를") + " 먹어치웠다!", "k");
                Emit(BattleEvent.Popup(t.Id, "흡혈", "blood"));
                Emit(BattleEvent.Hit(t.Id));
                Stats.Drained++;
                Kill(t, v, DeathCause.Drain);
                v.Hp = Math.Min(v.MaxHp, v.Hp + v.Heal);
                Emit(BattleEvent.HpChanged(v.Id, v.Hp));
                Emit(BattleEvent.Popup(v.Id, "+" + v.Heal, "heal"));
                Moon = 0;
                EatTarget = null;
                Say("달이 졌다.", "sys");
                return;
            }
            var why = t == null ? "노리던 아군이 이미 없다"
                : Board.Lit(t.Pos) ? Korean.Josa(t.Name, "이", "가") + " 빛 속에 있다"
                : Korean.Josa(t.Name, "이", "가") + " 손이 닿지 않는 곳에 있다";
            Say("보름달이 떴지만 " + why + ". 흡혈 실패. " + Korean.Josa(v.Name, "이", "가") + " 굶주린 채 덤벼든다.", "h");
            Emit(BattleEvent.Popup(v.Id, "흡혈 실패", "heal"));
            Moon = 0;
            EatTarget = null;
            Say("달이 졌다.", "sys");
            MeleeAct(v);
        }

        double TargetScore(Unit att, Unit t)
        {
            var d = Damage(att, t);
            return (d >= t.Hp ? 100 : 0) + GameData.Type(t.Type.Value).Pop * 3 - t.Hp * 0.5 + d;
        }

        Unit PickTarget(Unit att, List<Unit> list)
        {
            var best = list[0];
            foreach (var t in list.Skip(1))
                if (TargetScore(att, t) > TargetScore(att, best)) best = t;
            return best;
        }

        List<Unit> AdjacentAllies(Hex h) => Allies.Where(a => Hex.Distance(h, a.Pos) == 1).ToList();

        void MeleeAct(Unit e)
        {
            if (!Allies.Any()) return;
            var adj = AdjacentAllies(e.Pos);
            if (adj.Count == 0)
            {
                var reach = Board.Reachable(e.Pos, e.Mov, e.Side, OccupantAt);
                Hex? best = null;
                var bestScore = -1e9;
                foreach (var (h, steps) in reach)
                {
                    var near = AdjacentAllies(h);
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
                // No liveness check: an enemy the pike wall just killed still strikes, as in the prototype. docs/systems/unity-port.md
                adj = AdjacentAllies(e.Pos);
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
                Emit(BattleEvent.Popup(e.Id, "-1", "dmg"));
                Say(a.Name + "의 창벽 → " + e.Name + " 1 피해", "h");
                if (e.Hp <= 0) { Kill(e, a, DeathCause.Blow); break; }
            }
        }
    }
}
