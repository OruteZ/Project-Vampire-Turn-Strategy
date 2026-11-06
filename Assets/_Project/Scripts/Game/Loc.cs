using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    public static class Loc
    {
        public const string Table = "Text";

        public static string T(string key, params object[] args)
        {
            for (var i = 0; i < args.Length; i++)
                if (args[i] is Message m) args[i] = Text(m);
            return LocalizationSettings.StringDatabase.GetLocalizedString(Table, key, args, Current);
        }

        // In edit mode nothing selects a locale until someone sets one, so the data editor and tests read the source language.
        public static Locale Current => LocalizationSettings.SelectedLocale ?? LocalizationSettings.ProjectLocale;

        public static string Text(Message m) => m.Key == null ? null : T(m.Key, (object[])m.Args.Clone());
    }

    [Serializable, DisplayName("Korean Josa")]
    public class JosaFormatter : FormatterBase
    {
        public override string[] DefaultNames => new[] { "josa" };

        // {0:josa(이|가)}: the particle after a final consonant first, then the one after a vowel. Non-Hangul endings take the first.
        public override bool TryEvaluateFormat(IFormattingInfo info)
        {
            var pair = info.FormatterOptions.Split('|');
            if (pair.Length != 2) return false;
            var word = info.CurrentValue?.ToString() ?? "";
            info.Write(word + Pick(word, pair[0], pair[1]));
            return true;
        }

        public static string Pick(string word, string withFinal, string withoutFinal)
        {
            if (word.Length == 0) return withoutFinal;
            var ch = word[word.Length - 1];
            if (ch < 0xAC00 || ch > 0xD7A3) return withFinal;
            return (ch - 0xAC00) % 28 != 0 ? withFinal : withoutFinal;
        }
    }
}
