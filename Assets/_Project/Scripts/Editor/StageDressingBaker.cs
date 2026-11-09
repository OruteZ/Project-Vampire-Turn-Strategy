using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Editor
{
    // docs/art/stage-dressing.md 4-5: builds a dressing prefab only for stages that have none, so hand edits survive.
    public static class StageDressingBaker
    {
        public const string Folder = "Assets/_Project/Art/Stages", CatalogPath = "Assets/_Project/Art/ArtCatalog.asset";

        public static string Bake()
        {
            var art = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath);
            Directory.CreateDirectory(Folder);
            var all = new List<GameObject>();
            var made = new List<string>();
            foreach (var stage in GameData.Stages)
            {
                var path = Folder + "/" + stage.Id + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    var root = BoardView.BuildDressing(art, stage, Bed(art, art.Theme(stage.Id)));
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                    Object.DestroyImmediate(root);
                    made.Add(stage.Id);
                }
                all.Add(prefab);
            }
            art.stageDressings = all.ToArray();
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return "꾸미기 프리팹: 새로 " + made.Count + " (" + string.Join(", ", made) + "), 전체 " + all.Count;
        }

        // The bed sits a shade darker than the tiles so the hex edges still read (stage-dressing.md 1-1).
        static Material Bed(ArtCatalog art, BoardTheme theme)
        {
            var name = "Bed " + (theme != null ? theme.name : "Plain");
            var path = Folder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(art.plain) { color = (theme != null ? theme.ground : art.plain.color) * 0.7f, name = name };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
