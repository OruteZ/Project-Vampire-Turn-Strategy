using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    public class BattleRulesTests
    {
        static Battle Demo() => new Battle(GameData.Stages.First(s => s.Id == "demo"));

        [Test]
        public void CommandOutsideItsPhaseIsRejectedAndChangesNothing()
        {
            var b = Demo();
            var before = b.Snapshot(m => m.ToString());
            var knight = b.Allies.First(u => u.Kind == "knight");
            var skeleton = b.Enemies.First(u => u.Kind == "skeleton");
            Assert.IsFalse(b.Attack(knight.Id, skeleton.Id).Ok, "attack during deploy");
            Assert.IsFalse(b.EndTurn().Ok, "end turn during deploy");
            Assert.AreEqual(before, b.Snapshot(m => m.ToString()));
            Assert.IsTrue(b.Start().Ok);
            Assert.IsFalse(b.Start().Ok, "start twice");
        }

        [Test]
        public void DamageIsAtLeastOne()
        {
            var b = new Battle(GameData.Stages.First(s => s.Id == "s7"), new[] { "knight_def" });
            var knight = b.Allies.First(u => u.Kind == "knight");
            var skeleton = b.Enemies.First(u => u.Kind == "skeleton");
            Assert.AreEqual(4, knight.Def, "knight 2 + plate armour 2");
            Assert.AreEqual(1, b.Damage(skeleton, knight), "skeleton attack 2 against defence 4 still deals 1");
        }

        [Test]
        public void PlacingPastThePopulationCapIsRejected()
        {
            var b = Demo();
            var free = Enumerable.Range(0, b.Board.Rows).SelectMany(r => Enumerable.Range(0, b.Board.Cols).Select(c => new Hex(c, r)))
                .First(h => b.InDeploy(h) && b.UnitAt(h) == null);
            Assert.AreEqual(10, b.Used(t => t.Pop), "sample formation fills the demo's population of 10");
            var result = b.Place("pspear", free);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual("인구가 부족합니다. 배치한 유닛을 구역 밖으로 끌어내 회수하세요.", Loc.Text(result.Reason));
            Assert.IsNull(b.UnitAt(free));
        }

        [Test]
        public void AttackingAnAllyDealsAttackDamageAndUsesTheAttack()
        {
            var b = Demo();
            b.Start();
            var knight = b.Allies.First(u => u.Kind == "knight");
            var sword = b.Allies.First(u => u.Kind == "sword");
            Assert.IsFalse(b.Execute(knight.Id, knight.Id).Ok, "self attack");
            Assert.IsFalse(b.Move(knight.Id, sword.Pos).Ok, "move onto an ally");
            var hp = sword.Hp;
            var damage = b.Damage(knight, sword);
            Assert.IsTrue(b.Execute(knight.Id, sword.Id).Ok, "adjacent ally can be attacked");
            Assert.AreEqual(hp - damage, sword.Hp, "same damage as an attack, not an instant kill");
            Assert.IsTrue(b.Allies.Contains(sword));
            Assert.AreEqual(0, b.Stats.Executed);
            Assert.IsTrue(knight.Acted && knight.Moved, "it spends the attack");
            Assert.IsFalse(b.Execute(knight.Id, sword.Id).Ok, "once per turn");
        }

        static string Josa(string word, string withFinal, string withoutFinal) => word + JosaFormatter.Pick(word, withFinal, withoutFinal);

        [Test]
        public void JosaFollowsTheFinalConsonant()
        {
            Assert.AreEqual("기사가", Josa("기사", "이", "가"));
            Assert.AreEqual("검사 2이", Josa("검사 2", "이", "가"));
            Assert.AreEqual("스폰(궁수)을", Josa("스폰(궁수)", "을", "를"));
            Assert.AreEqual("뱀파이어 백작을", Josa("뱀파이어 백작", "을", "를"));
        }
    }
}
