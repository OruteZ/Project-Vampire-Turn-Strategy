using System;
using System.Collections.Generic;
using System.Linq;
using VampireSpawn.Core;

namespace StagePlay
{
    // A one-turn-lookahead player: each turn it tries many random orders (who moves where, who hits whom),
    // plays the enemy phase for each, and keeps the turn that leaves the best position.
    // It never plans two turns ahead, so a careful person beats it; it is a yardstick for comparing stages and formations, not a ceiling.
    public static class Mc
    {
        public sealed class Result
        {
            public Ending? End; public bool Won; public int Turns, Lost, Risen, Drained; public bool Bonus; public List<string> Log;
        }

        static Battle Rebuild(Stage s, string[] up, Placement[] f, List<string> log)
        {
            var b = new Battle(s, up, f);
            StageConsole.B = b;
            foreach (var c in log) StageConsole.Do(c);
            return b;
        }

        public static Result Play(Stage s, string[] up, Placement[] f, int seed, int samples, int cap = 30)
        {
            var rng = new Random(seed);
            var log = new List<string> { "go" };
            var b = Rebuild(s, up, f, log);
            while (b.Phase != Phase.Over && b.Turn <= cap)
            {
                if (b.Reinforcing) { log.AddRange(StageSim.Reinforce(b).Select(p => $"p {p.Kind} {p.Col},{p.Row}")); log.Add("go"); b = Rebuild(s, up, f, log); continue; }
                if (b.Phase != Phase.Player) break;
                List<string> best = null; var bestScore = double.MinValue;
                for (var i = 0; i < samples; i++)
                {
                    var t = Rebuild(s, up, f, log);
                    var plan = Plan(t, rng, i == 0);
                    plan.Add("end"); StageConsole.Do("end");
                    var sc = Score(t) + rng.NextDouble() * 0.01;
                    if (sc > bestScore) { bestScore = sc; best = plan; }
                }
                log.AddRange(best);
                b = Rebuild(s, up, f, log);
            }
            return new Result { End = b.Ending, Won = b.Won == true, Turns = b.Turn, Lost = b.Stats.Lost, Risen = b.Stats.Risen, Drained = b.Stats.Drained, Bonus = b.Won == true && b.BonusMet, Log = log };
        }

        // One random turn, applied to b as it is built so every order is legal.
        static List<string> Plan(Battle b, Random rng, bool plain)
        {
            var plan = new List<string>();
            void Do(string c) { if (StageConsole.Do(c).Contains(" -> ok")) plan.Add(c); }
            var order = b.Units.Where(u => u.Side == Side.Ally).OrderBy(_ => rng.Next()).ToList();
            foreach (var u in order)
            {
                if (b.Phase != Phase.Player || !b.Units.Contains(u)) break;
                if (u.Role == Role.Escort)
                {
                    var reach = b.Reach(u).Keys.ToList();
                    var dest = b.Stage.Spots.OrderBy(h => Hex.Distance(u.Pos, h)).First();
                    var to = rng.Next(3) == 0 ? reach[rng.Next(reach.Count)] : reach.OrderBy(h => Hex.Distance(h, dest)).First();
                    if (to != u.Pos) Do($"m {u.Id} {to.Col},{to.Row}");
                    continue;
                }
                if (!plain && !u.Moved)
                {
                    var reach = b.Reach(u).Keys.ToList();
                    var foes = b.Enemies.Where(e => !e.Asleep).ToList();
                    var hit = reach.Where(h => foes.Any(e => b.InRange(u, h, e.Pos))).ToList();
                    var roll = rng.Next(10);
                    Hex to = u.Pos;
                    if (roll < 4 && hit.Count > 0) to = hit[rng.Next(hit.Count)];
                    else if (roll < 7) to = reach[rng.Next(reach.Count)];
                    if (to != u.Pos) Do($"m {u.Id} {to.Col},{to.Row}");
                }
                if (b.Phase != Phase.Player || !b.Units.Contains(u) || u.Acted) continue;
                var acts = new List<string>();
                foreach (var e in b.Enemies.Where(e => !e.Asleep && b.InRange(u, u.Pos, e.Pos)).OrderBy(e => e.Hp).Take(3)) acts.Add($"a {u.Id} {e.Id}");
                foreach (var c in b.Corpses.Where(c => b.CanBury(u, c))) { acts.Insert(0, $"b {u.Id} {c.Id}"); }
                foreach (var t in b.Units.Where(t => b.CanMend(u, t) && t.Hp < t.MaxHp).OrderBy(t => t.Hp).Take(2)) acts.Add($"h {u.Id} {t.Id}");
                if (b.EatTarget is int eat && b.Find(eat) is Unit prey && prey != u && b.InRange(u, u.Pos, prey.Pos) && b.Damage(u, prey) >= prey.Hp && rng.Next(4) == 0)
                    acts.Add($"x {u.Id} {prey.Id}");
                if (u.Has(Ability.Trap))
                {
                    var foe = b.Enemies.Where(e => !e.Asleep).OrderBy(e => Hex.Distance(u.Pos, e.Pos)).FirstOrDefault();
                    var spot = foe == null ? (Hex?)null : b.Board.Neighbors(u.Pos).Where(h => b.CanTrap(u, h)).OrderBy(h => Hex.Distance(h, foe.Pos)).Select(h => (Hex?)h).FirstOrDefault();
                    if (spot.HasValue) acts.Add($"t {u.Id} {spot.Value.Col},{spot.Value.Row}");
                }
                if (acts.Count == 0) continue;
                Do(plain || rng.Next(10) < 6 ? acts[0] : acts[rng.Next(acts.Count)]);
            }
            return plan;
        }

