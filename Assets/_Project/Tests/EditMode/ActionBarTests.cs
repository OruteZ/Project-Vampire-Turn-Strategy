using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game;
using VampireSpawn.Game.Screens;

namespace VampireSpawn.Tests
{
    // docs/ui/battle-hud.md 3-4: the action bar slots and their keys.
    public class ActionBarTests
    {
        static readonly string[] Open = Enumerable.Repeat(".......", 7).ToArray();

        static BattleSession Field(params Placement[] more)
        {
            var stage = new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Boss,
                new Bonus(BonusType.NoEat, 0, 0), new Boss(30, 1, 0, 0, 0), Open, new[] { new Placement(EnemyType.Vampire, 6, 6) }, new Placement[0]);
            var b = new Battle(stage, null, new[] { new Placement("doctor", 1, 1), new Placement("hunter", 3, 1), new Placement("gravedigger", 5, 1) }.Concat(more).ToArray());
            b.Start();
            return new BattleSession(b);
        }

        [Test]
        public void SlotsFollowTheUnitAndItsState()
        {
            var s = Field();
            var a = new BattleActions();
            Assert.IsEmpty(BattleScreen.Slots(s, a), "nothing selected, no bar");

            s.Click(new Hex(1, 1));
            var doc = BattleScreen.Slots(s, a);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, -1 }, doc.Select(x => x.Key));
            Assert.AreEqual("Z", doc[5].Hotkey);
            Assert.IsFalse(doc[5].Ready, "nothing to undo");
            Assert.IsTrue(doc[0].Ready, "can still move");
            Assert.IsFalse(doc[1].Ready, "no enemy in range");
            Assert.AreEqual(Loc.T("hud.mend"), doc[4].Label);

            s.Toggle(SelectMode.Mend);
            Assert.IsTrue(BattleScreen.Slots(s, a)[4].On);
            s.ClearMode();
            Assert.AreEqual(SelectMode.Act, s.Mode);

            var to = s.Battle.Reach(s.SelectedUnit).Keys.First(h => s.Battle.UnitAt(h) == null);
            s.Click(to);
            Assert.IsFalse(BattleScreen.Slots(s, a)[0].Ready, "moved already");
            Assert.IsTrue(BattleScreen.Slots(s, a)[5].Ready, "the move can be undone");

            s.Click(new Hex(3, 1));
            var hunter = BattleScreen.Slots(s, a);
            Assert.IsFalse(hunter.Any(x => x.Key == 5));
            Assert.IsFalse(hunter.Any(x => x.Key == 0), "skills that work by themselves are not action cells (#153)");
            CollectionAssert.Contains(BattleScreen.Passives(s.SelectedUnit), BattleScreen.SkillName(Ability.Splash), "splash shows in the unit block");

            s.Click(new Hex(5, 1));
            var digger = BattleScreen.Slots(s, a).First(x => x.Key == 5);
            Assert.AreEqual(BattleScreen.SkillName(Ability.Bury), digger.Label);
            Assert.IsFalse(digger.Ready, "no corpse next to it");
        }

        [Test]
        public void TrapSlotCountsTheScoutsTrapsLeft()
        {
            var s = Field(new Placement("scout", 1, 3));
            var a = new BattleActions();
            s.Click(new Hex(1, 3));
            var scout = s.SelectedUnit;
            Assert.AreEqual("3/3", BattleScreen.Slots(s, a).First(x => x.Key == 5).Count);
            Assert.IsTrue(s.Battle.SetTrap(scout.Id, new Hex(1, 4)).Ok);
            s.Battle.EndTurn();
            s.Click(new Hex(1, 3));
            var trap = BattleScreen.Slots(s, a).First(x => x.Key == 5);
            Assert.AreEqual("2/3", trap.Count);
            Assert.IsTrue(trap.Ready);
        }
    }
}
