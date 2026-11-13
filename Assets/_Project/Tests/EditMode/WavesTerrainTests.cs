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

        // docs/systems/goals.md 1-2, 1-7
        [Test]
        public void AStageWithoutAVampireEndsOnlyOnItsOwnGoal()
        {
            var stage = new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Defense, new Bonus(BonusType.NoEat, 0, 0), new Boss(1, 1, 0, 0, 0), Open,
                new[] { new Placement("skeleton", 6, 0, stance: Stance.Hold) }, new Placement[0], spots: new[] { new Hex(3, 6) }, hold: 2);
            Assert.IsFalse(stage.HasBoss);
            var b = new Battle(stage, null, new[] { new Placement("pspear", 3, 6) });
            b.Start();
            Assert.AreEqual(Phase.Player, b.Phase, "no vampire is not a win");
            b.EndTurn();
            b.EndTurn();
            Assert.AreEqual(Ending.Held, b.Ending);
        }

        [Test]
        public void KillingTheVampireDoesNotEndADefence()
        {
            var stage = new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Defense, new Bonus(BonusType.NoEat, 0, 0), new Boss(1, 1, 0, 0, 0), Open,
                new[] { new Placement(EnemyType.Vampire, 3, 2) }, new Placement[0], spots: new[] { new Hex(3, 6) }, hold: 9);
            GameData.Use(new DataSet(new RuleSet(GameData.Current.Rules.DrainHeal, 6, 2, 1, 2), GameData.Types, GameData.Upgrades, GameData.Minions, GameData.Stages));
            var b = new Battle(stage, null, new[] { new Placement("knight", 3, 3), new Placement("pspear", 3, 6) });
            b.Start();
            b.EndTurn();
            var knight = b.Allies.First(a => a.Type == "knight");
            if (b.Vampire != null && Hex.Distance(knight.Pos, b.Vampire.Pos) == 1) b.Attack(knight.Id, b.Vampire.Id);
            Assert.IsNull(b.Vampire, "boss with 1 hp falls to the knight");
            Assert.IsNull(b.Ending, "the defence goes on");
        }

        // docs/systems/reinforce.md
        [Test]
        public void ReachingTheGoalOpensAMidBattleDeployOnlyForNewcomers()
        {
            var map = new[] { ".......", ".......", ".......", ".......", ".......", "ddddddd", "ddddddd" };
            var stage = new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 2, 0, 0, Goal.Boss, new Bonus(BonusType.NoEat, 0, 0), new Boss(30, 1, 0, 0, 0), map,
                new[] { new Placement(EnemyType.Vampire, 6, 0) }, new Placement[0],
                triggers: new[] { new Trigger("bell", TriggerKind.Reach, 0, new[] { new Hex(3, 4) }) },
                reinforcements: new[] { new Reinforcement("bell", 3, 0, new[] { new Hex(0, 0), new Hex(1, 0), new Hex(2, 0) }) });
            var b = new Battle(stage, null, new[] { new Placement("pspear", 3, 5), new Placement("pspear", 4, 5) });
            b.Start();
            var first = b.Allies.First();
            Assert.IsTrue(b.Move(first.Id, new Hex(3, 4)).Ok);
            b.EndTurn();
            Assert.AreEqual(Phase.Deploy, b.Phase);
            Assert.IsTrue(b.Reinforcing);
            Assert.AreEqual(5, b.PopCap, "2 + 3");
            Assert.IsFalse(b.Place("pspear", new Hex(3, 6)).Ok, "the old deploy zone is closed");
            Assert.IsTrue(b.Place("sword", new Hex(0, 0)).Ok);
            Assert.IsTrue(b.Place("pspear", new Hex(1, 0)).Ok);
            Assert.IsFalse(b.Place("pspear", new Hex(2, 0)).Ok, "pop 2 + 2 + 1 = 5, full");
            Assert.IsFalse(b.Recall(first.Id).Ok, "allies already fighting stay");
            Assert.IsFalse(b.UseSample().Ok);
            Assert.IsTrue(b.Start().Ok);
            Assert.AreEqual(Phase.Player, b.Phase);
            var sword = b.Allies.Single(a => a.Type == "sword");
            Assert.IsTrue(b.Move(sword.Id, new Hex(0, 1)).Ok, "a newcomer acts on the turn it lands");
        }

        [Test]
        public void LostUnitsFreeTheirPopulation()
        {
            var stage = new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 3, 0, 0, Goal.Boss, new Bonus(BonusType.NoEat, 0, 0), new Boss(30, 1, 0, 0, 0), Open,
                new[] { new Placement(EnemyType.Vampire, 6, 6) }, new Placement[0],
                triggers: new[] { new Trigger("t", TriggerKind.Turn, 1) },
                reinforcements: new[] { new Reinforcement("t", 1, 0, new[] { new Hex(0, 0) }) });
            var b = new Battle(stage, null, new[] { new Placement("pspear", 3, 2), new Placement("pspear", 3, 4), new Placement("parcher", 3, 3) });
            b.Start();
            var archer = b.Allies.Single(a => a.Type == "parcher");
            foreach (var spear in b.Allies.Where(a => a.Type == "pspear").ToList())
                Assert.IsTrue(b.Execute(spear.Id, archer.Id).Ok);
            Assert.IsFalse(b.Allies.Contains(archer), "2 + 2 against 4 hp");
            Assert.AreEqual(1, b.Stats.Executed);
            Assert.AreEqual(0, b.Corpses.Count, "an ally killed by an ally leaves no corpse");
            b.EndTurn();
            Assert.IsTrue(b.Reinforcing);
            Assert.AreEqual(2, b.Used(t => t.Pop));
            Assert.IsTrue(b.Place("sword", new Hex(0, 0)).Ok, "cap 4, two spearmen alive: room for a sword");
        }

        // docs/systems/triggers.md 1
        [Test]
        public void ReachingAMarkedHexBringsTheWaveNextEnemyPhase()
        {
            var b = new Battle(Staged(Open, new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement("skeleton", 0, 0, wave: "gate") },
                new[] { new Trigger("gate", TriggerKind.Reach, 0, new[] { new Hex(3, 3) }) }), null, new[] { new Placement("pspear", 3, 4) });
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

        // docs/content/stages.md 1-3: '_' is a missing cell, not a wall. Nothing stands on it, but shots fly over it.
        [Test]
        public void AVoidCellHoldsNothingButShotsCrossIt()
        {
            var map = new[] { ".......", ".......", ".......", "..._...", ".......", "ddddddd", "ddd_ddd" };
            var b = new Battle(Staged(map, new[] { new Placement(EnemyType.Vampire, 6, 0), new Placement("skeleton", 3, 2, stance: Stance.Hold) }, new Trigger[0]), null, new[] { new Placement("parcher", 3, 5) });
            Assert.AreEqual(Terrain.Void, b.Board.At(new Hex(3, 3)));
            Assert.IsFalse(b.Board.Walkable(new Hex(3, 3)));
            Assert.IsFalse(b.InDeploy(new Hex(3, 6)), "no deploying onto a void");
            Assert.IsFalse(b.Place("pspear", new Hex(3, 6)).Ok);
            b.Start();
            var archer = b.Allies.Single();
            Assert.IsFalse(b.Move(archer.Id, new Hex(3, 3)).Ok, "no walking onto a void");
            Assert.IsTrue(b.Board.Reachable(archer.Pos, 9, Side.Ally, b.OccupantAt).All(r => b.Board.At(r.hex) != Terrain.Void));
            Assert.IsTrue(b.Move(archer.Id, new Hex(3, 4)).Ok);
            var skeleton = b.Enemies.Single(e => e.Kind == "skeleton");
            Assert.IsTrue(b.Attack(archer.Id, skeleton.Id).Ok, "range 2 over the void");
        }

        [Test]
        public void EveryShippedStageEndsUnderTheBot()
        {
            foreach (var s in GameData.Stages)
            {
                var r = StageSim.Play(s);
                Assert.IsTrue(r.End.HasValue, s.Id + " ends within " + StageSim.TurnCap + " turns");
            }
        }

        // docs/systems/triggers.md 2
        [Test]
        public void GuardWaitsUntilAnAllyComesNearAndHoldNeverMoves()
        {
            var b = new Battle(Field(new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement("skeleton", 3, 0, stance: Stance.Guard, alert: 3),
                new Placement("skeleton", 0, 0, stance: Stance.Hold) }), null, new[] { new Placement("pspear", 3, 4) });
            b.Start();
            b.EndTurn();
            var guard = b.Enemies.First(e => e.Stance == Stance.Guard);
            Assert.AreEqual(new Hex(3, 0), guard.Pos, "ally 4 away, guard 3: stays");
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
            Assert.IsTrue(board.Reachable(new Hex(1, 0), 1, Side.Ally, Empty).Any(r => r.hex == new Hex(2, 0)), "a first step is always allowed, even onto rough");
            Assert.IsFalse(board.Reachable(new Hex(0, 0), 2, Side.Ally, Empty).Any(r => r.hex == new Hex(2, 0)), "after a plain step, rough still costs 2");
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
