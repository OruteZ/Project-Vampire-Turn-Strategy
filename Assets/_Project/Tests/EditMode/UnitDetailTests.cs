using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game;
using VampireSpawn.Game.Screens;

namespace VampireSpawn.Tests
{
    // docs/ui/unit-detail.md: what the detail window says about a unit.
    public class UnitDetailTests
    {
        static readonly string[] Open = Enumerable.Repeat(".......", 7).ToArray();

        static void Reads(string text, string of) =>
            Assert.IsFalse(string.IsNullOrEmpty(text) || text.Contains("{") || text.Contains("No translation"), of + ": " + text);

        [Test]
        public void EveryUnitTypeReadsInFullWithAndWithoutItsUpgrades()
        {
            foreach (var t in GameData.Types)
                foreach (var owned in new[] { Enumerable.Empty<string>(), GameData.Branches(t.Key).Select(u => u.Id) })
                {
                    var m = DetailModel.OfType(t.Key, owned);
                    var texts = new[] { m.Name, m.RoleText, m.SkillsTitle }.Concat(m.Stats.Select(s => s.Label))
                        .Concat(m.Skills.SelectMany(s => new[] { s.Name, s.Desc })).Concat(m.Upgrades.SelectMany(u => new[] { u.Name, u.Effect }));
                    foreach (var text in texts) Reads(text, t.Key);
                    Assert.AreEqual(GameData.Branches(t.Key).Length, m.Upgrades.Count, t.Key);
                    Assert.IsEmpty(m.States, "a unit type has no state");
                    var u = Battle.Preview(t.Key, owned);
                    CollectionAssert.AreEqual(new[] { u.MaxHp, u.Atk, 0, u.Mov, 1 }, m.Spawn, "spawn.md 9: hp, atk and mov carry over, def 0, melee");
                }
        }

        [Test]
        public void AnUpgradedStatIsMarkedAndItsTipNamesTheBaseAndTheSource()
        {
            var up = GameData.Branches("knight").First(u => u.Mod.Hp > 0 && u.Mod.Def == 0);
            var plain = DetailModel.OfType("knight", null).Stats[0];
            var better = DetailModel.OfType("knight", new[] { up.Id }).Stats[0];
            Assert.IsFalse(plain.Changed);
            Assert.IsNull(plain.Tip);
            Assert.IsTrue(better.Changed);
            Assert.AreEqual((GameData.Type("knight").Hp + up.Mod.Hp).ToString(), better.Value);
            StringAssert.Contains(Loc.T("detail.base", GameData.Type("knight").Hp), better.Tip);
            StringAssert.Contains(Loc.Text(up.Name) + " +" + up.Mod.Hp, better.Tip);
        }

        [Test]
        public void AUnitOnTheBoardShowsItsStatesAsTermsAndAnEnemyItsTraits()
        {
            var stage = new Stage("demo", GameData.Types.Select(t => t.Key).ToArray(), 99, 99, 0, Goal.Boss,
                new Bonus(BonusType.NoEat, 0, 0), new Boss(30, 1, 0, 0, 9), Open,
                new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement("skeleton", 5, 5) }, new Placement[0]);
            var b = new Battle(stage, null, new[] { new Placement("scout", 1, 1), new Placement("archer", 3, 1) });
            b.Start();

            var scout = DetailModel.Of(b, b.Allies.First(u => u.Type == "scout"), null);
            var traps = scout.States.Single(s => s.Id == "traps");
            Assert.AreEqual(Loc.T("state.traps", Battle.TrapsHeld, Battle.TrapsHeld), traps.Name);
            Reads(traps.Desc, "traps");
            StringAssert.Contains(" / ", scout.Stats[0].Value, "a unit on the board shows hp now / max");

            var archer = DetailModel.Of(b, b.Allies.First(u => u.Type == "archer"), null);
            Assert.AreEqual(Loc.T("detail.trait.ranged"), archer.Skills.Last().Name);

            var vampire = DetailModel.Of(b, b.Vampire, null);
            Assert.IsTrue(vampire.Enemy);
            Assert.IsNull(vampire.Spawn);
            Assert.IsNull(vampire.Upgrades);
            Assert.AreEqual("coffin", vampire.States.Single().Id);
            Assert.AreEqual(3, vampire.Skills.Count, "coffin, feeding, turning");
            foreach (var s in vampire.Skills) Reads(s.Desc, s.Name);

            var minion = DetailModel.Of(b, b.Enemies.First(u => u.Role == Role.Minion), null);
            Assert.AreEqual(Loc.T("detail.trait.minion"), minion.Skills.Single().Name);
        }

        [Test]
        public void AGuardInAPackSaysThePackWakesTogether()
        {
            var stage = new Stage("demo", new[] { "knight" }, 99, 99, 0, Goal.Boss,
                new Bonus(BonusType.NoEat, 0, 0), new Boss(30, 1, 0, 0, 9), Open,
                new[] { new Placement(EnemyType.Vampire, 6, 6), new Placement("skeleton", 5, 5, stance: Stance.Guard, alert: 2, group: "a"),
                    new Placement("skeleton", 4, 5, stance: Stance.Guard, alert: 2) }, new Placement[0]);
            var b = new Battle(stage, null, new[] { new Placement("knight", 1, 1) });
            b.Start();
            var guards = b.Enemies.Where(u => u.Stance == Stance.Guard).ToArray();
            Assert.AreEqual(Loc.T("unit.card.stance.guardGroup", 2), DetailModel.Of(b, guards.Single(u => u.Group != null), null).States.Single(s => s.Id == "guard").Desc);
            Assert.AreEqual(Loc.T("unit.card.stance.guard", 2), DetailModel.Of(b, guards.Single(u => u.Group == null), null).States.Single(s => s.Id == "guard").Desc);
        }
    }
}
