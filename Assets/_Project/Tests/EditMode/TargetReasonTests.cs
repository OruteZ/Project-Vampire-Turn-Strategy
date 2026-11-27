using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    // docs/systems/ai.md 6: the enemy's hit line says why it picked that ally.
    public class TargetReasonTests
    {
        static readonly string[] Open = Enumerable.Repeat(".......", 7).ToArray();
        static readonly Hex Foe = new Hex(3, 2);

        static Battle Hit(string enemy, params string[] allies)
        {
            var board = new Board(Open);
            var near = board.Neighbors(Foe).Where(board.InBounds).ToList();
            var stage = new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Boss, new Bonus(BonusType.NoEat, 0, 0),
                new Boss(30, 1, 0, 0, 0), Open, new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement(enemy, Foe.Col, Foe.Row) }, new Placement[0]);
            var b = new Battle(stage, null, allies.Select((k, i) => new Placement(k, near[i].Col, near[i].Row)));
            b.Start();
            b.EndTurn();
            return b;
        }

        static string[] Tags(Battle b)
        {
            var line = b.Log.Select(l => l.Text).First(m => m.Key == "log.hit" || m.Key == "log.hitTags");
            return line.Key == "log.hit" ? new string[0] : ((Message)line.Args[3]).Args.Cast<Message>().Select(t => t.Key).ToArray();
        }

        [Test]
        public void TauntWhenTheKnightWasTheOnlyChoice() => CollectionAssert.AreEqual(new[] { "tag.taunt" }, Tags(Hit("skeleton", "pspear", "knight")));

        [Test]
        public void LethalWhenOneBlowKills() => CollectionAssert.AreEqual(new[] { "tag.lethal" }, Tags(Hit("deathKnight", "pspear", "parcher")));

        [Test]
        public void HighValueWhenItAloneCostsTheMostPopulation() => CollectionAssert.AreEqual(new[] { "tag.highValue" }, Tags(Hit("skeleton", "pspear", "sword")));

        [Test]
        public void WoundedWhenItAloneHasTheLeastHealth() => CollectionAssert.AreEqual(new[] { "tag.wounded" }, Tags(Hit("skeleton", "pspear", "parcher")));

        [Test]
        public void NoReasonForALoneTargetOrATie()
        {
            CollectionAssert.IsEmpty(Tags(Hit("skeleton", "pspear")));
            CollectionAssert.IsEmpty(Tags(Hit("skeleton", "pspear", "pspear")));
        }
    }
}
