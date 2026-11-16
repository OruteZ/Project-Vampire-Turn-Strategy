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
            var b = new Battle(GameData.Stages.First(s => s.Id == "s8"), new[] { "knight_def" });
            var knight = b.Allies.First(u => u.Kind == "knight");
            var skeleton = b.Enemies.First(u => u.Kind == "skeleton");
            Assert.AreEqual(2, knight.Def, "knight 1 + plate armour 1");
            Assert.AreEqual(1, b.Damage(skeleton, knight), "skeleton attack 2 against defence 2 still deals 1");
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
        public void ClearAlliesOfAKindKeepsTheOthers()
        {
            var b = Demo();
            var kind = b.Allies.First().Type;
            var others = b.Allies.Count(u => u.Type != kind);
            Assert.Greater(others, 0, "demo sample mixes kinds");
            Assert.IsTrue(b.ClearAllies(kind).Ok);
            Assert.IsFalse(b.Allies.Any(u => u.Type == kind));
            Assert.AreEqual(others, b.Allies.Count());
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

        [Test]
        public void ConsecutiveWalksWithNoSharedHexPlayTogether()
        {
            static Hex H(int c, int r) => new Hex(c, r);
            static BattleEvent W(int id, Hex from, params Hex[] path) => BattleEvent.Walk(id, path, from);
            var hit = BattleEvent.Hit(1, 2);
            var apart = new[] { W(1, H(0, 0), H(0, 1)), W(2, H(5, 5), H(5, 6)), W(3, H(9, 9), H(9, 8)), hit, W(4, H(2, 2), H(2, 3)) };
            Assert.AreEqual(3, BattleEvent.WalkRun(apart, 0), "three apart walks, cut by the hit");
            Assert.AreEqual(0, BattleEvent.WalkRun(apart, 3), "not a walk");
            Assert.AreEqual(1, BattleEvent.WalkRun(apart, 4), "last walk alone");
            var vacated = new[] { W(1, H(0, 0), H(0, 1)), W(2, H(1, 0), H(0, 0)) };
            Assert.AreEqual(1, BattleEvent.WalkRun(vacated, 0), "second walks into the hex the first left");
            var crossing = new[] { W(1, H(0, 0), H(1, 1), H(2, 2)), W(2, H(2, 0), H(1, 1), H(0, 2)) };
            Assert.AreEqual(1, BattleEvent.WalkRun(crossing, 0), "paths cross");
        }

        [Test]
        public void EnemyWalkStartsWhereTheUnitStood()
        {
            var b = Demo();
            Assert.IsTrue(b.Start().Ok);
            var before = b.Enemies.ToDictionary(u => u.Id, u => u.Pos);
            var walks = b.EndTurn().Events.Where(e => e.Kind == BattleEventKind.Walk && before.ContainsKey(e.UnitId)).ToList();
            Assert.IsNotEmpty(walks);
            foreach (var w in walks.GroupBy(w => w.UnitId).Select(g => g.First()))
                Assert.AreEqual(before[w.UnitId], w.At);
        }
    }
}
