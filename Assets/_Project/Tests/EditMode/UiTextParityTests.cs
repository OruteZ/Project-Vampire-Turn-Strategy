using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using VampireSpawn.Core;
using VampireSpawn.Game.Screens;

namespace VampireSpawn.Tests
{
    public class UiTextParityTests
    {
        [Serializable] class Shot { public string name, screen, camp, campAfter, stage, trace; public bool arm; public string[] ops, blocks; }
        [Serializable] class Golden { public Shot[] shots; }
        [Serializable] class Trace { public string stage, policy; public string[] upgrades; public Step[] steps; }
        [Serializable] class Step { public string cmd; }
        [Serializable] class Traces { public Trace[] traces; }

        static Golden golden;

        [OneTimeSetUp] public void Fresh() => golden = null;
        static Traces traces;

        static string Out(string file) => Path.Combine(Directory.GetCurrentDirectory(), "tools", "golden", "out", file);

        static Golden Load() => golden ??= JsonUtility.FromJson<Golden>("{\"shots\":" + File.ReadAllText(Out("strings.json")) + "}");

        static IEnumerable<string> ShotNames() => Load().shots.Select(s => s.name);

        static readonly Regex Tag = new Regex("<[^>]+>"), Space = new Regex("\\s+");

        static List<string> Blocks(Transform root) =>
            root.GetComponentsInChildren<Text>(true).Select(t => Space.Replace(Tag.Replace(t.text, ""), "")).Where(s => s.Length > 0).OrderBy(s => s, StringComparer.Ordinal).ToList();

        RectTransform root;

        [SetUp] public void MakeRoot() => root = (RectTransform)new GameObject("UiTextParity", typeof(RectTransform)).transform;

        [TearDown] public void DropRoot() => UnityEngine.Object.DestroyImmediate(root.gameObject);

        [Test]
        public void GoldenCoversEveryScreen()
        {
            var screens = new HashSet<string>(Load().shots.Select(s => s.screen));
            CollectionAssert.IsSubsetOf(new[] { "menu", "camp", "battle", "over" }, screens);
            foreach (var n in new[] { "demo-corpse", "demo-spawn", "demo-target", "demo-vampire-awake", "over-s2-first", "camp-armed" })
                Assert.That(Load().shots.Any(s => s.name == n), n);
        }

        [TestCaseSource(nameof(ShotNames))]
        public void ScreenTextMatchesPrototype(string name)
        {
            var shot = Load().shots.First(s => s.name == name);
            switch (shot.screen)
            {
                case "menu":
                    MenuScreen.Build(root, Campaign.Parse(shot.camp), null, null);
                    break;
                case "camp":
                    CampaignScreen.Build(root, Campaign.Parse(shot.camp), shot.arm, null, new CampaignActions());
                    break;
                case "battle":
                {
                    var s = new BattleSession(new Battle(GameData.Stages.First(x => x.Id == shot.stage)));
                    foreach (var op in shot.ops) OverlayTests.Apply(s, op);
                    var actions = new BattleActions();
                    BattleScreen.Header(VampireSpawn.Game.Screens.Ui.Row(root, "Bar"), s, true, false, false, actions);
                    BattleScreen.SidePanel(VampireSpawn.Game.Screens.Ui.Col(root, "Side"), s, Enumerable.Empty<string>(), false, actions, null);
                    VampireSpawn.Game.Screens.Ui.Text(root, BattleScreen.Hint(s, false));
                    break;
                }
                case "over":
                {
                    traces ??= JsonUtility.FromJson<Traces>("{\"traces\":" + File.ReadAllText(Out("battles.json")) + "}");
                    var tr = traces.traces.First(t => t.policy + "/" + t.stage == shot.trace);
                    var demo = tr.stage == "demo";
                    var c = Campaign.Parse(shot.camp);
                    var b = demo ? new Battle(GameData.Stages[0]) : c.NewBattle(GameData.Stages.First(x => x.Id == tr.stage));
                    foreach (var step in tr.steps) Assert.IsTrue(BattleParityTests.Run(b, JsonUtility.FromJson<BattleParityTests.Cmd>(step.cmd)).Ok, step.cmd);
                    BattleScreen.Over(root, OverInfo.From(b, demo, c), new BattleActions());
                    Assert.AreEqual(shot.campAfter, c.ToJson(), name + " campaign after finish");
                    break;
                }
                default: throw new ArgumentException(shot.screen);
            }
            var want = shot.blocks.OrderBy(s => s, StringComparer.Ordinal).ToList();
            var got = Blocks(root);
            var missing = want.ToList();
            foreach (var g in got) missing.Remove(g);
            var extra = got.ToList();
            foreach (var w in want) extra.Remove(w);
            Assert.IsTrue(missing.Count == 0 && extra.Count == 0, name + "\nmissing: " + string.Join(" | ", missing) + "\nextra: " + string.Join(" | ", extra));
        }
    }
}
