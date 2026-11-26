using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // docs/ui/battle-hud.md 3-8 (#150 B1).
    public class UndoMoveTests
    {
        [TearDown] public void BackToShipped() => GameData.Use(null);

        static readonly string[] Open = Enumerable.Repeat(".......", 7).ToArray();

        static Stage Field(Placement[] enemies, Trigger[] triggers = null) => new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Boss,
            new Bonus(BonusType.NoEat, 0, 0), new Boss(30, 1, 0, 0, 0), Open, enemies, new Placement[0], triggers: triggers);

        static Placement P(string kind, int col, int row) => new Placement(kind, col, row);

        static string State(Battle b) => b.Snapshot(Loc.Text) + "\nlog=" + string.Join("|", b.Log.Select(l => Loc.Text(l.Text)));

        [Test]
        public void UndoingAMoveRestoresTheBattleAndSelectsTheUnit()
        {
            var s = new BattleSession(new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6) }), null, new[] { P("knight", 3, 3) }));
            s.Start();
            var b = s.Battle;
            var knight = b.Allies.Single();
            var before = State(b);
            var reach = b.Reach(knight).Count;
            Assert.IsFalse(b.CanUndo, "nothing moved yet");
            s.Click(knight.Pos);
            Assert.IsTrue(s.Click(new Hex(3, 5)).Ok);
            Assert.AreEqual(new Hex(3, 5), knight.Pos);
            Assert.IsTrue(b.CanUndo);
            s.Escape();
            Assert.IsTrue(s.Undo().Ok);
            Assert.AreEqual(before, State(b), "position, moves left, log");
            Assert.AreEqual(reach, b.Reach(knight).Count);
            Assert.AreEqual(knight.Id, s.Selected, "the unit that went back is selected");
            Assert.IsFalse(b.CanUndo);
            Assert.IsFalse(s.Undo().Ok);
        }

        [Test]
        public void MovesUndoLastFirst()
        {
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6) }), null, new[] { P("knight", 3, 3), P("sword", 1, 3) });
            b.Start();
            var knight = b.Allies.First(u => u.Type == "knight");
            var sword = b.Allies.First(u => u.Type == "sword");
            var before = State(b);
            Assert.IsTrue(b.Move(knight.Id, new Hex(3, 5)).Ok);
            Assert.IsTrue(b.Move(sword.Id, new Hex(1, 4)).Ok);
            Assert.IsTrue(b.UndoMove().Ok);
            Assert.AreEqual(new Hex(1, 3), sword.Pos);
            Assert.AreEqual(new Hex(3, 5), knight.Pos, "only the last move went back");
            Assert.IsTrue(b.UndoMove().Ok);
            Assert.AreEqual(before, State(b));
        }

        [Test]
        public void AnAttackClearsTheUndo()
        {
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 0) }), null, new[] { P("knight", 3, 3), P("sword", 0, 6) });
            b.Start();
            var knight = b.Allies.First(u => u.Type == "knight");
            Assert.IsTrue(b.Move(knight.Id, new Hex(3, 1)).Ok);
            Assert.IsTrue(b.Attack(knight.Id, b.Enemies.First(e => e.Kind == "skeleton").Id).Ok);
            Assert.IsFalse(b.CanUndo);
            Assert.IsFalse(b.UndoMove().Ok);
        }

        [Test]
        public void EndingTheTurnClearsTheUndo()
        {
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6) }), null, new[] { P("knight", 3, 3) });
            b.Start();
            Assert.IsTrue(b.Move(b.Allies.Single().Id, new Hex(3, 5)).Ok);
            b.EndTurn();
            Assert.AreEqual(Phase.Player, b.Phase);
            Assert.IsFalse(b.CanUndo);
        }

        [Test]
        public void UndoingAStepOntoAMarkedHexCancelsTheTrigger()
        {
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6), new Placement("skeleton", 0, 0, wave: "gate") },
                new[] { new Trigger("gate", TriggerKind.Reach, 0, new[] { new Hex(3, 3) }) }), null, new[] { P("pspear", 3, 4) });
            b.Start();
            var spear = b.Allies.Single();
            Assert.IsTrue(b.Move(spear.Id, new Hex(3, 3)).Ok);
            Assert.IsTrue(b.Fired("gate"));
            Assert.IsTrue(b.UndoMove().Ok);
            Assert.IsFalse(b.Fired("gate"));
            b.EndTurn();
            Assert.AreEqual(1, b.Pending.Count, "the wave did not come");
        }

        [Test]
        public void AnEscortThatArrivedCannotBeUndone()
        {
            var s = Field(new[] { P(EnemyType.Vampire, 6, 6) });
            s = new Stage(s.Id, s.Units, s.Pop, s.Silver, s.Reward, Goal.Escort, s.Bonus, s.Boss, s.Map, s.Enemies, s.Sample,
                new[] { new Hex(3, 0) }, 0, new EscortStats(6, 0, 3, new Hex(3, 3)));
            var b = new Battle(s, null, new[] { P("knight", 3, 4) });
            b.Start();
            Assert.IsTrue(b.Move(b.Escort.Id, new Hex(3, 0)).Ok);
            Assert.AreEqual(Ending.Arrived, b.Ending);
            Assert.IsFalse(b.CanUndo);
            Assert.IsFalse(b.UndoMove().Ok);
        }
    }
}
