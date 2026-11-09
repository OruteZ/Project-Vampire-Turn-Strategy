using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;
using Terrain = VampireSpawn.Core.Terrain;

namespace VampireSpawn.Editor
{
    // docs/art/stage-dressing.md 4-5, battle-terrain.md 5: builds only what a stage's dressing prefab lacks, so hand edits survive.
    public static class StageDressingBaker
    {
        public const string Folder = "Assets/_Project/Art/Stages", CatalogPath = "Assets/_Project/Art/ArtCatalog.asset";

        // battle-terrain.md 2.
        const float Width = 160f, Depth = 140f, HeightRange = 8f, Below = 2f;
        const int Resolution = 257, Splat = 256;

        public static string Bake()
        {
            var art = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath);
            Directory.CreateDirectory(Folder);
            var all = new List<GameObject>();
            var made = new List<string>();
            var grounded = new List<string>();
            foreach (var stage in GameData.Stages)
            {
                var path = Folder + "/" + stage.Id + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    var root = BoardView.BuildDressing(art, stage);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Object.DestroyImmediate(root);
                    made.Add(stage.Id);
                }
                var contents = PrefabUtility.LoadPrefabContents(path);
                if (contents.GetComponentInChildren<UnityEngine.Terrain>() == null)
                {
                    AddTerrain(art, stage, contents);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    grounded.Add(stage.Id);
                }
                PrefabUtility.UnloadPrefabContents(contents);
                all.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            }
            art.stageDressings = all.ToArray();
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return "꾸미기 프리팹: 새로 " + made.Count + " (" + string.Join(", ", made) + "), 지형 더함 " + grounded.Count + " (" + string.Join(", ", grounded) + "), 전체 " + all.Count;
        }

        static void AddTerrain(ArtCatalog art, Stage stage, GameObject root)
        {
            var old = root.transform.Find("Ground");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var board = new Board(stage.Map);
            var theme = art.Theme(stage.Id);
            var s = art.hexSize;
            var first = BoardView.Local(new Hex(0, 0), s);
            var last = BoardView.Local(new Hex(board.Cols - 1, board.Rows - 1), s);
            float xMin = first.x - s, xMax = last.x + s, zMax = first.z + s, zMin = last.z - s * 1.5f;
            var origin = new Vector3((xMin + xMax) * 0.5f - Width * 0.5f, -Below, (zMin + zMax) * 0.5f - Depth * 0.5f);
            var seed = stage.Id.Aggregate(17, (k, ch) => unchecked(k * 31 + ch)) & 0xffff;
            float ox = seed % 251 * 1.37f, oz = seed / 251 * 0.91f;
            var water = new List<Vector2>();
            for (var r = 0; r < board.Rows; r++)
                for (var c = 0; c < board.Cols; c++)
                    if (board.At(new Hex(c, r)) == Terrain.Water) { var w = BoardView.Local(new Hex(c, r), s); water.Add(new Vector2(w.x, w.z)); }

            var heights = new float[Resolution, Resolution];
            for (var i = 0; i < Resolution; i++)
                for (var j = 0; j < Resolution; j++)
                {
                    float x = origin.x + j * Width / (Resolution - 1), z = origin.z + i * Depth / (Resolution - 1);
                    var y = 0.12f * (2f * Mathf.PerlinNoise(x / 6f + ox, z / 6f + oz) - 1f);
                    float side = Mathf.Max(xMin - x, 0f, x - xMax), north = Mathf.Max(z - zMax, 0f), south = Mathf.Max(zMin - z, 0f);
                    var away = Mathf.Sqrt(side * side + (north + south) * (north + south));
                    if (away > 0f)
                    {
                        var amp = north > 0f ? Mathf.Lerp(1.1f, 1.4f, north / (north + side)) : south > 0f ? Mathf.Lerp(1.1f, 0.3f, south / (south + side)) : 1.1f;
                        var hills = 0.7f * Mathf.PerlinNoise(x / 14f + oz, z / 14f + ox) + 0.3f * Mathf.PerlinNoise(x / 6f + ox + 40f, z / 6f + oz);
                        y += Mathf.Clamp01(away / 12f) * amp * hills;
                    }
                    foreach (var w in water)
                    {
                        var d = Vector2.Distance(w, new Vector2(x, z)) / (0.8f * s);
                        if (d < 1f) y = Mathf.Min(y, Mathf.Lerp(-0.5f, y, Mathf.SmoothStep(0f, 1f, d)));
                    }
                    heights[i, j] = (y + Below) / HeightRange;
                }

            // Resolution before size: setting the resolution afterwards resets the size.
            var data = new TerrainData { heightmapResolution = Resolution };
            data.size = new Vector3(Width, HeightRange, Depth);
            data.SetHeights(0, 0, heights);
            if (theme != null && theme.floor != null)
            {
                var layers = theme.patches != null ? new[] { theme.floor, theme.patches } : new[] { theme.floor };
                data.alphamapResolution = Splat;
                data.terrainLayers = layers;
                var alpha = new float[Splat, Splat, layers.Length];
                for (var i = 0; i < Splat; i++)
                    for (var j = 0; j < Splat; j++)
                    {
                        float x = origin.x + j * Width / (Splat - 1), z = origin.z + i * Depth / (Splat - 1);
                        var patch = layers.Length > 1 ? Mathf.SmoothStep(0f, 1f, (Mathf.PerlinNoise(x / 5f + ox + 50f, z / 5f + oz + 50f) - (1f - theme.patchShare)) / 0.08f + 0.5f) : 0f;
                        alpha[i, j, 0] = 1f - patch;
                        if (layers.Length > 1) alpha[i, j, 1] = patch;
                    }
                data.SetAlphamaps(0, 0, alpha);
            }
            var dataPath = Folder + "/" + stage.Id + " Terrain.asset";
            AssetDatabase.DeleteAsset(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);

            var go = UnityEngine.Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = origin;
            go.transform.SetSiblingIndex(0);
            var terrain = go.GetComponent<UnityEngine.Terrain>();
            // The default LOD (pixel error 5) flattens the 0.5-deep water pits and buries their surface.
            terrain.heightmapPixelError = 1f;
            var props = root.transform.Find("Props");
            if (props == null) return;
            foreach (Transform p in props)
            {
                var at = p.position;
                at.y = terrain.SampleHeight(at) + terrain.transform.position.y - 0.05f;
                p.position = at;
            }
        }
    }
}
