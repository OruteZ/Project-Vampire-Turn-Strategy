using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Editor
{
    // Creates the data assets from the HTML prototype values, and puts single assets back to them.
    public static class DataAssetsBuilder
    {
        public const string Folder = "Assets/_Project/Data";
        public const string RootPath = Folder + "/GameData.asset";

        public static GameDataAsset Root() => AssetDatabase.LoadAssetAtPath<GameDataAsset>(RootPath);

        static T Make<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // Overwrites every value with the prototype's; asset files and GUIDs stay, so scene references survive.
        public static GameDataAsset ResetAll()
        {
            var p = PrototypeData.Create();
            var root = Make<GameDataAsset>(RootPath);
            Undo.RecordObject(root, "데이터 전체 되돌리기");
            root.rules = Make<RulesData>(Folder + "/Rules.asset");
            root.rules.Fill(p.Rules);
            root.skeleton = Make<EnemyData>(Folder + "/Enemies/Skeleton.asset");
            root.skeleton.Fill(p.Skeleton);
            root.units = p.Types.Select(t =>
            {
                var u = Make<UnitTypeData>($"{Folder}/Units/{(int)t.Kind:00}_{t.Key}.asset");
                u.Fill(t, p.Upgrades);
                return u;
            }).ToArray();
            root.stages = p.Stages.Select((s, i) =>
            {
                var a = Make<StageData>($"{Folder}/Stages/{i:00}_{s.Id}.asset");
                a.Fill(s);
                return a;
            }).ToList();
            foreach (var o in new Object[] { root, root.rules, root.skeleton }.Concat(root.units).Concat(root.stages)) EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssets();
            return root;
        }

        // Puts one asset back to the prototype value it was made from; false when the prototype has no such entry.
        public static bool Reset(Object asset)
        {
            var p = PrototypeData.Create();
            Undo.RecordObject(asset, "기본값으로 되돌리기");
            switch (asset)
            {
                case RulesData r: r.Fill(p.Rules); break;
                case EnemyData e: e.Fill(p.Skeleton); break;
                case UnitTypeData u: u.Fill(GameDataOf(p, u.kind), p.Upgrades); break;
                case StageData s:
                    var proto = p.Stages.FirstOrDefault(x => x.Id == s.id);
                    if (proto == null) return false;
                    s.Fill(proto);
                    break;
                default: return false;
            }
            EditorUtility.SetDirty(asset);
            return true;
        }

        static UnitType GameDataOf(DataSet p, UnitKind kind) => p.Types[(int)kind];

        public static StageData DuplicateStage(GameDataAsset root, StageData source)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath($"{Folder}/Stages/{root.stages.Count:00}_{source.id}_copy.asset");
            var copy = Object.Instantiate(source);
            copy.id = source.id + "_copy";
            AssetDatabase.CreateAsset(copy, path);
            Undo.RecordObject(root, "장 복제");
            root.stages.Add(copy);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
            return copy;
        }
    }
}
