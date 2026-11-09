using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Game;

namespace VampireSpawn.Editor
{
    // Fills ArtCatalog.mapProps from CampaignMapLayout.Names. campaign-map.md 2-6: a name in both packs takes PolygonDarkFantasy's.
    public static class CampaignMapProps
    {
        static readonly string[] Folders = { "Assets/Synty/PolygonDarkFantasy/Prefabs", "Assets/Synty/PolygonGeneric/Prefabs" };

        public static string Fill()
        {
            var art = AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/_Project/Art/ArtCatalog.asset");
            var found = new List<GameObject>();
            var missing = new List<string>();
            foreach (var name in CampaignMapLayout.Names)
            {
                var prefab = Folders.Select(f => Find(name, f)).FirstOrDefault(p => p != null);
                if (prefab == null) missing.Add(name); else found.Add(prefab);
            }
            var so = new SerializedObject(art);
            var list = so.FindProperty("mapProps");
            list.arraySize = found.Count;
            for (var i = 0; i < found.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return found.Count + " found" + (missing.Count > 0 ? ", missing: " + string.Join(", ", missing) : "");
        }

        static GameObject Find(string name, string folder) =>
            AssetDatabase.FindAssets(name + " t:Prefab", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => System.IO.Path.GetFileNameWithoutExtension(p) == name)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .FirstOrDefault();
    }
}
