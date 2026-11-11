using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VampireSpawn.Core
{
    // #113: a text seat at the battle, so an agent can play a stage turn by turn the way a person would (docs/dev/stage-tools.md).
    // script eval: StageConsole.New("s6"), StageConsole.Do("m 12 3,4; a 12 30; end"), StageConsole.State(), StageConsole.Threat().
    public static class StageConsole
    {
        public static Battle B;

        public static string New(string stage, string formation = null)
        {
            var s = GameData.Stages.First(x => x.Id == stage);
            var f = formation?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(p =>
            {
                var a = p.Split('@'); var h = ParseHex(a[1]);
                return new Placement(a[0], h.Col, h.Row);
            }).ToArray();
            B = new Battle(s, null, f);
            return State();
        }

        static Hex ParseHex(string s) { var a = s.Split(','); return new Hex(int.Parse(a[0]), int.Parse(a[1])); }

        // m id c,r | a id target | x id target | b id corpse | h id target | w id | p kind c,r | r id (recall) | go | end
        public static string Do(string commands)
        {
            var sb = new StringBuilder();
            foreach (var raw in commands.Split(';'))
            {
                var c = raw.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (c.Length == 0) continue;
                CommandResult r;
                switch (c[0])
                {
                    case "m": r = B.Move(int.Parse(c[1]), ParseHex(c[2])); break;
                    case "a": r = B.Attack(int.Parse(c[1]), int.Parse(c[2])); break;
                    case "x": r = B.Execute(int.Parse(c[1]), int.Parse(c[2])); break;
                    case "b": r = B.Bury(int.Parse(c[1]), int.Parse(c[2])); break;
                    case "h": r = B.Mend(int.Parse(c[1]), int.Parse(c[2])); break;
                    case "w": r = B.Wait(int.Parse(c[1])); break;
                    case "p": r = B.Place(c[1], ParseHex(c[2])); break;
                    case "r": r = B.Recall(int.Parse(c[1])); break;
                    case "go": r = B.Start(); break;
                    case "end": r = B.EndTurn(); break;
                    default: sb.Append("? ").Append(raw).Append('\n'); continue;
                }
                sb.Append(raw.Trim()).Append(" -> ").Append(r.Ok ? "ok" : "NO " + r.Reason).Append('\n');
                if (!r.Ok) break;
                foreach (var e in r.Events)
                    if (e.Kind == BattleEventKind.Log) sb.Append("   ").Append(e.Text).Append('\n');
            }
            return sb.Append(Units()).ToString();
        }

        public static string State() => Head() + Map() + Units();

        static string Head()
        {
            var s = B.Stage;
            var sb = new StringBuilder();
            sb.Append($"{s.Id} {B.Board.Cols}x{B.Board.Rows} goal={s.Goal} hold={s.Hold} spots=[{string.Join(" ", s.Spots)}] bonus={s.Bonus.Type}:{s.Bonus.Value}\n");
            foreach (var t in s.Triggers) sb.Append($"trigger {t.Id} {t.Kind} {t.Value} [{string.Join(" ", t.Hexes)}] fired={B.Fired(t.Id)}\n");
            foreach (var d in s.Doors) sb.Append($"door [{string.Join(" ", d.Hexes)}] on {d.Trigger}\n");
            foreach (var r in B.Upcoming) sb.Append($"ally reinforcement +{r.Pop}pop on {r.Trigger} at [{string.Join(" ", r.Hexes)}]\n");
            foreach (var p in B.Pending) sb.Append($"enemy wave {p.Kind} {p.Col},{p.Row} {(p.Wave ?? "turn " + p.Arrive)} {p.Stance}\n");
            return sb.ToString();
        }

        public static string Map()
        {
            var sb = new StringBuilder("    ");
            for (var c = 0; c < B.Board.Cols; c++) sb.Append(c % 10);
            sb.Append('\n');
            for (var r = 0; r < B.Board.Rows; r++)
            {
                sb.Append(r.ToString().PadLeft(3)).Append(' ');
                for (var c = 0; c < B.Board.Cols; c++)
                {
                    var h = new Hex(c, r);
                    var u = B.UnitAt(h);
                    sb.Append(u != null ? Glyph(u) : B.CorpseAt(h) != null ? '%' : B.Board.At(h) switch
                    {
                        Terrain.Rock => '#', Terrain.Water => '~', Terrain.Rough => ',', Terrain.Torch => 'T', Terrain.Sun => '*', Terrain.Deploy => B.InDeploy(h) ? 'd' : '.',
                        _ => B.InDeploy(h) ? 'd' : B.Board.Lit(h) ? ':' : '.',
                    });
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static char Glyph(Unit u) => u.Role switch
        {
            Role.Ally => char.ToLowerInvariant(u.Type[u.Type.StartsWith("p") && u.Type.Length > 1 ? 1 : 0]),
            Role.Escort => 'e', Role.Vampire => 'V', Role.Spawn => 'Z',
            _ => u.Kind == "skeletonArcher" ? 'A' : u.Kind == "skeletonHeavy" ? 'H' : 'S',
        };

        public static string Units()
        {
            var sb = new StringBuilder();
            var v = B.Vampire;
            sb.Append($"turn {B.Turn} phase {B.Phase}{(B.Reinforcing ? "(reinforce)" : "")} moon {B.Moon}/{Rules.MoonMax} eat={B.EatTarget} pop {B.Used(t => t.Pop)}/{B.PopCap} killed {B.MinionsKilled} lost {B.Stats.Lost} risen {B.Stats.Risen} end={B.Ending}\n");
            foreach (var u in B.Units.OrderBy(u => u.Side).ThenBy(u => u.Id))
                sb.Append($"{(u.Side == Side.Ally ? "ALLY " : "ENEMY")} {u.Id,3} {u.Kind,-14} {u.Pos,-6} hp {u.Hp}/{u.MaxHp} atk {u.Atk} def {u.Def} mov {u.Mov} rng {u.Rng}" +
                    $"{(u.Side == Side.Ally ? (u.Moved ? " moved" : "") + (u.Acted ? " acted" : "") + (B.Board.Lit(u.Pos) ? " lit" : "") : (u.Asleep ? " asleep" : "") + (u.Stance != Stance.Charge ? " " + u.Stance + (u.Stance == Stance.Guard ? u.Alert.ToString() : "") : "") + (u.JustArrived ? " arrived" : "") + (u.JustRisen ? " risen" : ""))}\n");
            foreach (var c in B.Corpses) sb.Append($"CORPSE {c.Id} {c.Type} {c.Pos} hp {c.MaxHp} atk {c.Atk}\n");
            return sb.ToString();
        }

        public static string Reach(int id)
        {
            var u = B.Find(id);
            return string.Join(" ", B.Reach(u).Where(k => B.OccupantAt(k.Key) == Occupant.Empty || k.Key == u.Pos).OrderBy(k => k.Key.Row).ThenBy(k => k.Key.Col).Select(k => k.Key + ":" + k.Value));
        }

        // What an attentive player counts by hand: for every hex, how many awake enemies could hit it next enemy phase, and their summed attack.
        public static string Threat()
        {
            var hits = new Dictionary<Hex, (int n, int atk)>();
            foreach (var e in B.Enemies.Where(e => !e.Asleep && e.Stance != Stance.Guard))
            {
                var from = e.Stance == Stance.Hold ? new[] { e.Pos } : B.Reach(e).Keys.Where(h => h == e.Pos || B.OccupantAt(h) == Occupant.Empty).ToArray();
                var seen = new HashSet<Hex>();
                foreach (var f in from)
                    for (var r = 0; r < B.Board.Rows; r++)
                        for (var c = 0; c < B.Board.Cols; c++)
                        {
                            var h = new Hex(c, r);
                            if (Hex.Distance(f, h) <= e.Rng && B.Board.Walkable(h) && seen.Add(h))
                                hits[h] = hits.TryGetValue(h, out var x) ? (x.n + 1, x.atk + e.Atk) : (1, e.Atk);
                        }
            }
            var sb = new StringBuilder("threat (count of enemies that could hit each hex next phase; guards asleep not counted)\n    ");
            for (var c = 0; c < B.Board.Cols; c++) sb.Append(c % 10);
            sb.Append('\n');
            for (var r = 0; r < B.Board.Rows; r++)
            {
                sb.Append(r.ToString().PadLeft(3)).Append(' ');
                for (var c = 0; c < B.Board.Cols; c++)
                {
                    var h = new Hex(c, r);
                    sb.Append(!B.Board.Walkable(h) ? '#' : hits.TryGetValue(h, out var x) ? (char)('0' + Math.Min(9, x.n)) : '.');
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
