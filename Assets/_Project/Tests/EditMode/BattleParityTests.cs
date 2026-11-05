using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    public class BattleParityTests
    {
        [Serializable] class Step { public string cmd, snap; public string[] events; }
        [Serializable] class Trace { public string stage, policy, initial; public string[] upgrades, finalLog; public Step[] steps; }
        [Serializable] class Golden { public Trace[] traces; }
        [Serializable] class Cmd { public string op, to, kind; public int id, target; }

        static Golden golden;

        static Golden Load()
        {
            if (golden != null) return golden;
            var file = Path.Combine(Directory.GetCurrentDirectory(), "tools", "golden", "out", "battles.json");
            golden = JsonUtility.FromJson<Golden>("{\"traces\":" + File.ReadAllText(file) + "}");
            return golden;
        }

        static IEnumerable<string> TraceNames() => Load().traces.Select(t => t.policy + "/" + t.stage);

        static Hex ParseHex(string s)
        {
            var p = s.Split(',');
            return new Hex(int.Parse(p[0]), int.Parse(p[1]));
        }

        static UnitKind KindOf(string key) => GameData.Types.First(t => t.Key == key).Kind;

        static CommandResult Run(Battle b, Cmd c) => c.op switch
        {
            "start" => b.Start(),
            "endTurn" => b.EndTurn(),
            "move" => b.Move(c.id, ParseHex(c.to)),
            "attack" => b.Attack(c.id, c.target),
            "execute" => b.Execute(c.id, c.target),
            "wait" => b.Wait(c.id),
            "place" => b.Place(KindOf(c.kind), ParseHex(c.to)),
            "recall" => b.Recall(c.id),
            "relocate" => b.Relocate(c.id, ParseHex(c.to)),
            "clear" => b.ClearAllies(),
            "sample" => b.UseSample(),
            _ => throw new ArgumentException("unknown op " + c.op),
        };

        [Test]
        public void GoldenHasEveryPolicyAndStage()
        {
            Assert.AreEqual(43, Load().traces.Length);
            Assert.That(Load().traces.All(t => t.steps.Length > 0 && t.steps.Last().snap.Contains("ph=over")), "every trace ends the battle");
        }

        [TestCaseSource(nameof(TraceNames))]
        public void TraceReplaysIdentically(string name)
        {
            var trace = Load().traces.First(t => t.policy + "/" + t.stage == name);
            var stage = GameData.Stages.First(s => s.Id == trace.stage);
            var battle = new Battle(stage, trace.upgrades);
            Assert.AreEqual(trace.initial, battle.Snapshot(), name + " initial state");
            for (var i = 0; i < trace.steps.Length; i++)
            {
                var step = trace.steps[i];
                var at = name + " step " + i + " " + step.cmd + " ";
                var result = Run(battle, JsonUtility.FromJson<Cmd>(step.cmd));
                Assert.IsTrue(result.Ok, at + "rejected: " + result.Reason);
                Assert.AreEqual(step.events, result.Events.Where(e => e.InPrototype).Select(e => e.ToString()).ToArray(), at + "events");
                Assert.AreEqual(step.snap, battle.Snapshot(), at + "state");
            }
            Assert.AreEqual(battle.Vampire == null, battle.Won, name + " won flag");
            Assert.AreEqual(trace.finalLog, battle.Log.Select(l => l.Text + "|" + l.Style).ToArray(), name + " final 80-line log");
        }
    }
}
