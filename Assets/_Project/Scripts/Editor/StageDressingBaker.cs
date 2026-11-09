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
        const int Splat = 256;
        // battle-terrain.md 2-2: the river cells sit a step below the land: bed, a gentle shelf the waterline crosses, then a steep wall.
        // The waterline is where the flat water cuts the terrain mesh; crossing a slope one grid cell wide it zigzags along the triangles.
        const float RiverBed = -0.6f, ShelfTop = -0.4f, Shelf = 0.5f, Wall = 0.15f, WaterLevel = -0.45f;

        // Height across the bank by distance past the wall's foot (in world units), before blending into the land height y.
        static float BankProfile(float bank, float s, float y) =>
            bank < -Shelf * s ? RiverBed
            : bank < 0f ? Mathf.Lerp(RiverBed, ShelfTop, (bank + Shelf * s) / (Shelf * s))
            : Mathf.Lerp(ShelfTop, y, Mathf.SmoothStep(0f, 1f, bank / (Wall * s)));

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
            var river = River(board, s, ox, oz);
            // Distance past the river bank; negative inside the channel (battle-terrain.md 2-2).
            float Bank(float x, float z)
            {
                var best = float.MaxValue;
                foreach (var (a, b, wa, wb) in river)
                {
                    var p = new Vector2(x, z);
                    var ab = b - a;
                    var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    var w = Mathf.Lerp(wa, wb, t) * (0.8f + 0.08f * Mathf.PerlinNoise(x / 4f + ox, z / 4f + oz)) * s;
                    best = Mathf.Min(best, Vector2.Distance(p, a + ab * t) - w);
                }
                return best;
            }

            // A finer grid where there is a shoreline, or the bank shows 0.6-wide stair steps.
            var Resolution = river.Count > 0 ? 1025 : 257;
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
                    if (river.Count > 0)
                    {
                        var bank = Bank(x, z);
                        if (bank < Wall * s) y = Mathf.Min(y, BankProfile(bank, s, y));
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
                        if (river.Count > 0 && layers.Length > 1 && Bank(x, z) < 0.25f * s) patch = 1f;
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
            // The default LOD (pixel error 5) flattens the 0.5-deep channel and buries the water.
            terrain.heightmapPixelError = 1f;
            if (river.Count > 0)
            {
                var water = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(water.GetComponent<Collider>());
                water.name = "Water";
                water.transform.SetParent(root.transform, false);
                water.transform.localPosition = origin + new Vector3(Width * 0.5f, Below + WaterLevel, Depth * 0.5f);
                water.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                water.transform.localScale = new Vector3(Width, Depth, 1f);
                water.GetComponent<MeshRenderer>().sharedMaterial = art.water;
            }
            var props = root.transform.Find("Props");
            if (props == null) return;
            foreach (var p in props.Cast<Transform>().ToList())
            {
                var at = p.position;
                if (river.Count > 0 && Bank(at.x - root.transform.position.x, at.z - root.transform.position.z) < 0.3f * s) { Object.DestroyImmediate(p.gameObject); continue; }
                at.y = terrain.SampleHeight(at) + terrain.transform.position.y - 0.05f;
                p.position = at;
            }
        }

        // battle-terrain.md 2-2: segments joining touching water cells, plus a meandering run off the board from each edge water cell. Widths are in cells.
        static List<(Vector2 a, Vector2 b, float wa, float wb)> River(Board board, float s, float ox, float oz)
        {
            var cells = new List<(Hex h, Vector2 p)>();
            for (var r = 0; r < board.Rows; r++)
                for (var c = 0; c < board.Cols; c++)
                    if (board.At(new Hex(c, r)) == Terrain.Water) { var w = BoardView.Local(new Hex(c, r), s); cells.Add((new Hex(c, r), new Vector2(w.x, w.z))); }
            var segs = new List<(Vector2, Vector2, float, float)>();
            for (var i = 0; i < cells.Count; i++)
            {
                segs.Add((cells[i].p, cells[i].p, 1f, 1f));
                for (var j = i + 1; j < cells.Count; j++)
                    if (Vector2.Distance(cells[i].p, cells[j].p) <= 1.8f * s) segs.Add((cells[i].p, cells[j].p, 1f, 1f));
                // A run that stops one land cell short of the edge (a crossing, s9's moat) carries on past that cell.
                var h = cells[i].h;
                bool Land(int c, int r) => board.At(new Hex(c, r)) != Terrain.Water;
                var (dir, skip) = h.Col == 0 ? (Vector2.left, 0) : h.Col == board.Cols - 1 ? (Vector2.right, 0)
                    : h.Col == 1 && Land(0, h.Row) ? (Vector2.left, 1) : h.Col == board.Cols - 2 && Land(board.Cols - 1, h.Row) ? (Vector2.right, 1)
                    : h.Row == 0 ? (Vector2.up, 0) : h.Row == board.Rows - 1 ? (Vector2.down, 0) : (Vector2.zero, 0);
                if (dir == Vector2.zero) continue;
                var side = new Vector2(-dir.y, dir.x);
                var start = cells[i].p + dir * (skip * 2.5f * s);
                var prev = start;
                for (var k = 1; k <= 40; k++)
                {
                    var bend = 2f * (2f * Mathf.PerlinNoise(k / 10f + ox + i, oz) - 1f) * Mathf.Clamp01(k / 6f);
                    var next = start + dir * k + side * bend;
                    segs.Add((prev, next, 1f + 0.5f * Mathf.Clamp01((k - 1) / 20f), 1f + 0.5f * Mathf.Clamp01(k / 20f)));
                    prev = next;
                }
            }
            return segs;
        }
    }
}
