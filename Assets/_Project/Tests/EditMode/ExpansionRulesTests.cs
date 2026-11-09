using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    // #99 rules: docs/content/units.md 4, docs/systems/ai.md 2 and 5, docs/systems/progression.md 5-17.
    public class ExpansionRulesTests
    {
        [TearDown] public void BackToShipped() => GameData.Use(null);

        static readonly string[] Open = Enumerable.Repeat(".......", 7).ToArray();

        static Stage Field(Placement[] enemies, int bossAtk = 1) => new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0,
            new Bonus(BonusType.NoEat, 0, 0), new Boss(30, bossAtk, 0, 0, 0), Open, enemies, new Placement[0]);

        static Placement P(string kind, int col, int row) => new Placement(kind, col, row);

        static Hex At(int col, int row) => new Hex(col, row);

        static Unit Ally(Battle b, int col, int row) => b.UnitAt(At(col, row));

        static void Rules(int moonMax, int eatRange, int moonWake)
        {
            var d = GameData.Current;
            GameData.Use(new DataSet(new RuleSet(d.Rules.DrainHeal, moonMax, eatRange, moonWake, d.Rules.MendRange), d.Types, d.Upgrades, d.Minions, d.Stages));
        }

        // A neighbour of h, and a hex next to that neighbour but two away from h.
        static (Hex near, Hex beyond) Line(Hex h)
        {
            var board = new Board(Open);
            var near = board.Neighbors(h).First(n => board.InBounds(n));
            var beyond = board.Neighbors(near).First(n => board.InBounds(n) && Hex.Distance(n, h) == 2);
            return (near, beyond);
        }

        [Test]
        public void CloseRanksAddsOneDefenceOnlyNextToAnotherSpearman()
        {
            var lone = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 2) }), new[] { "pspear_formation" }, new[] { P("pspear", 3, 3) });
            var skeleton = lone.Enemies.First(e => e.Kind == "skeleton");
            Assert.AreEqual(2, lone.Damage(skeleton, Ally(lone, 3, 3)), "alone: attack 2 against defence 0");
            var (near, _) = Line(At(3, 3));
            var pair = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 2) }), new[] { "pspear_formation" },
                new[] { P("pspear", 3, 3), P("pspear", near.Col, near.Row) });
            var parts = pair.DamageParts(pair.Enemies.First(e => e.Kind == "skeleton"), Ally(pair, 3, 3));
            Assert.AreEqual(1, parts.damage, "next to another spearman: defence 1");
            Assert.IsTrue(parts.tags.Any(t => t.Key == "tag.formation"));
        }

        [Test]
        public void RangedMinionShootsFromWhereItStands()
        {
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6), P("skeletonArcher", 3, 1) }), null, new[] { P("knight", 3, 3) });
            var archer = b.Enemies.First(e => e.Kind == "skeletonArcher");
            var knight = Ally(b, 3, 3);
            Assert.AreEqual(2, Hex.Distance(archer.Pos, knight.Pos));
            Assert.AreEqual(GameData.Minion("skeletonArcher").Rng, archer.Rng, "range comes from the data");
            b.Start();
            var hp = knight.Hp;
            b.EndTurn();
            Assert.AreEqual(At(3, 1), archer.Pos, "in reach, so it does not move");
            Assert.AreEqual(hp - b.Damage(archer, knight), knight.Hp);
        }

        [Test]
        public void HolyWaterIsNeverTheFullMoonsPrey()
        {
            Rules(moonMax: 2, eatRange: 9, moonWake: 1);
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 3, 0) }), new[] { "hunter_ward" }, new[] { P("hunter", 3, 2), P("pspear", 3, 6) });
            b.Start();
            b.EndTurn();
            Assert.IsTrue(b.MoonNext());
            Assert.AreEqual(Ally(b, 3, 6).Id, b.EatTarget, "the far spearman, not the warded hunter next to the vampire");
        }

        [Test]
        public void BuryingRemovesTheBodyAndHolyBurialWanesTheMoon()
        {
            Rules(moonMax: 6, eatRange: 2, moonWake: 1);
            var victim = At(3, 3);
            var board = new Board(Open);
            var vampireAt = board.Neighbors(victim).First();
            var diggerAt = board.Neighbors(victim).First(n => Hex.Distance(n, vampireAt) == 2);
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, vampireAt.Col, vampireAt.Row) }, bossAtk: 99),
                new[] { "gravedigger_rite", "gravedigger_twice" }, new[] { P("doctor", victim.Col, victim.Row), P("gravedigger", diggerAt.Col, diggerAt.Row) });
            var digger = Ally(b, diggerAt.Col, diggerAt.Row);
            b.Start();
            b.EndTurn();
            b.EndTurn();
            var corpse = b.CorpseAt(victim);
            Assert.IsNotNull(corpse, "the vampire killed the doctor in the dark");
            var moon = b.Moon;
            var r = b.Bury(digger.Id, corpse.Id);
            Assert.IsTrue(r.Ok);
            Assert.IsNull(b.CorpseAt(victim));
            Assert.IsTrue(r.Events.Any(e => e.Kind == BattleEventKind.Buried));
            Assert.AreEqual(moon - 1, b.Moon, "holy burial");
            Assert.IsFalse(digger.Acted, "all-night burial: one more action");
            Assert.IsTrue(b.Wait(digger.Id).Ok);
            b.EndTurn();
            Assert.AreEqual(0, b.Stats.Risen, "a buried body never rises");
        }

        [Test]
        public void TreatHealsAWoundedAllyInReachButNotItself()
        {
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 2) }), null, new[] { P("knight", 3, 3), P("doctor", 3, 5) });
            var knight = Ally(b, 3, 3);
            var doctor = Ally(b, 3, 5);
            b.Start();
            b.EndTurn();
            Assert.Less(knight.Hp, knight.MaxHp, "the skeleton hit the knight");
            Assert.IsFalse(b.CanMend(doctor, doctor));
            var want = b.MendAmount(doctor, knight);
            var hp = knight.Hp;
            Assert.IsTrue(b.Mend(doctor.Id, knight.Id).Ok);
            Assert.AreEqual(hp + want, knight.Hp);
            Assert.IsTrue(doctor.Acted);
        }

        [Test]
        public void OneBranchPerTypeAndOldSavesKeepTheFirst()
        {
            var all = Campaign.Stages.Select(s => s.Id);
            var c = Campaign.Parse("{\"gold\":999,\"cleared\":{" + string.Join(",", all.Select(id => "\"" + id + "\":true")) + "},\"bonus\":{},\"ups\":{}}");
            var b = GameData.Branches("sword");
            Assert.IsTrue(c.Buy(b[0].Id));
            Assert.IsFalse(c.Buy(b[1].Id), "the other two branches are locked");
            Assert.AreEqual(b[0], c.Chosen("sword"));
            Assert.IsTrue(c.Buy(GameData.Branches("pspear")[2].Id), "another type is free to choose");

            var old = Campaign.Parse("{\"gold\":5,\"cleared\":{},\"bonus\":{},\"ups\":{\"" + b[2].Id + "\":true,\"" + b[1].Id + "\":true,\"gone_upgrade\":true}}");
            Assert.IsTrue(old.Repaired);
            CollectionAssert.AreEqual(new[] { b[2].Id }, old.Upgrades, "first one kept, unknown id dropped");
            Assert.AreEqual(5 + b[1].Cost, old.Gold, "the dropped branch is refunded");
            Assert.IsFalse(Campaign.Parse(old.ToJson()).Repaired);
        }
    }
}
