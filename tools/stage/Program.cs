using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using VampireSpawn.Core;

namespace StagePlay
{
    // Headless seat at a battle (docs/dev/stage-tools.md): the rules assembly runs outside Unity on a data dump,
    // so stages can be played, replayed and bot-run from a shell, many at once.
    // A session is a text file: first line "stage|formation|upgrades", then one command line per call. Every call replays it from the top.
    public static class Program
    {
        static string Root => Environment.GetEnvironmentVariable("SP_DIR") ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..");

        public static int Main(string[] a)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var data = Environment.GetEnvironmentVariable("SP_DATA") ?? Path.Combine(Root, "data.json");
            using (var doc = JsonDocument.Parse(File.ReadAllText(data)))
                GameData.Use((DataSet)Reflect.Load(doc.RootElement, typeof(DataSet)));
            if (a.Length == 0) { Console.WriteLine("new <session> <stage> [formation] [upgrades] | play <session> [cmds] | replay <file>... | bot [stage] [formation] [upgrades] | validate | info <session> threat|reach <id>"); return 1; }
            switch (a[0])
            {
                case "validate":
                    var v = GameData.Current.Validate();
                    Console.WriteLine(v.Count == 0 ? "valid" : string.Join("\n", v));
                    return v.Count == 0 ? 0 : 2;
                case "new":
                    File.WriteAllText(SessionPath(a[1]), $"{a[2]}|{(a.Length > 3 ? a[3] : "")}|{(a.Length > 4 ? a[4] : "")}\n");
                    Open(a[1], out _);
                    Console.Write(Compact(StageConsole.State()));
                    return 0;
                case "play":
                    {
                        var b = Open(a[1], out var path);
                        var done = new List<string>();
                        var sb = new StringBuilder();
                        foreach (var raw in (a.Length > 2 ? a[2] : "").Split(';').Select(x => x.Trim()).Where(x => x.Length > 0))
                            foreach (var c in raw == "auto" ? Auto(b) : new[] { raw })
                            {
                                var r = StageConsole.Do(c);
                                var ok = r.Contains(" -> ok");
                                if (ok) done.Add(c);
                                sb.Append("> ").Append(Short(r)).Append('\n');
                            }
                        if (done.Count > 0) File.AppendAllText(path, string.Join("; ", done) + "\n");
                        Console.Write(sb);
                        Console.Write(Compact(StageConsole.State()));
                        return 0;
                    }
                case "info":
                    Open(a[1], out _);
                    Console.Write(a[2] == "threat" ? StageConsole.Threat() : StageConsole.Reach(int.Parse(a[3])));
                    return 0;
                case "replay":
                    foreach (var f in a.Skip(1))
                    {
                        var b = Open(f, out _);
                        var head = File.ReadLines(File.Exists(f) ? f : SessionPath(f)).First().Split('|');
                        var team = string.Join("+", b.Units.Where(u => u.Side == Side.Ally).Select(u => u.Kind).GroupBy(k => k).Select(g => g.Key + g.Count()));
                        Console.WriteLine($"{Path.GetFileNameWithoutExtension(f)}\t{head[0]}\t{(b.Ending?.ToString() ?? "unfinished")}\tturn {b.Turn}\tlost {b.Stats.Lost}\trisen {b.Stats.Risen}\tdrained {b.Stats.Drained}\tbonus {(b.Won == true && b.BonusMet ? "yes" : "no")}\tup [{head.ElementAtOrDefault(2)}]\tleft {team}");
                    }
                    return 0;
                case "form":
                    Console.WriteLine(string.Join(" ", Comp(GameData.Stages.First(s => s.Id == a[1]), a[2]).Select(p => $"{p.Kind}@{p.Col},{p.Row}")));
                    return 0;
                case "builds":
                    {
                        // builds <stage> <n> [seed]: n random builds (a few unit kinds, random counts, at most one upgrade branch per kind), one lookahead-bot game each.
                        // stage, formation, upgrades, end, won, lost, turns, buries, mends, traps, ally attacks, drained
                        var s = GameData.Stages.First(x => x.Id == a[1]);
                        var n = int.Parse(a[2]);
                        var rng = new Random(a.Length > 3 ? int.Parse(a[3]) : 1);
                        var samples = int.Parse(Environment.GetEnvironmentVariable("SP_SAMPLES") ?? "100");
                        var num = int.Parse(s.Id.Substring(1));
                        for (var i = 0; i < n; i++)
                        {
                            string force = Environment.GetEnvironmentVariable("SP_FORCE"), ban = Environment.GetEnvironmentVariable("SP_BAN");
                            var kinds = s.Units.Where(k => k != ban && k != force).OrderBy(_ => rng.Next()).Take(Math.Min(s.Units.Length, 2 + rng.Next(3))).ToList();
                            if (!string.IsNullOrEmpty(force)) kinds.Insert(0, force);
                            var spec = string.Join(" ", kinds.Select((k, j) => j == 0 || rng.Next(3) == 0 ? k + ":" + (j == 0 && !string.IsNullOrEmpty(force) ? 2 : 1 + rng.Next(3)) : k + ":*"));
                            if (!spec.Contains("*")) spec += " " + s.Units[rng.Next(Math.Min(2, s.Units.Length))] + ":*";
                            var f = Comp(s, spec);
                            var up = f.Select(p => p.Kind).Distinct().Where(k => GameData.Type(k).UnlockAt < num && rng.Next(2) == 0)
                                .Select(k => { var br = GameData.Branches(k); return br.Length == 0 ? null : br[rng.Next(br.Length)].Id; }).Where(x => x != null).ToArray();
                            var r = Mc.Play(s, up, f, i + 1, samples);
                            int Count(string c) => r.Log.Count(l => l.StartsWith(c + " "));
                            Console.WriteLine($"{s.Id}\t{string.Join("+", f.GroupBy(p => p.Kind).Select(g => g.Key + g.Count()))}\t{string.Join(",", up)}\t{(r.End?.ToString() ?? "cap")}\t{(r.Won ? 1 : 0)}\t{r.Lost}\t{r.Turns}\t{Count("b")}\t{Count("h")}\t{Count("t")}\t{Count("x")}\t{r.Drained}");
                        }
                        return 0;
                    }
                case "mc":
                    {
                        // mc <stage|all> [formation] [upgrades] ; env SP_SEEDS (8), SP_SAMPLES (200), SP_SAVE (play-file prefix for the first win)
                        var up = a.Length > 3 && a[3].Length > 0 ? a[3].Split(',') : null;
                        int seeds = int.Parse(Environment.GetEnvironmentVariable("SP_SEEDS") ?? "8"), samples = int.Parse(Environment.GetEnvironmentVariable("SP_SAMPLES") ?? "200");
                        var save = Environment.GetEnvironmentVariable("SP_SAVE");
                        foreach (var s in GameData.Stages.Where(s => a[1] == "all" ? s.Id != "demo" : a[1].Split(',').Contains(s.Id)))
                        {
                            var f = FormationFor(s, a.Length > 2 ? a[2] : null);
                            var rs = Enumerable.Range(1, seeds).Select(seed => Mc.Play(s, up, f, seed, samples)).ToList();
                            var wins = rs.Where(r => r.Won).ToList();
                            Console.WriteLine($"{s.Id}	{(f == null ? "sample" : string.Join("+", f.GroupBy(p => p.Kind).Select(g => g.Key + g.Count())))}	win {wins.Count}/{seeds}	lost {(wins.Count > 0 ? wins.Average(r => r.Lost).ToString("0.0") : "-")}	turns {(wins.Count > 0 ? wins.Average(r => r.Turns).ToString("0.0") : "-")}	bonus {wins.Count(r => r.Bonus)}	ends {string.Join(",", rs.GroupBy(r => r.End?.ToString() ?? "cap").Select(g => g.Key + g.Count()))}");
                            if (save != null && wins.Count > 0)
                            {
                                var w = wins.OrderBy(r => r.Lost).First();
                                var form = f ?? s.Sample;
                                File.WriteAllText(SessionPath(save + s.Id), $"{s.Id}|{string.Join(" ", form.Select(p => $"{p.Kind}@{p.Col},{p.Row}"))}|{(up == null ? "" : string.Join(",", up))}\n{string.Join("; ", w.Log)}\n");
                            }
                        }
                        return 0;
                    }
                case "bot":
                    {
                        Console.WriteLine("stage\tend\tturns\tlost\trisen\tkilled");
                        var f = a.Length > 2 && a[2].Length > 0 ? StageConsole.Formation(a[2]) : null;
                        var up = a.Length > 3 && a[3].Length > 0 ? a[3].Split(',') : null;
                        foreach (var s in GameData.Stages.Where(s => a.Length < 2 || a[1] == "all" || s.Id == a[1]))
                        {
                            var r = StageSim.Play(s, up, f);
                            Console.WriteLine($"{r.Stage}\t{(r.End?.ToString() ?? "unfinished")}\t{r.Turns}\t{r.Lost}\t{r.Risen}\t{r.Killed}");
                        }
                        return 0;
                    }
            }
            return 1;
        }

