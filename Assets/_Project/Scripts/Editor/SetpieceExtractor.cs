using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VampireSpawn.Editor
{
    // docs/art/stage-dressing.md 5-1: turns clusters of props in a Synty demo scene into setpiece prefabs, never overwriting one that exists.
    public static class SetpieceExtractor
    {
        public const string Folder = "Assets/_Project/Art/Setpieces";
        // Core pieces make a group; rocks and plants only cling to the core next to them, or they chain the whole scene into one lump.
        static readonly string[] Core = { "Tombs", "Statues", "Cover", "Storage", "Miscellaneous" }, Cling = { "Rocks", "Foliage" };
        // Pieces that only make sense against the demo's own walls and cliffs (vines, moss, grime decals), and cliff-sized rocks.
        static readonly string[] Skip = { "Cliff", "Vine", "Moss", "Grunge" };
        const float Link = 2f, ClingReach = 1.5f, MaxSpan = 9f, Scale = 0.85f, MaxPieceSize = 6f;
        const int MinCount = 3, MaxCount = 8, MaxCore = 5;

        public static string Extract(string scenePath, string theme)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var items = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Where(t => t.parent != null && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && t.GetComponentInChildren<Renderer>() != null)
                    .Where(t => !Skip.Any(t.name.Contains) && Size(t) <= MaxPieceSize)
                    .ToList();
                var core = items.Where(t => Core.Contains(t.parent.name)).ToList();
                var cling = items.Where(t => Cling.Contains(t.parent.name)).ToList();
                var folder = Folder + "/" + theme;
                Directory.CreateDirectory(folder);
                var made = 0;
                var clusters = Clusters(core).SelectMany(Split)
                    .Select(c => c.Concat(cling.Where(r => c.Any(t => Flat(t.position - r.position) <= ClingReach)).OrderBy(r => c.Min(t => Flat(t.position - r.position))).Take(MaxCount - c.Count)).ToList())
                    .Where(c => c.Count >= MinCount && Span(c) <= MaxSpan)
                    .OrderBy(c => Mathf.Round(c.Average(t => t.position.x))).ThenBy(c => c.Average(t => t.position.z)).ToList();
                for (var i = 0; i < clusters.Count; i++)
                {
                    var path = $"{folder}/{theme}_{i + 1:00}.prefab";
                    if (File.Exists(path)) continue;
                    var c = clusters[i];
                    var pivot = new Vector3(c.Average(t => t.position.x), c.Min(t => t.position.y), c.Average(t => t.position.z));
                    var root = new GameObject(theme + "_" + (i + 1).ToString("00"));
                    foreach (var t in c)
                    {
                        var source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                        var copy = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                        copy.transform.localPosition = (t.position - pivot) * Scale;
                        copy.transform.localRotation = t.rotation;
                        copy.transform.localScale = t.lossyScale * Scale;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Object.DestroyImmediate(root);
                    made++;
                }
                return $"세트피스 {theme}: 중심 소품 {core.Count}, 곁 소품 {cling.Count}, 무리 {clusters.Count}, 새로 {made}";
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // Single-link clusters on the ground plane.
        static List<List<Transform>> Clusters(List<Transform> items)
        {
            var parent = Enumerable.Range(0, items.Count).ToArray();
            int Find(int i) => parent[i] == i ? i : parent[i] = Find(parent[i]);
            for (var i = 0; i < items.Count; i++)
                for (var j = i + 1; j < items.Count; j++)
                {
                    var d = items[i].position - items[j].position;
                    if (d.x * d.x + d.z * d.z <= Link * Link) parent[Find(i)] = Find(j);
                }
            return Enumerable.Range(0, items.Count).GroupBy(Find).Select(g => g.Select(i => items[i]).ToList()).ToList();
        }

        static float Size(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return Mathf.Max(b.size.x, b.size.y, b.size.z);
        }

        static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;

        static float Span(List<Transform> c) => c.SelectMany(a => c, (a, b) => Flat(a.position - b.position)).Max();

        // A long row of core pieces (a cemetery aisle) splits into chunks of at most MaxCore along its longer axis.
        static IEnumerable<List<Transform>> Split(List<Transform> c)
        {
            if (c.Count <= MaxCore) { yield return c; yield break; }
            var alongX = c.Max(t => t.position.x) - c.Min(t => t.position.x) >= c.Max(t => t.position.z) - c.Min(t => t.position.z);
            var sorted = c.OrderBy(t => alongX ? t.position.x : t.position.z).ToList();
            var parts = Mathf.CeilToInt(sorted.Count / (float)MaxCore);
            for (var i = 0; i < parts; i++) yield return sorted.Skip(i * sorted.Count / parts).Take((i + 1) * sorted.Count / parts - i * sorted.Count / parts).ToList();
        }
    }
}