        static int PopOf(Unit u) => GameData.Current.Types.FirstOrDefault(t => t.Key == u.Type)?.Pop ?? 3;

        static double Score(Battle b)
        {
            if (b.Won == true) return 100000 - b.Turn * 10 - b.Stats.Lost * 50;
            if (b.Won == false) return -100000 + b.Turn;
            double s = 0;
            foreach (var u in b.Units)
            {
                if (u.Role == Role.Ally)
                {
                    var pop = PopOf(u);
                    s += pop * 14 + pop * 6.0 * u.Hp / u.MaxHp;
                    if (b.Board.Lit(u.Pos)) s += 0.5;
                }
                else if (u.Role == Role.Escort)
                {
                    s += 30.0 * u.Hp / u.MaxHp - 4 * b.Stage.Spots.Min(h => Hex.Distance(u.Pos, h));
                }
                else
                {
                    s -= u.Role == Role.Vampire ? (u.Asleep ? 0 : 1.6 * u.Hp) : u.Role == Role.Spawn ? 6 + 1.2 * u.Hp : 3 + 1.0 * u.Hp;
                }
            }
            s -= 12 * b.Corpses.Count;
            if (b.EatTarget is int eat && b.Find(eat) is Unit prey && b.Vampire is Unit v)
            {
                var d = Hex.Distance(v.Pos, prey.Pos);
                var safe = d >= GameData.Current.Rules.EatRange && prey.Mov >= 1;
                if (!safe) s -= PopOf(prey) * 10;
            }
            if (b.Stage.Goal == Goal.Boss)
            {
                var foes = b.Enemies.Where(e => !e.Asleep).ToList();
                if (foes.Count > 0) s -= 0.15 * b.Allies.Sum(a => foes.Min(e => Hex.Distance(a.Pos, e.Pos)));
            }
            // A door that opens when someone reaches a hex: lean toward it, or the bot never finds the boss behind it.
            foreach (var t in b.Stage.Triggers.Where(t => t.Kind == TriggerKind.Reach && !b.Fired(t.Id) && t.Hexes.Length > 0))
                if (b.Allies.Any()) s -= 1.5 * b.Allies.Min(a => t.Hexes.Min(h => Hex.Distance(a.Pos, h)));
            s += 25 * b.Stage.Triggers.Count(t => t.Kind == TriggerKind.Reach && b.Fired(t.Id));
            return s - b.Turn * 0.5;
        }
    }
}
