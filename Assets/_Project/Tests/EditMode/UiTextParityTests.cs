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

        // Menu and result screens follow docs/art/ui-style.md 14 (names and numbers only), not the HTML text; ScreenFollowsGoldenState checks what they must still show.
        static IEnumerable<string> ShotNames() => Load().shots.Where(s => s.screen != "battle" && s.screen != "camp").Select(s => s.name);

        // ui-style.md 14-1-1: no sentence on screen outside the rules window, the mode hint, warnings and button labels.
        static readonly Regex Sentence = new Regex("[.!?。]\\s*$|다\\.?$");

        static void AssertNoSentences(Transform root, string name, params string[] allowed)
        {
            var bad = root.GetComponentsInChildren<Text>(true).Where(t => t.name != "Warn" && t.GetComponentInParent<Button>(true) == null).Select(t => t.text.Trim()).Where(t => t.Length > 8 && Sentence.IsMatch(t) && !allowed.Contains(t)).ToList();
            CollectionAssert.IsEmpty(bad, name + " shows sentences");
        }

        // The campaign screen follows docs/ui/stage-select.md and upgrade.md; CampaignScreenShowsEveryGoldenState covers it.
        static IEnumerable<string> CampShots() => Load().shots.Where(s => s.screen == "camp").Select(s => s.name);

        static IEnumerable<string> BattleShots() => Load().shots.Where(s => s.screen == "battle").Select(s => s.name);

        static readonly Regex RawKey = new Regex("^[a-z]+(\\.[a-zA-Z]+)+$");

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
            if (BattleScreen.ShowsHint(s, false)) CollectionAssert.Contains(texts, BattleScreen.Hint(s, false), name + " hint");
            else CollectionAssert.DoesNotContain(texts, BattleScreen.Hint(s, false), name + " hint only in deploy and execute");
            AssertNoSentences(root, name, BattleScreen.Hint(s, false));
            CollectionAssert.Contains(texts, BattleScreen.PrimaryLabel(b, false, out _), name + " primary button");
            if (b.Phase == Phase.Deploy)
                foreach (var k in b.Stage.Units) Assert.That(texts, Has.Some.Contains(Loc.Text(GameData.Type(k).Name)), name + " roster");
            else
            {
                Assert.That(texts, Has.Some.Contains(b.Turn.ToString()), name + " turn");
                if (hover != null) Assert.That(texts, Has.Some.Contains(hover.Hp + "/" + hover.MaxHp), name + " enemy panel");
                foreach (var l in b.Log.Take(1)) Assert.That(texts, Has.Some.StartsWith(Loc.Text(l.Text).Substring(0, 1)), name + " log");
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
            CollectionAssert.DoesNotContain(texts, Loc.Text(Campaign.Stages[sel].Brief), name + " brief moved to the tooltip");
            AssertNoSentences(root, name, GameData.Upgrades.Select(u => Loc.Text(u.Desc)).ToArray());
            foreach (var t in GameData.Types.Where(t => c.UpgradesOpen(t.Kind))) CollectionAssert.Contains(texts, Loc.Text(t.Name), name + " barracks");
            if (shot.arm) CollectionAssert.Contains(texts, Loc.T("camp.resetConfirm"), name + " reset armed");
        }

        [TestCaseSource(nameof(ShotNames))]
        public void ScreenFollowsGoldenState(string name)
        {
            var shot = Load().shots.First(s => s.name == name);
            switch (shot.screen)
            {
                case "menu":
                {
                    var c = Campaign.Parse(shot.camp);
                    MenuScreen.Build(root, c, null, null);
                    var texts = root.GetComponentsInChildren<Text>(true).Select(t => t.text).ToList();
                    foreach (var want in new[] { "PROJECT VAMPIRE SPAWN", Loc.T("menu.demo"), Loc.T("menu.campaign"), c.Gold.ToString(), Campaign.Stages.Count(c.IsCleared) + "/" + Campaign.Stages.Length })
                        CollectionAssert.Contains(texts, want, name);
                    CollectionAssert.DoesNotContain(texts, Loc.T("menu.tagline"), name + " tagline cut");
                    AssertNoSentences(root, name);
                    return;
                }
                case "over":
                {
                    traces ??= JsonUtility.FromJson<Traces>("{\"traces\":" + File.ReadAllText(Out("battles.json")) + "}");
                    var tr = traces.traces.First(t => t.policy + "/" + t.stage == shot.trace);
                    var demo = tr.stage == "demo";
                    var c = Campaign.Parse(shot.camp);
                    var b = demo ? new Battle(GameData.Stages[0]) : c.NewBattle(GameData.Stages.First(x => x.Id == tr.stage));
                    foreach (var step in tr.steps) Assert.IsTrue(BattleParityTests.Run(b, JsonUtility.FromJson<BattleParityTests.Cmd>(step.cmd)).Ok, step.cmd);
                    var info = OverInfo.From(b, demo, c);
                    new BattleHud(null).Over(root, info, new BattleActions());
                    Assert.AreEqual(shot.campAfter, c.ToJson(), name + " campaign after finish");
                    var texts = root.GetComponentsInChildren<Text>(true).Select(t => t.text).ToList();
                    CollectionAssert.Contains(texts, Loc.T(info.Won ? "over.won" : "over.lost"), name);
                    foreach (var n in new[] { info.Stats.Lost, info.Stats.Executed, info.Stats.Risen, info.Stats.Drained }) CollectionAssert.Contains(texts, n.ToString(), name + " tally");
                    CollectionAssert.Contains(texts, Loc.T("over.retry"), name);
                    if (info.Won && info.Camp && info.Result.ClearGold > 0) CollectionAssert.Contains(texts, "+" + info.Result.ClearGold, name + " gold");
                    AssertNoSentences(root, name);
                    return;
                }
                default: throw new ArgumentException(shot.screen);
            }
        }
    }
}
