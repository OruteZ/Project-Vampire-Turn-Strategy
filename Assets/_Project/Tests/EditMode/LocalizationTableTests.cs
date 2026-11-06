using System.Linq;
using NUnit.Framework;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    public class LocalizationTableTests
    {
        [Test]
        public void EveryKeyHasTextInEveryLanguage()
        {
            var missing = (from locale in LocalizationSettings.AvailableLocales.Locales
                           let table = LocalizationSettings.StringDatabase.GetTable(Loc.Table, locale)
                           from entry in table.SharedData.Entries
                           let e = table.GetEntry(entry.Id)
                           where e == null || string.IsNullOrWhiteSpace(e.Value)
                           select locale.Identifier.Code + ":" + entry.Key).ToArray();
            Assert.That(missing, Is.Empty, "empty cells: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryLocaleNamesItselfAndListsFonts()
        {
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                Assert.That(locale.LocaleName, Is.Not.Empty.And.Not.EqualTo(locale.Identifier.Code));
                var table = (StringTable)LocalizationSettings.StringDatabase.GetTable(Loc.Table, locale);
                Assert.That(table.GetEntry("font.body")?.Value, Is.Not.Empty, locale.Identifier.Code + " font.body");
                Assert.That(table.GetEntry("font.display")?.Value, Is.Not.Empty, locale.Identifier.Code + " font.display");
            }
        }
    }
}
