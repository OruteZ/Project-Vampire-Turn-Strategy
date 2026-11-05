using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    public class BattlePlaybackTests
    {
        [Serializable] class Step { public string cmd; }
        [Serializable] class Trace { public string stage, policy; public string[] upgrades; public Step[] steps; }
        [Serializable] class Golden { public Trace[] traces; }
        [Serializable] class Cmd { public string op, to, kind; public int id, target; }

        static Hex H(string s) { var p = s.Split(','); return new Hex(int.Parse(p[0]), int.Parse(p[1])); }

        static Func<Battle, CommandResult> Command(Cmd c) => c.op switch
        {
            "start" => b => b.Start(),
            "endTurn" => b => b.EndTurn(),
            "move" => b => b.Move(c.id, H(c.to)),
            "attack" => b => b.Attack(c.id, c.target),
            "execute" => b => b.Execute(c.id, c.target),
            "wait" => b => b.Wait(c.id),
            "place" => b => b.Place(GameData.Types.First(t => t.Key == c.kind).Kind, H(c.to)),
            "recall" => b => b.Recall(c.id),
            "relocate" => b => b.Relocate(c.id, H(c.to)),
            "clear" => b => b.ClearAllies(),
            "sample" => b => b.UseSample(),
            _ => throw new ArgumentException(c.op),
        };

        [UnityTest]
        public IEnumerator ScreenMatchesBattleAfterEveryCommand([Values("passive/demo", "aggressive/s3", "moonwise/s2", "upgraded/s9")] string name)
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            var view = UnityEngine.Object.FindFirstObjectByType<BattleView>();
            Assert.IsNotNull(view, "BattleView in Main");
            view.speed = 60f;
            var golden = JsonUtility.FromJson<Golden>("{\"traces\":" + File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "tools", "golden", "out", "battles.json")) + "}");
            var trace = golden.traces.First(t => t.policy + "/" + t.stage == name);
            view.Begin(new Battle(GameData.Stages.First(s => s.Id == trace.stage), trace.upgrades));
            yield return null;
            Assert.IsEmpty(view.Mismatches(), name + " before any command");
            for (var i = 0; i < trace.steps.Length; i++)
            {
                var cmd = JsonUtility.FromJson<Cmd>(trace.steps[i].cmd);
                var result = view.Run(Command(cmd));
                Assert.IsTrue(result.Ok, name + " step " + i + " rejected: " + result.Reason);
                var deadline = Time.realtimeSinceStartup + 30f;
                while (view.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
                yield return new WaitForSeconds(0.05f);
                var problems = view.Mismatches();
                Assert.IsEmpty(problems, name + " step " + i + " " + trace.steps[i].cmd + ": " + string.Join("; ", problems));
            }
            Assert.AreEqual(Phase.Over, view.Battle.Phase);
        }
    }
}
