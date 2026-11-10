using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    // #113: docs/systems/hex-grid.md (rough ground) and docs/systems/waves.md (reinforcements).
    public class WavesTerrainTests
    {
        [TearDown] public void BackToShipped() => GameData.Use(null);

        static readonly string[] Open = Enumerable.Repeat(".......", 7).ToArray();

        static Stage Field(Placement[] enemies, BonusType bonus = BonusType.AllMinions) => new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Boss,
            new Bonus(bonus, 0, 0), new Boss(30, 1, 0, 0, 0), Open, enemies, new Placement[0]);

        [Test]
        public void RoughGroundCostsTwoToEnter()
        {
            var board = new Board(new[] { "..,.." });
            Occupant Empty(Hex h) => Occupant.Empty;
            var three = board.Reachable(new Hex(0, 0), 3, Side.Ally, Empty).ToDictionary(r => r.hex, r => r.steps);
            Assert.AreEqual(3, three[new Hex(2, 0)], "one plain step, then rough costs 2");
            Assert.IsFalse(three.ContainsKey(new Hex(3, 0)), "past the rough needs 4");
            Assert.IsTrue(board.Reachable(new Hex(0, 0), 4, Side.Ally, Empty).Any(r => r.hex == new Hex(3, 0)));
            Assert.AreEqual(4, board.DistField(new[] { new Hex(0, 0) }, Side.Ally, Empty)[new Hex(3, 0)], "from (3,0) back to (0,0): rough 2, then 1, 1");
        }

        [Test]
        public void ReinforcementAppearsOnItsTurnAndWaitsAPhase()
        {
            var b = new Battle(Field(new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement("skeleton", 3, 0, arrive: 1) }), null, new[] { new Placement("pspear", 3, 5) });
            Assert.IsFalse(b.Enemies.Any(e => e.Kind == "skeleton"), "not on the board before its turn");
            Assert.AreEqual(1, b.Pending.Count);
            Assert.IsFalse(b.BonusMet, "all minions are not down while one is still to come");
            b.Start();
            b.EndTurn();
            var s = b.Enemies.Single(e => e.Kind == "skeleton");
            Assert.AreEqual(new Hex(3, 0), s.Pos, "appeared on its hex and did not act");
            Assert.AreEqual(0, b.Pending.Count);
            b.EndTurn();
            Assert.AreNotEqual(new Hex(3, 0), s.Pos, "acts from the next enemy phase");
        }

        [Test]
        public void ReinforcementOnATakenHexUsesTheNearestFreeOne()
        {
            var b = new Battle(Field(new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement("skeleton", 3, 3, arrive: 1) }), null, new[] { new Placement("pspear", 3, 3) });
            b.Start();
            b.EndTurn();
            var s = b.Enemies.Single(e => e.Kind == "skeleton");
            Assert.AreEqual(1, Hex.Distance(new Hex(3, 3), s.Pos));
        }
    }
}
