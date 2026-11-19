using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    // docs/art/ui-style.md 8-6: the aim arrow from a selected ranged ally to the hovered enemy.
    public class AimTests
    {
        static BattleSession Field(string ally, bool wall = false)
        {
            var map = Enumerable.Repeat(".......", 7).ToArray();
            if (wall) map[2] = "...#...";
            var enemies = new[] { new Placement(EnemyType.Vampire, 4, 1), new Placement("skeleton", 3, 3), new Placement("skeleton", 3, 6) };
            var stage = new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Boss,
                new Bonus(BonusType.NoEat, 0, 0), new Boss(30, 1, 0, 0, 0), map, enemies, new Placement[0]);
            var b = new Battle(stage, null, new[] { new Placement(ally, 3, 1) });
            b.Start();
            return new BattleSession(b);
        }

        static readonly Hex Shooter = new Hex(3, 1), Near = new Hex(3, 3), Far = new Hex(3, 6), Sleeper = new Hex(4, 1);

        [Test]
        public void ArrowIsClearOnlyWhenTheShotCanBeTaken()
        {
            var s = Field("archer");
            Assert.IsNull(s.AimAt(Near), "nothing selected");
            s.Click(Shooter);
            Assume.That(s.SelectedUnit.Rng, Is.InRange(2, 4));

            Assert.AreEqual((Shooter, Near, true), s.AimAt(Near));
            Assert.AreEqual((Shooter, Far, false), s.AimAt(Far), "out of range");
            Assert.AreEqual((Shooter, Sleeper, false), s.AimAt(Sleeper), "asleep, though next to it");
            Assert.IsNull(s.AimAt(new Hex(0, 0)), "empty cell");
            Assert.IsNull(s.AimAt(null));

            var walled = Field("archer", wall: true);
            walled.Click(Shooter);
            Assert.AreEqual((Shooter, Near, false), walled.AimAt(Near), "a wall blocks the line");
        }

        [Test]
        public void NoArrowForMeleeOrSpentOrOtherModes()
        {
            var melee = Field("knight");
            melee.Click(Shooter);
            Assert.IsNull(melee.AimAt(Near), "melee units do not aim");

            var s = Field("archer");
            s.Click(Shooter);
            s.ToggleExecute();
            Assert.IsNull(s.AimAt(Near), "execute mode aims at allies");
            s.ClearMode();
            s.Click(Near);
            Assert.IsNull(s.AimAt(Near), "already shot");
        }
    }
}
