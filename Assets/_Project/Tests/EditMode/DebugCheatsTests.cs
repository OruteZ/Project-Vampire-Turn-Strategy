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
            var c = DebugCheats.With(new Campaign(), ups: DebugCheats.AllUpgrades.Take(1));
            c = DebugCheats.With(c, gold: 777, cleared: DebugCheats.FirstStages(2));
            Assert.AreEqual(777, c.Gold);
            Assert.AreEqual(1, c.Upgrades.Count, "upgrades kept");
            Assert.IsTrue(c.StageOpen(2), "third stage opens after two clears");
            Assert.IsFalse(c.StageOpen(3));
            Assert.AreEqual(0, DebugCheats.With(c, gold: -5).Gold);
            Assert.AreEqual(c.ToJson(), Campaign.Parse(c.ToJson()).ToJson(), "survives a save round trip");
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
            var kinds = (UnitKind[])System.Enum.GetValues(typeof(UnitKind));
            for (var i = 0; i < plain.Length; i++)
                Assert.IsTrue(b.Place(kinds[i % kinds.Length], plain[i]).Ok, "place at " + plain[i]);
            Assert.Greater(b.Used(t => t.Pop), from.Pop, "past the stage's own cap");
            Assert.IsTrue(b.Start().Ok);
        }
    }
}