        // "knight:2 archer:1 pspear:* parcher:*": fixed counts first, then the starred kinds in turn until the population cap.
        // Melee stands on the deploy hexes nearest the enemy, sturdiest first; ranged and support behind them.
        static Placement[] Comp(Stage s, string spec)
        {
            var picks = new List<UnitType>(); int pop = 0, silver = 0;
            bool Add(string k)
            {
                if (!s.Units.Contains(k)) return false;
                var t = GameData.Type(k);
                if (pop + t.Pop > s.Pop || silver + t.Silver > s.Silver) return false;
                picks.Add(t); pop += t.Pop; silver += t.Silver; return true;
            }
            var parts = spec.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(':')).ToList();
            foreach (var p in parts.Where(p => p[1] != "*")) for (var i = 0; i < int.Parse(p[1]); i++) Add(p[0]);
            var fill = parts.Where(p => p[1] == "*").Select(p => p[0]).ToList();
            for (var any = true; any && fill.Count > 0;) { any = false; foreach (var k in fill) any |= Add(k); }
            var b = new Battle(s, null, new Placement[0]);
            var foes = s.Enemies.Where(e => e.Arrive == 0 && e.Wave == null).Select(e => new Hex(e.Col, e.Row)).ToList();
            var cells = b.Board.Cells.Where(h => b.InDeploy(h) && b.UnitAt(h) == null).OrderBy(h => foes.Min(e => Hex.Distance(h, e))).ThenBy(h => Math.Abs(h.Col - b.Board.Cols / 2)).ToList();
            var order = picks.OrderBy(t => t.Rng > 1 || t.Heal > 0 ? 1 : 0).ThenByDescending(t => t.Hp).ToList();
            return order.Take(cells.Count).Select((t, i) => new Placement(t.Key, cells[i].Col, cells[i].Row)).ToArray();
        }

        static Placement[] FormationFor(Stage s, string f) => string.IsNullOrEmpty(f) ? null : f.StartsWith("comp:") ? Comp(s, f.Substring(5)) : StageConsole.Formation(f);

        static string SessionPath(string name) => File.Exists(name) ? name : Path.Combine(Directory.CreateDirectory(Path.Combine(Root, "plays")).FullName, name + ".play");

        static Battle Open(string session, out string path)
        {
            path = SessionPath(session);
            var lines = File.ReadAllLines(path);
            var head = lines[0].Split('|');
            var stage = GameData.Stages.First(x => x.Id == head[0]);
            var up = head.Length > 2 && head[2].Length > 0 ? head[2].Split(',') : null;
            StageConsole.B = new Battle(stage, up, head.Length > 1 && head[1].Length > 0 ? StageConsole.Formation(head[1]) : null);
            foreach (var l in lines.Skip(1)) foreach (var c in l.Split(';')) if (c.Trim().Length > 0) StageConsole.Do(c.Trim());
            return StageConsole.B;
        }

        // Every ally that has not acted hits the weakest awake enemy in reach, ones it can kill first.
        static IEnumerable<string> Auto(Battle b)
        {
            foreach (var u in b.Units.Where(x => x.Side == Side.Ally && x.Role != Role.Escort).ToList())
            {
                if (!b.Units.Contains(u) || u.Acted || u.Atk <= 0 || b.Phase != Phase.Player) continue;
                var t = b.Enemies.Where(e => !e.Asleep && b.InRange(u, u.Pos, e.Pos)).OrderBy(e => e.Hp).FirstOrDefault();
                if (t != null) yield return $"a {u.Id} {t.Id}";
            }
        }

        static string Names(string s)
        {
            s = Regex.Replace(s, @"name\.numbered\(unit\.(\w+)\.name,(\d+)\)", "$1$2");
            return s.Replace(".name", "").Replace("log.", "").Replace("unit.", "").Replace("skeleton", "sk").Replace("stage.", "");
        }

        static string Short(string r) => Names(string.Join(" / ", r.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !Regex.IsMatch(l, @"^(ALLY|ENEMY|CORPSE|ESCORT|turn) "))));

        static string Compact(string state)
        {
            var sb = new StringBuilder();
            foreach (var l0 in state.Split('\n'))
            {
                var l = Regex.Replace(l0.TrimEnd(), " +", " ");
                var m = Regex.Match(l, @"^(ALLY|ENEMY) (\d+) (\w+) (\S+) hp (\S+) atk (\d+) def (\d+) mov (\d+) rng (\d+)(.*)$");
                if (m.Success)
                {
                    var g = m.Groups;
                    l = $"{g[1].Value[0]}{g[2].Value} {g[3].Value.Replace("skeleton", "sk")} {g[4].Value} {g[5].Value} a{g[6].Value}d{g[7].Value}m{g[8].Value}r{g[9].Value}{g[10].Value}";
                }
                sb.Append(l0.StartsWith("  ") || l0.StartsWith("   ") ? l0.TrimEnd() : l).Append('\n');
            }
            return sb.ToString();
        }
    }
}
