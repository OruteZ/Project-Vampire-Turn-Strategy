using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    public class DebugCheatsTests
    {
        [Test]
        public void GoldAndProgressChangeOnlyWhatIsAsked()
        {
            var c = DebugCheats.With(new Campaign(), ups: DebugCheats.Branch(1).Take(1));
            c = DebugCheats.With(c, gold: 777, cleared: DebugCheats.FirstStages(2));
            Assert.AreEqual(777, c.Gold);
            Assert.AreEqual(1, c.Upgrades.Count, "upgrades kept");
            Assert.IsTrue(c.StageOpen(2), "third stage opens after two clears");
            Assert.IsFalse(c.StageOpen(3));
            Assert.AreEqual(0, DebugCheats.With(c, gold: -5).Gold);
            Assert.AreEqual(c.ToJson(), Campaign.Parse(c.ToJson()).ToJson(), "survives a save round trip");
            c.SeeRule("turn");
            CollectionAssert.AreEqual(new[] { "turn" }, DebugCheats.With(c, gold: 1).SeenRules, "seen rule cards kept");
        }

        [Test]
        public void ProgressRestartsWithClearRewardsAndEmptyBarracks()
        {
            var s = Campaign.Stages;
            var c = DebugCheats.Progress(2, withBonus: true);
            Assert.AreEqual(s[0].Reward + s[1].Reward + s[0].Bonus.Gold + s[1].Bonus.Gold, c.Gold);
            Assert.AreEqual(0, c.Upgrades.Count);
            Assert.AreEqual(2, c.BonusDone.Count);
            Assert.IsTrue(c.StageOpen(2));
            Assert.IsFalse(c.StageOpen(3));
            Assert.AreEqual(s[0].Reward, DebugCheats.Progress(1, withBonus: false).Gold);
        }

        [Test]
        public void BranchGivesEveryTypeThatBranch()
        {
            for (var n = 1; n <= 3; n++)
            {
                var c = DebugCheats.With(new Campaign(), ups: DebugCheats.Branch(n));
                foreach (var t in GameData.Types) Assert.IsTrue(c.Owns(GameData.Branches(t.Key)[n - 1]), t.Key + " branch " + n);
            }
        }

        [Test]
        public void TestStageTakesEveryKindAnywherePlainWithoutCaps()
        {
            var from = GameData.Stages.First(s => s.Id == "demo");
            var b = new Battle(DebugCheats.TestStage(from, deployAnywhere: true));
            b.ClearAllies();
            var plain = Enumerable.Range(0, b.Board.Rows)
                .SelectMany(r => Enumerable.Range(0, b.Board.Cols).Select(col => new Hex(col, r)))
                .Where(h => new Board(from.Map).At(h) == Terrain.Plain && b.UnitAt(h) == null).ToArray();
            var kinds = GameData.Types.Select(t => t.Key).ToArray();
            for (var i = 0; i < plain.Length; i++)
                Assert.IsTrue(b.Place(kinds[i % kinds.Length], plain[i]).Ok, "place at " + plain[i]);
            Assert.Greater(b.Used(t => t.Pop), from.Pop, "past the stage's own cap");
            Assert.IsTrue(b.Start().Ok);
        }
    }
}
