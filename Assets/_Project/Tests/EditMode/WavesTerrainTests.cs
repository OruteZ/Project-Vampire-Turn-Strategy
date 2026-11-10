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

        static Stage Staged(string[] map, Placement[] enemies, Trigger[] triggers, Door[] doors = null) => new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Boss,
            new Bonus(BonusType.AllMinions, 0, 0), new Boss(30, 1, 0, 0, 0), map, enemies, new Placement[0], triggers: triggers, doors: doors);

        // docs/systems/triggers.md 1
        [Test]
        public void ReachingAMarkedHexBringsTheWaveNextEnemyPhase()
        {
            var b = new Battle(Staged(Open, new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement("skeleton", 0, 0, wave: "gate") },
                new[] { new Trigger("gate", TriggerKind.Reach, 0, new[] { new Hex(3, 3) }) }), null, new[] { new Placement("pspear", 3, 5) });
            b.Start();
            b.EndTurn();
            Assert.AreEqual(1, b.Pending.Count, "not reached yet, no wave");
            var spear = b.Allies.Single();
            Assert.IsTrue(b.Move(spear.Id, new Hex(3, 3)).Ok);
            Assert.IsTrue(b.Fired("gate"));
            Assert.AreEqual(1, b.Pending.Count, "effects wait for the enemy phase");
            b.EndTurn();
            Assert.AreEqual(new Hex(0, 0), b.Enemies.Single(e => e.Kind == "skeleton").Pos);
        }

        [Test]
        public void KillsOpenADoor()
        {
            var map = new[] { ".......", "...#...", ".......", ".......", ".......", ".......", "......." };
            var b = new Battle(Staged(map, new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement("skeleton", 3, 4) },
                new[] { new Trigger("door", TriggerKind.Kills, 1) }, new[] { new Door(new[] { new Hex(3, 1) }, "door") }), null, new[] { new Placement("knight", 3, 5) });
            b.Start();
            var knight = b.Allies.Single();
            var skeleton = b.Enemies.Single(e => e.Kind == "skeleton");
            while (b.Enemies.Contains(skeleton) && b.Phase == Phase.Player) { b.Attack(knight.Id, skeleton.Id); if (b.Enemies.Contains(skeleton)) b.EndTurn(); }
            Assert.IsTrue(b.Fired("door"));
            Assert.AreEqual(Terrain.Rock, b.Board.At(new Hex(3, 1)));
            b.EndTurn();
            Assert.AreEqual(Terrain.Plain, b.Board.At(new Hex(3, 1)));
        }

        // docs/systems/triggers.md 2
        [Test]
        public void GuardWaitsUntilAnAllyComesNearAndHoldNeverMoves()
        {
            var b = new Battle(Field(new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement("skeleton", 3, 0, stance: Stance.Guard, alert: 3),
                new Placement("skeleton", 0, 0, stance: Stance.Hold) }), null, new[] { new Placement("pspear", 3, 6) });
            b.Start();
            b.EndTurn();
            var guard = b.Enemies.First(e => e.Stance == Stance.Guard);
            Assert.AreEqual(new Hex(3, 0), guard.Pos, "ally 6 away, guard 3: stays");
            var spear = b.Allies.Single();
            b.Move(spear.Id, new Hex(3, 3));
            b.EndTurn();
            Assert.AreNotEqual(new Hex(3, 0), guard.Pos, "ally within 3: wakes and charges");
            Assert.AreEqual(Stance.Charge, guard.Stance);
            Assert.AreEqual(new Hex(0, 0), b.Enemies.Single(e => e.Stance == Stance.Hold).Pos);
        }

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
