using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game.Screens;

namespace VampireSpawn.Tests
{
    // docs/ui/battle-hud.md 6-2 (#150 D3).
    public class RestartTests
    {
        [TearDown] public void BackToShipped() => GameData.Use(null);

        [Test]
        public void RestartArmsOnTheFirstPressAndDisarmsLeave()
        {
            var arm = new TwoPress();
            Assert.IsFalse(arm.Press(Arm.Leave, 0f, 2.5f));
            Assert.AreEqual(Arm.Leave, arm.Armed);
            Assert.IsFalse(arm.Press(Arm.Restart, 1f, 2.5f), "pressing the other button arms it, it does not confirm");
            Assert.AreEqual(Arm.Restart, arm.Armed, "only one is armed");
            Assert.IsTrue(arm.Press(Arm.Restart, 3f, 2.5f), "second press inside the window");
            Assert.AreEqual(Arm.None, arm.Armed);
        }

        [Test]
        public void TheArmRunsOut()
        {
            var arm = new TwoPress();
            arm.Press(Arm.Restart, 0f, 2.5f);
            Assert.IsFalse(arm.Expire(2f));
            Assert.IsTrue(arm.Expire(2.6f));
            Assert.AreEqual(Arm.None, arm.Armed);
            Assert.IsFalse(arm.Press(Arm.Restart, 2.7f, 2.5f), "after running out a press only arms again");
        }

        [Test]
        public void RestartingFromTheStartFormationIsTurnOneWithTheSameAlliesAndNoMoon()
        {
            var stage = GameData.Stages[0];
            var b = new Battle(stage, null, stage.Sample);
            b.Start();
            var start = b.Allies.Select(u => u.Type + "@" + u.Pos).OrderBy(x => x).ToArray();
            var u0 = b.Allies.First();
            var to = b.Reach(u0).Keys.First(h => !h.Equals(u0.Pos));
            Assert.IsTrue(b.Move(u0.Id, to).Ok);
            b.EndTurn();
            Assert.Greater(b.Turn, 1);

            var again = new Battle(stage, null, b.StartFormation);
            again.Start();
            Assert.AreEqual(1, again.Turn);
            Assert.AreEqual(0, again.Moon);
            Assert.AreEqual(Phase.Player, again.Phase);
            CollectionAssert.AreEqual(start, again.Allies.Select(u => u.Type + "@" + u.Pos).OrderBy(x => x).ToArray());
        }
    }
}
