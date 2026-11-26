using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game.Screens;

namespace VampireSpawn.Tests
{
    // docs/ui/rule-cards.md: which cards a stage opens with, and that the campaign save remembers the ones seen.
    public class RuleCardTests
    {
        [Test]
        public void StageOneTeachesTurningLightAndMoon()
        {
            CollectionAssert.AreEqual(new[] { "rule:turn", "rule:light", "rule:moon" }, Campaign.NewRules(0));
        }

        [Test]
        public void LaterStagesShowWhatTheyAreFirstToBring()
        {
            var stages = Campaign.Stages;
            for (var i = 1; i < stages.Length; i++)
            {
                var cards = Campaign.NewRules(i);
                foreach (var t in GameData.Types.Where(t => t.UnlockAt == i + 1)) CollectionAssert.Contains(cards, "unit:" + t.Key, stages[i].Id);
                foreach (var id in cards.Where(c => c.StartsWith("foe:")))
                    Assert.IsFalse(stages.Take(i).Any(s => s.Enemies.Any(e => "foe:" + e.Kind == id)), id + " already met before " + stages[i].Id);
            }
            var defense = System.Array.FindIndex(stages, s => s.Goal == Goal.Defense);
            CollectionAssert.Contains(Campaign.NewRules(defense), "goal:defense");
            Assert.IsFalse(Enumerable.Range(defense + 1, stages.Length - defense - 1).Any(i => Campaign.NewRules(i).Contains("goal:defense")), "a goal is new only once");
        }

        [Test]
        public void EveryCardReadsInFull()
        {
            foreach (var id in Enumerable.Range(0, Campaign.Stages.Length).SelectMany(Campaign.NewRules))
            {
                var card = BattleScreen.Card(id);
                foreach (var text in new[] { card.Name, card.Line })
                    Assert.IsFalse(string.IsNullOrEmpty(text) || text.Contains("{") || text.Contains("No translation"), id + ": " + text);
                Assert.IsNotNull(card.Icon, id);
            }
        }

        [Test]
        public void SeenCardsStayHiddenOnRetryAndSurviveTheSave()
        {
            var c = new Campaign();
            var first = c.UnseenRules(0);
            Assert.AreEqual(3, first.Length);
            c.SeeRule(first[0]);
            CollectionAssert.AreEqual(first.Skip(1), c.UnseenRules(0), "a seen card does not come back");
            var back = Campaign.Parse(c.ToJson());
            CollectionAssert.AreEqual(first.Skip(1), back.UnseenRules(0), "the save keeps the seen cards");
        }

        [Test]
        public void AnOldSaveWithoutSeenCardsLoadsUnchanged()
        {
            var up = GameData.Upgrades[0];
            const string Old = "{\"gold\":42,\"cleared\":{\"s1\":true,\"s2\":true},\"bonus\":{\"s1\":true},\"ups\":{\"UP\":true}}";
            var c = Campaign.Parse(Old.Replace("UP", up.Id));
            Assert.AreEqual(42, c.Gold);
            CollectionAssert.AreEqual(new[] { "s1", "s2" }, c.Cleared);
            CollectionAssert.AreEqual(new[] { "s1" }, c.BonusDone);
            CollectionAssert.AreEqual(new[] { up.Id }, c.Upgrades);
            Assert.IsFalse(c.Repaired);
            Assert.AreEqual(3, c.UnseenRules(0).Length, "with no record every card shows again");
            c.SeeRule("rule:turn");
            var again = Campaign.Parse(c.ToJson());
            Assert.AreEqual(42, again.Gold);
            CollectionAssert.AreEqual(c.Cleared, again.Cleared);
            CollectionAssert.AreEqual(c.BonusDone, again.BonusDone);
            CollectionAssert.AreEqual(c.Upgrades, again.Upgrades);
            CollectionAssert.AreEqual(new[] { "rule:turn" }, again.SeenRules);
        }
    }
}
