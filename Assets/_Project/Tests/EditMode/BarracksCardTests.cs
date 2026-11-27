using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game;
using VampireSpawn.Game.Screens;

namespace VampireSpawn.Tests
{
    // docs/ui/upgrade.md 5-2: a stat upgrade's card shows its numbers before and after.
    public class BarracksCardTests
    {
        [Test]
        public void AStatUpgradeReadsBeforeAndAfterAndASkillKeepsItsSentence()
        {
            var hp = GameData.Branches("knight").First(u => u.Ability == Ability.None && u.Mod.Hp > 0 && u.Mod.Def == 0);
            var max = Battle.Preview("knight", new string[0]).MaxHp;
            Assert.AreEqual(Loc.T("unit.card.hp") + " " + max + " → " + (max + hp.Mod.Hp), CampToolkit.Effect("knight", hp, new string[0]));
            Assert.AreEqual(CampToolkit.Effect("knight", hp, new string[0]), CampToolkit.Effect("knight", hp, new[] { hp.Id }), "an owned upgrade still shows what it added");
            var skill = GameData.Upgrades.First(u => u.Ability != Ability.None);
            Assert.IsNull(CampToolkit.Effect(skill.Unit, skill, new string[0]));
        }
    }
}
