using UnityEditor;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;
using Object = UnityEngine.Object;

namespace VampireSpawn.Editor
{
    [InitializeOnLoad]
    public static class DataAssetsBuilder
    {
        public const string Folder = "Assets/_Project/Data";
        public const string RootPath = Folder + "/GameData.asset";

        // Edit-mode tests and editor windows read GameData before any scene's App has run.
        static DataAssetsBuilder() => GameData.Loader = () => Root()?.ToDataSet();

        public static GameDataAsset Root() => AssetDatabase.LoadAssetAtPath<GameDataAsset>(RootPath);

        // A new unit type, minion or stage starts as a copy of a similar one, appended to the root list.
        public static Object Duplicate(GameDataAsset root, Object source)
        {
            Undo.RecordObject(root, "복제");
            Object copy;
            switch (source)
            {
                case StageData s:
                    var st = Object.Instantiate(s);
                    st.id = s.id + "_copy";
                    Create(st, $"Stages/{root.stages.Count:00}_{st.id}");
                    root.stages.Add(st);
                    copy = st;
                    break;
                case UnitTypeData u:
                    var ut = Object.Instantiate(u);
                    ut.key = u.key + "_copy";
                    foreach (var up in ut.upgrades) up.id = ut.key + (up.id.StartsWith(u.key) ? up.id.Substring(u.key.Length) : "_" + up.id);
                    Create(ut, $"Units/{root.units.Count:00}_{ut.key}");
                    root.units.Add(ut);
                    copy = ut;
                    break;
                case EnemyData e:
                    var et = Object.Instantiate(e);
                    et.key = e.key + "Copy";
                    Create(et, "Enemies/" + et.key);
                    root.minions.Add(et);
                    copy = et;
                    break;
                default: return null;
            }
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
            return copy;
        }

        static void Create(Object asset, string name) =>
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath($"{Folder}/{name}.asset"));
    }
}
