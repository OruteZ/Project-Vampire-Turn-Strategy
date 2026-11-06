using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using VampireSpawn.Core;
using VampireSpawn.Game;
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

        // The battle screen follows docs/ui/battle-hud.md, not the HTML layout; BattleHudShowsEveryGoldenState covers it.
        static IEnumerable<string> ShotNames() => Load().shots.Where(s => s.screen != "battle" && s.screen != "camp").Select(s => s.name);

        // The campaign screen follows docs/ui/stage-select.md and upgrade.md; CampaignScreenShowsEveryGoldenState covers it.
        static IEnumerable<string> CampShots() => Load().shots.Where(s => s.screen == "camp").Select(s => s.name);

        static IEnumerable<string> BattleShots() => Load().shots.Where(s => s.screen == "battle").Select(s => s.name);

        static readonly Regex RawKey = new Regex("^[a-z]+(\\.[a-zA-Z]+)+$");

        static readonly Regex Tag = new Regex("<[^>]+>"), Space = new Regex("\\s+");

        // The language row (docs/ui/localization.md 6) is new UI with no prototype twin.
        static List<string> Blocks(Transform root) =>
            root.GetComponentsInChildren<Text>(true).Where(t => t.GetComponentsInParent<Transform>(true).All(p => p.name != "Languages")).Select(t => Space.Replace(Tag.Replace(t.text, ""), "")).Where(s => s.Length > 0).OrderBy(s => s, StringComparer.Ordinal).ToList();

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

        [TestCaseSource(nameof(BattleShots))]
        public void BattleHudShowsEveryGoldenState(string name)
        {
            var shot = Load().shots.First(s => s.name == name);
            var s = new BattleSession(new Battle(GameData.Stages.First(x => x.Id == shot.stage)));
            foreach (var op in shot.ops) OverlayTests.Apply(s, op);
            var b = s.Battle;
            var hover = b.Enemies.FirstOrDefault();
            new BattleHud(null).Build(root, s, new HudState { Hover = hover, Demo = true, Alt = true }, new BattleActions());
            var texts = root.GetComponentsInChildren<Text>(true).Select(t => t.text).ToList();
            CollectionAssert.IsEmpty(texts.Where(t => RawKey.IsMatch(t)), name + " shows a table key instead of its text");
            CollectionAssert.Contains(texts, BattleScreen.Hint(s, false), name + " hint");
            CollectionAssert.Contains(texts, BattleScreen.PrimaryLabel(b, false, out _), name + " primary button");
            if (b.Phase == Phase.Deploy)
                foreach (var k in b.Stage.Units) Assert.That(texts, Has.Some.Contains(Loc.Text(GameData.Type(k).Name)), name + " roster");
            else
            {
                Assert.That(texts, Has.Some.Contains(b.Turn.ToString()), name + " turn");
                if (hover != null) Assert.That(texts, Has.Some.Contains(hover.Hp + "/" + hover.MaxHp), name + " enemy panel");
                foreach (var l in b.Log.Take(3)) Assert.That(texts, Has.Some.StartsWith(Loc.Text(l.Text).Substring(0, 1)), name + " log");
            }
        }

        [TestCaseSource(nameof(CampShots))]
        public void CampaignScreenShowsEveryGoldenState(string name)
        {
            var shot = Load().shots.First(s => s.name == name);
            var c = Campaign.Parse(shot.camp);
            var sel = CampaignScreen.DefaultStage(c);
            CampaignScreen.Build(root, c, new CampView { Selected = sel, ResetArm = shot.arm }, new CampaignActions(), null);
            var texts = root.GetComponentsInChildren<Text>(true).Select(t => t.text).ToList();
            CollectionAssert.IsEmpty(texts.Where(t => RawKey.IsMatch(t)), name + " shows a table key instead of its text");
            CollectionAssert.Contains(texts, c.Gold.ToString(), name + " gold");
            foreach (var st in Campaign.Stages) CollectionAssert.Contains(texts, Loc.Text(st.Title), name + " route");
            CollectionAssert.Contains(texts, Loc.Text(Campaign.Stages[sel].Brief), name + " stage info");
            foreach (var t in GameData.Types.Where(t => c.UpgradesOpen(t.Kind))) CollectionAssert.Contains(texts, Loc.Text(t.Name), name + " barracks");
            if (shot.arm) CollectionAssert.Contains(texts, Loc.T("camp.resetConfirm"), name + " reset armed");
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
                case "over":
                {
                    traces ??= JsonUtility.FromJson<Traces>("{\"traces\":" + File.ReadAllText(Out("battles.json")) + "}");
                    var tr = traces.traces.First(t => t.policy + "/" + t.stage == shot.trace);
                    var demo = tr.stage == "demo";
                    var c = Campaign.Parse(shot.camp);
                    var b = demo ? new Battle(GameData.Stages[0]) : c.NewBattle(GameData.Stages.First(x => x.Id == tr.stage));
                    foreach (var step in tr.steps) Assert.IsTrue(BattleParityTests.Run(b, JsonUtility.FromJson<BattleParityTests.Cmd>(step.cmd)).Ok, step.cmd);
                    new BattleHud(null).Over(root, OverInfo.From(b, demo, c), new BattleActions());
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
