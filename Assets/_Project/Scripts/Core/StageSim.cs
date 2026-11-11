using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VampireSpawn.Core
{
    // #113 plan 6: plays each stage with its sample formation and a plain bot, to catch stages that end too fast, never end, or soft-lock.
    // A bot is weaker than a person, so this only flags extremes; it says nothing about fun. Run: script eval "return VampireSpawn.Core.StageSim.Report();"
    public static class StageSim
    {
        public const int TurnCap = 40;

        public readonly struct Run
        {
            public readonly string Stage;
            public readonly bool Won;
            public readonly Ending? End;
            public readonly int Turns, Lost, Risen, Killed, PeakEnemies;

            public Run(string stage, bool won, Ending? end, int turns, int lost, int risen, int killed, int peak)
            {
                Stage = stage; Won = won; End = end; Turns = turns; Lost = lost; Risen = risen; Killed = killed; PeakEnemies = peak;
            }
        }

        public static string Report(string only = null)
        {
            var sb = new StringBuilder("stage | end | turns | lost | risen | minions killed | peak enemies\n");
            foreach (var s in GameData.Stages.Where(s => only == null || s.Id == only))
            {
                var r = Play(s);
                sb.Append($"{r.Stage} | {(r.End?.ToString() ?? "unfinished")} | {r.Turns} | {r.Lost} | {r.Risen} | {r.Killed} | {r.PeakEnemies}\n");
            }
            return sb.ToString();
        }

        public static Run Play(Stage stage)
        {
            var b = new Battle(stage);
            b.Start();
            var peak = b.Enemies.Count();
            while (b.Phase == Phase.Player && b.Turn <= TurnCap)
            {
                foreach (var u in b.Units.Where(x => x.Side == Side.Ally).ToList())
                    if (b.Phase == Phase.Player && b.Units.Contains(u)) Act(b, u);
                if (b.Phase != Phase.Player) break;
                b.EndTurn();
                peak = System.Math.Max(peak, b.Enemies.Count());
            }
            var won = b.Won == true;
            return new Run(stage.Id, won, b.Ending, b.Turn, b.Stats.Lost, b.Stats.Risen, b.MinionsKilled, peak);
        }

        // Attack what it can reach, else walk toward the nearest awake enemy (the escort walks to its nearest destination), then attack again if now in reach.
        static void Act(Battle b, Unit u)
        {
            if (u.Role == Role.Escort)
            {
                var dest = b.Stage.Spots.OrderBy(h => Hex.Distance(u.Pos, h)).FirstOrDefault();
                Step(b, u, dest);
                return;
            }
            if (TryHit(b, u)) return;
            var foe = b.Enemies.Where(e => !e.Asleep).OrderBy(e => Hex.Distance(u.Pos, e.Pos)).FirstOrDefault()
                ?? b.Enemies.OrderBy(e => Hex.Distance(u.Pos, e.Pos)).FirstOrDefault();
            if (foe == null) return;
            Step(b, u, foe.Pos);
            if (b.Phase == Phase.Player && b.Units.Contains(u)) TryHit(b, u);
        }

        // On a defence stage the bot never strays more than this from the defended hexes.
        const int Leash = 3;

        static bool Allowed(Battle b, Hex h) =>
            b.Stage.Goal != Goal.Defense || b.Stage.Spots.Length == 0 || b.Stage.Spots.Min(s => Hex.Distance(s, h)) <= Leash;

        static bool TryHit(Battle b, Unit u)
        {
            if (u.Acted) return false;
            var t = b.Enemies.Where(e => !e.Asleep && Hex.Distance(u.Pos, e.Pos) <= u.Rng).OrderBy(e => e.Hp).FirstOrDefault();
            return t != null && b.Attack(u.Id, t.Id).Ok;
        }

        static void Step(Battle b, Unit u, Hex goal)
        {
            if (u.Moved) return;
            var reach = b.Reach(u);
            var ok = reach.Keys.Where(h => u.Role == Role.Escort || Allowed(b, h)).ToList();
            if (ok.Count == 0) return;
            var best = ok.OrderBy(h => Hex.Distance(h, goal)).ThenBy(h => reach[h]).First();
            if (best != u.Pos && Hex.Distance(best, goal) < Hex.Distance(u.Pos, goal)) b.Move(u.Id, best);
        }
    }
}
