using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using VampireSpawn.Game;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Editor
{
    // Data text lives in the Korean column of the Text table; designers edit it here instead of on the asset.
    public static class SourceText
    {
        static StringTableCollection Collection => LocalizationEditorSettings.GetStringTableCollection(Loc.Table);
        static StringTable Ko => (StringTable)Collection.GetTable("ko");

        public static string Get(string key) => Ko.GetEntry(key)?.Value ?? "";

        public static IEnumerable<(string label, string key, bool area)> Keys(Object o)
        {
            switch (o)
            {
                case UnitTypeData u:
                    yield return ("이름", "unit." + u.key + ".name", false);
                    yield return ("글자", "unit." + u.key + ".glyph", false);
                    yield return ("설명", "unit." + u.key + ".desc", true);
                    foreach (var up in u.upgrades)
                    {
                        yield return ("강화 " + up.id + " 이름", "upgrade." + up.id + ".name", false);
                        yield return ("강화 " + up.id + " 설명", "upgrade." + up.id + ".desc", false);
                    }
                    break;
                case EnemyData _:
                    yield return ("이름", "skeleton.name", false);
                    yield return ("글자", "skeleton.glyph", false);
                    break;
                case StageData s:
                    yield return ("번호", "stage." + s.id + ".label", false);
                    yield return ("제목", "stage." + s.id + ".title", false);
                    yield return ("소개", "stage." + s.id + ".brief", true);
                    yield return ("보스 이름", "stage." + s.id + ".boss", false);
                    break;
            }
        }

        public static void Section(Object o)
        {
            var keys = new List<(string label, string key, bool area)>(Keys(o));
            if (keys.Count == 0) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("문장 (한국어 표, 다른 언어는 Localization Tables 창)", EditorStyles.boldLabel);
            foreach (var (label, key, area) in keys)
            {
                var old = Get(key);
                var now = area
                    ? EditorGUILayout.TextField(label, old, EditorStyles.textArea, GUILayout.MinHeight(48))
                    : EditorGUILayout.TextField(label, old);
                if (now != old) Set(key, now);
            }
        }

        public static void Set(string key, string value)
        {
            var ko = Ko;
            Undo.RecordObjects(new Object[] { ko, Collection.SharedData }, "문장 고치기");
            ko.AddEntry(key, value).IsSmart = true;
            EditorUtility.SetDirty(ko);
            EditorUtility.SetDirty(Collection.SharedData);
        }
    }
}
