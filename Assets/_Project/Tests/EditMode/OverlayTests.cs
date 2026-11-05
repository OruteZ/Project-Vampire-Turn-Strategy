using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    public class OverlayTests
    {
        [Serializable] class Step { public string op, hint, ins, mode; public int sel; public string[] ov; }
        [Serializable] class Run { public string stage; public string[] upgrades; public Step[] steps; }
        [Serializable] class Golden { public Run[] runs; }

        static Golden golden;

        static Golden Load()
        {
            if (golden != null) return golden;
            var file = Path.Combine(Directory.GetCurrentDirectory(), "tools", "golden", "out", "overlay.json");
            golden = JsonUtility.FromJson<Golden>("{\"runs\":" + File.ReadAllText(file) + "}");
            return golden;
        }

        static IEnumerable<int> RunIndexes() => Enumerable.Range(0, Load().runs.Length);

        static Hex ParseHex(string s)
        {
            var p = s.Split(',');
            return new Hex(int.Parse(p[0]), int.Parse(p[1]));
        }

        static readonly Dictionary<Mark, string> ClassOf = new Dictionary<Mark, string>
        {
            [Mark.Deploy] = "ov-deploy", [Mark.Threat] = "ov-threat", [Mark.Move] = "ov-move", [Mark.Atk] = "ov-atk",
            [Mark.Exec] = "ov-exec", [Mark.Sel] = "ov-sel", [Mark.MarkZone] = "ov-markzone", [Mark.Target] = "ov-mark",
        };

        public static List<string> Describe(BattleSession s)
        {
            var o = s.Overlay().Select(m => ClassOf[m.Mark] + "@" + m.Hex).ToList();
            var moon = s.Moon();
            if (moon != null)
            {
                o.Add("label:" + moon.VampireLabel + "@" + moon.Vampire);
                if (moon.Target is Hex t)
                {
                    o.Add("line@" + moon.Vampire + ">" + t);
                    o.Add("label:" + moon.TargetLabel + "@" + t);
                }
            }
            o.Sort(string.CompareOrdinal);
            return o;
        }

        static void Apply(BattleSession s, string op)
        {
            var parts = op.Split(' ');
            switch (parts[0])
            {
                case "init": break;
                case "pick": s.Pick(GameData.Types.First(t => t.Key == parts[1]).Kind); break;
                case "click": s.Click(ParseHex(parts[1])); break;
                case "start": s.Start(); break;
                case "threat": s.ShowThreat = parts[1] == "on"; break;
                case "exec": s.ToggleExecute(); break;
                case "escape": s.Escape(); break;
                case "endTurn": s.EndTurn(); break;
                default: throw new ArgumentException("unknown op " + op);
            }
        }

        [Test]
        public void GoldenCoversEveryOverlayClass()
        {
            var seen = new HashSet<string>(Load().runs.SelectMany(r => r.steps).SelectMany(s => s.ov).Select(v => v.Split('@')[0]));
            foreach (var cls in ClassOf.Values.Concat(new[] { "line", "label:보름달 표적", "label:다음 턴 보름달", "label:보름달 · 굶주림" }))
                Assert.That(seen, Does.Contain(cls), cls);
        }

        [TestCaseSource(nameof(RunIndexes))]
        public void SelectionOverlayAndHintMatchPrototype(int index)
        {
            var run = Load().runs[index];
            var stage = GameData.Stages.First(st => st.Id == run.stage);
            var s = new BattleSession(new Battle(stage, run.upgrades));
            for (var i = 0; i < run.steps.Length; i++)
            {
                var step = run.steps[i];
                Apply(s, step.op);
                var at = $"{run.stage} step {i} ({step.op})";
                CollectionAssert.AreEqual(step.ov.OrderBy(x => x, StringComparer.Ordinal).ToList(), Describe(s), at + " overlay");
                Assert.AreEqual(step.hint, s.Hint, at + " hint");
                Assert.AreEqual(step.sel, s.Selected ?? -1, at + " selected");
                Assert.AreEqual(step.ins, s.InspectCorpse is int c ? "c" + c : s.InspectUnit?.ToString() ?? "", at + " inspect");
                Assert.AreEqual(step.mode, s.Mode == SelectMode.Execute ? "execute" : "act", at + " mode");
            }
        }
    }
}
