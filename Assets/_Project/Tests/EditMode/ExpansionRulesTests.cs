using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // #99 rules: docs/content/units.md 4, docs/systems/ai.md 2 and 5, docs/systems/progression.md 5-17.
    public class ExpansionRulesTests
    {
        [TearDown] public void BackToShipped() => GameData.Use(null);

        static readonly string[] Open = Enumerable.Repeat(".......", 7).ToArray();

        static Stage Field(Placement[] enemies, int bossAtk = 1) => new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Boss,
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

        [Test]
        public void BossGoalIsWonOnceNoVampireIsLeft()
        {
            var b = new Battle(Field(new[] { P("skeleton", 3, 2) }), null, new[] { P("knight", 3, 3) });
            var knight = Ally(b, 3, 3);
            var skeleton = b.Enemies.Single();
            b.Start();
            for (var i = 0; i < 20 && b.Phase != Phase.Over; i++)
            {
                b.Attack(knight.Id, skeleton.Id);
                if (b.Phase != Phase.Over) b.EndTurn();
            }
            Assert.AreEqual(true, b.Won, "a stage whose only enemy is a minion is won when it falls");
        }

        [Test]
        public void BossGoalIsLostOnceNoAllyIsLeft()
        {
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 2) }), null, new[] { P("pspear", 3, 3) });
            b.Start();
            for (var i = 0; i < 20 && b.Phase != Phase.Over; i++) b.EndTurn();
            Assert.AreEqual(false, b.Won, "the skeleton kills the lone spearman");
            Assert.IsNotNull(b.Vampire);
        }

        static Stage GoalStage(Goal g, Placement[] enemies, Hex[] spots, int hold = 0, EscortStats escort = null)
        {
            var s = Field(enemies);
            return new Stage(s.Id, s.Units, s.Pop, s.Silver, s.Reward, g, s.Bonus, s.Boss, s.Map, s.Enemies, s.Sample, spots, hold, escort);
        }

        [Test]
        public void DefenceIsWonByHoldingAndLostOnceAnEnemyStandsOnTheSpot()
        {
            Rules(moonMax: 9, eatRange: 2, moonWake: 9);
            var held = new Battle(GoalStage(Goal.Defense, new[] { P(EnemyType.Vampire, 6, 6) }, new[] { At(0, 0) }, hold: 2), null, new[] { P("knight", 3, 3) });
            held.Start();
            held.EndTurn();
            Assert.IsNull(held.Won, "one turn held out of two");
            held.EndTurn();
            Assert.AreEqual(Ending.Held, held.Ending);
            Assert.AreEqual(true, held.Won);

            var lost = new Battle(GoalStage(Goal.Defense, new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 2) }, new[] { At(3, 3) }, hold: 6),
                null, new[] { P("knight", 0, 6) });
            lost.Start();
            lost.EndTurn();
            Assert.AreEqual(At(3, 3), lost.Enemies.First(e => e.Kind == "skeleton").Pos, "the skeleton takes the spot before chasing the knight");
            Assert.AreEqual(Ending.Overrun, lost.Ending);
            Assert.AreEqual(false, lost.Won);
        }

        [Test]
        public void EscortWinsOnArrivalAndIsNeitherPlacedNorExecuted()
        {
            var b = new Battle(GoalStage(Goal.Escort, new[] { P(EnemyType.Vampire, 6, 6) }, new[] { At(3, 0) }, escort: new EscortStats(6, 0, 3, At(3, 3))),
                null, new[] { P("knight", 3, 4), P("pspear", 3, 3) });
            var escort = b.Escort;
            Assert.AreEqual(At(3, 3), escort.Pos);
            Assert.AreEqual(1, b.Allies.Count(), "the spearman on the escort's hex is dropped");
            Assert.IsFalse(b.Recall(escort.Id).Ok);
            b.Start();
            Assert.IsFalse(b.Execute(b.Allies.First().Id, escort.Id).Ok);
            Assert.IsTrue(b.Move(escort.Id, At(3, 0)).Ok);
            Assert.AreEqual(Ending.Arrived, b.Ending);
        }

        [Test]
        public void EscortFallingLosesAndAloneItDoesNotFight()
        {
            var fell = new Battle(GoalStage(Goal.Escort, new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 2) }, new[] { At(0, 0) }, escort: new EscortStats(1, 0, 3, At(3, 3))),
                null, new[] { P("knight", 0, 6) });
            fell.Start();
            fell.EndTurn();
            Assert.AreEqual(Ending.EscortFell, fell.Ending, "an escort that can be killed is the first target");
            Assert.IsEmpty(fell.Corpses);

            var alone = new Battle(GoalStage(Goal.Escort, new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 0, 5) }, new[] { At(0, 0) }, escort: new EscortStats(9, 0, 3, At(3, 0))),
                null, new[] { P("pspear", 0, 6) });
            alone.Start();
            for (var i = 0; i < 20 && alone.Phase != Phase.Over; i++) alone.EndTurn();
            Assert.AreEqual(Ending.Wiped, alone.Ending);
            Assert.IsNotNull(alone.Escort);
        }

        // docs/content/units.md "판타지 직업 개편" B.
        [Test]
        public void AlchemistFlaskSplashesTheOtherEnemiesNextToTheTarget()
        {
            var (near, beyond) = Line(At(3, 1));
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 1), P("skeleton", near.Col, near.Row), P("skeleton", beyond.Col, beyond.Row) }),
                null, new[] { P("hunter", 3, 3) });
            b.Start();
            Unit Foe(Hex h) => b.UnitAt(h);
            var hp = Foe(near).Hp;
            var far = Foe(beyond).Hp;
            CollectionAssert.AreEquivalent(new[] { Foe(near) }, b.Splashed(Ally(b, 3, 3), Foe(At(3, 1))).ToList());
            Assert.IsTrue(b.Attack(Ally(b, 3, 3).Id, Foe(At(3, 1)).Id).Ok);
            Assert.AreEqual(hp - 1, Foe(near).Hp, "next to the target: 1, armour ignored");
            Assert.AreEqual(far, Foe(beyond).Hp, "two away: untouched");
            Assert.IsTrue(b.Log.Any(l => Loc.Text(l.Text).Contains(Loc.T("tag.splash"))));
        }

        // docs/content/units.md "판타지 직업 개편" C.
        [Test]
        public void WerewolfTurnsWithTheMoonAndLeavesAHumanCorpse()
        {
            var type = GameData.Type("werewolf");
            var b = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 2) }), new[] { "werewolf_feast" }, new[] { P("werewolf", 3, 3) });
            b.Start();
            var wolf = Ally(b, 3, 3);
            b.EndTurn(); b.EndTurn();
            Assert.AreEqual(2, b.Moon);
            Assert.IsFalse(wolf.Wolf, "moon 2: still human");
            var events = b.EndTurn().Events;
            Assert.IsTrue(wolf.Wolf, "moon 3");
            Assert.AreEqual(type.Atk + 2, wolf.Atk);
            Assert.AreEqual(type.Mov + 1, wolf.Mov);
            Assert.IsTrue(events.Any(e => e.Kind == BattleEventKind.Shifted && e.UnitId == wolf.Id && e.Value == 1));
            var hurt = wolf.Hp;
            Assert.Less(hurt, wolf.MaxHp, "the skeleton next to it has hit it");
            Assert.IsTrue(b.Attack(wolf.Id, b.UnitAt(At(3, 2)).Id).Ok);
            Assert.AreEqual(hurt + 2, wolf.Hp, "feast: 2 back on the kill");

            var fall = new Battle(Field(new[] { P(EnemyType.Vampire, 3, 2) }, 99), null, new[] { P("werewolf", 3, 3), P("knight", 0, 6) });
            fall.Start();
            for (var i = 0; i < 4 && fall.Corpses.Count == 0; i++) fall.EndTurn();
            Assert.AreEqual(type.Atk, fall.Corpses.Single(c => c.Type == "werewolf").Atk, "the corpse keeps the human attack");

            var crescent = new Battle(Field(new[] { P(EnemyType.Vampire, 6, 6) }), new[] { "werewolf_early" }, new[] { P("werewolf", 3, 3) });
            crescent.Start();
            crescent.EndTurn(); crescent.EndTurn();
            Assert.IsTrue(Ally(crescent, 3, 3).Wolf, "crescent call: from moon 2");
        }

        [Test]
        public void EveryGoalHasAnObjectiveAndIsJudged()
        {
            foreach (Goal g in System.Enum.GetValues(typeof(Goal)))
            {
                var s = Field(new[] { P(EnemyType.Vampire, 6, 6), P("skeleton", 3, 2) });
                s = new Stage(s.Id, s.Units, s.Pop, s.Silver, s.Reward, g, s.Bonus, s.Boss, s.Map, s.Enemies, s.Sample);
                Assert.IsNotEmpty(Loc.Text(s.Objective), g.ToString());
                var b = new Battle(s, null, new[] { P("knight", 3, 3) });
                b.Start();
                Assert.DoesNotThrow(() => b.Move(b.Allies.First().Id, At(3, 4)), g + " judged after a move");
                Assert.DoesNotThrow(() => b.EndTurn(), g + " judged at the end of a turn");
            }
        }
    }
}
