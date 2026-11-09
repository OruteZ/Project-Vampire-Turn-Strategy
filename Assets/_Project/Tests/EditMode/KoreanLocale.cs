using NUnit.Framework;
using UnityEngine.Localization.Settings;

namespace VampireSpawn.Tests
{
    // Expected texts in the tests are Korean, so every EditMode test renders text with the Korean table.
    [SetUpFixture]
    public class KoreanLocale
    {
        [OneTimeSetUp]
        public void Select() => LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ko");
    }
}
