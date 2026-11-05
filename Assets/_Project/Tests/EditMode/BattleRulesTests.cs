using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    public class BattleRulesTests
    {
        static Battle Demo() => new Battle(GameData.Stages.First(s => s.Id == "demo"));

        [Test]
        public void CommandOutsideItsPhaseIsRejectedAndChangesNothing()
        {
            var b = Demo();
            var before = b.Snapshot();
            var knight = b.Allies.First(u => u.Kind == "knight");
            var skeleton = b.Enemies.First(u => u.Kind == "skeleton");
            Assert.IsFalse(b.Attack(knight.Id, skeleton.Id).Ok, "attack during deploy");
            Assert.IsFalse(b.EndTurn().Ok, "end turn during deploy");
            Assert.AreEqual(before, b.Snapshot());
            Assert.IsTrue(b.Start().Ok);
            Assert.IsFalse(b.Start().Ok, "start twice");
        }

        [Test]
        public void DamageIsAtLeastOne()
        {
            var b = new Battle(GameData.Stages.First(s => s.Id == "s7"), new[] { "knight_def" });
            var knight = b.Allies.First(u => u.Kind == "knight");
            var skeleton = b.Enemies.First(u => u.Kind == "skeleton");
            Assert.AreEqual(3, knight.Def, "knight 2 + plate armour 1");
            Assert.AreEqual(1, b.Damage(skeleton, knight), "skeleton attack 2 against defence 3 still deals 1");
        }

        [Test]
        public void PlacingPastThePopulationCapIsRejectedWithThePrototypeText()
        {
            var b = Demo();
            var free = Enumerable.Range(0, b.Board.Rows).SelectMany(r => Enumerable.Range(0, b.Board.Cols).Select(c => new Hex(c, r)))
                .First(h => b.InDeploy(h) && b.UnitAt(h) == null);
            Assert.AreEqual(10, b.Used(t => t.Pop), "sample formation fills the demo's population of 10");
            var result = b.Place(UnitKind.PSpear, free);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual("인구가 부족합니다. 배치한 유닛을 구역 밖으로 끌어내 회수하세요.", result.Reason);
            Assert.IsNull(b.UnitAt(free));
        }

        [Test]
        public void AnAllyCannotExecuteItselfOrMoveOntoAnotherUnit()
        {
            var b = Demo();
            b.Start();
            var knight = b.Allies.First(u => u.Kind == "knight");
            var sword = b.Allies.First(u => u.Kind == "sword");
            Assert.IsFalse(b.Execute(knight.Id, knight.Id).Ok, "self execute");
            Assert.IsFalse(b.Move(knight.Id, sword.Pos).Ok, "move onto an ally");
            Assert.IsTrue(b.Execute(knight.Id, sword.Id).Ok, "adjacent ally can be executed");
            Assert.AreEqual(1, b.Stats.Executed);
            Assert.AreEqual(0, b.Corpses.Count, "an executed ally leaves no corpse");
        }

        [Test]
        public void JosaFollowsTheFinalConsonant()
        {
            Assert.AreEqual("기사가", Korean.Josa("기사", "이", "가"));
            Assert.AreEqual("검사 2이", Korean.Josa("검사 2", "이", "가"));
            Assert.AreEqual("스폰(궁수)을", Korean.Josa("스폰(궁수)", "을", "를"));
            Assert.AreEqual("뱀파이어 백작을", Korean.Josa("뱀파이어 백작", "을", "를"));
        }
    }
}
