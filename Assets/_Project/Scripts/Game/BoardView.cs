using UnityEngine;
using VampireSpawn.Core;
using Terrain = VampireSpawn.Core.Terrain;

namespace VampireSpawn.Game
{
    public sealed class BoardView : MonoBehaviour
    {
        public ArtCatalog art;
        public Camera viewCamera;

        public Board Board { get; private set; }
        public Stage Stage { get; private set; }
        Mesh hexMesh;
        float fittedAspect;
        Rect frame = new Rect(0f, 0f, 1f, 1f), fittedFrame;
        Material ground;

        // The normalized screen rect the board must fit in; the camera itself draws the whole screen (stage-dressing.md 1-4).
        public Rect Frame => frame;

        // The fitted pose: where the camera sits and the board point it looks at. BattleCamera moves away from it and back.
        public Vector3 BasePosition { get; private set; }
        public Vector3 BaseLook { get; private set; }

        public void FrameInto(Rect r)
        {
            frame = r;
            if (Board != null) FitCamera();
        }

        static readonly float Sqrt3 = Mathf.Sqrt(3f);

        public Vector3 HexToWorld(Hex h) => transform.TransformPoint(Local(h, art.hexSize));

        // Board-local cell centre; stage dressing prefabs use the same frame (stage-dressing.md 4-4).
        public static Vector3 Local(Hex h, float hexSize) =>
            new Vector3(1.5f * hexSize * h.Col, 0f, -Sqrt3 * hexSize * (h.Row + 0.5f * (h.Col & 1)));

        public float TileTop => art.tileHeight;

        public void Show(Stage stage)
        {
            Clear();
            Stage = stage;
            Board = new Board(stage.Map);
            if (hexMesh == null) hexMesh = BuildHexMesh(art.hexSize * 0.97f, art.tileHeight);
            UseTheme(art.Theme(stage.Id));
            for (var r = 0; r < Board.Rows; r++)
                for (var c = 0; c < Board.Cols; c++)
                    BuildTile(new Hex(c, r));
            Dress(stage.Id);
            FitCamera();
        }

        void LateUpdate()
        {
            if (Board != null && viewCamera != null && (!Mathf.Approximately(viewCamera.aspect, fittedAspect) || frame != fittedFrame)) FitCamera();
        }

        void UseTheme(BoardTheme t)
        {
            if (ground != null) Kill(ground);
            ground = t != null ? new Material(art.plain) { color = t.ground, name = "Ground " + t.name } : null;
            if (t == null) return;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = t.ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = t.fog;
            if (viewCamera != null) viewCamera.backgroundColor = t.fog;
        }

        // Stable per-cell number, so a stage looks the same every time (stage-dressing.md 2).
        internal static int Hash(int c, int r)
        {
            unchecked
            {
                var h = c * 73856093 ^ r * 19349663;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                return (h ^ (h >> 15)) & 0x7fffffff;
            }
        }

        // The stage's baked prefab is its look (stage-dressing.md 4-6); the rules below only build its first shape.
        void Dress(string stageId)
        {
            var prefab = art.Dressing(stageId);
            if (prefab == null) { Debug.LogWarning("꾸미기 프리팹 없음: " + stageId + " (도구 모음 > 전투 보드 > 꾸미기 프리팹 굽기)"); return; }
            Instantiate(prefab, transform, false).name = "Dressing";
        }

        // stage-dressing.md 1-2, 1-3, 1-5, 3: a ground bed, a prop on every rock cell, and a ring of props up to Ring cells past the board; tall ones never on the camera (south) side.
        public static GameObject BuildDressing(ArtCatalog art, Stage stage, Material bed)
        {
            var board = new Board(stage.Map);
            var theme = art.Theme(stage.Id);
            var s = art.hexSize;
            var root = new GameObject(stage.Id);
            root.AddComponent<StageDressing>().stageId = stage.Id;
            var props = new GameObject("Props").transform;
            // Sideways the ring runs twice as far, so 21:9 shows world past the HUD too.
            const int Ring = 4, RingX = 8;
            var a = Local(new Hex(-Ring, -Ring), s);
            var b = Local(new Hex(board.Cols + Ring, board.Rows + Ring), s);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Kill(plane.GetComponent<Collider>());
            plane.name = "Ground";
            plane.transform.SetParent(root.transform, false);
            plane.transform.localPosition = (a + b) * 0.5f + Vector3.down * 0.02f;
            plane.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // Far wider than the ring so its edge never shows, even at 21:9.
            plane.transform.localScale = new Vector3(Mathf.Abs(b.x - a.x) + s * 80f, Mathf.Abs(b.z - a.z) + s * 80f, 1f);
            plane.GetComponent<MeshRenderer>().sharedMaterial = bed;
            props.SetParent(root.transform, false);

            var themed = theme != null && theme.blockers != null && theme.blockers.Length > 0;
            var blockers = themed ? theme.blockers : art.rockProps;
            for (var r = 0; r < board.Rows; r++)
                for (var c = 0; c < board.Cols; c++)
                {
                    var h = new Hex(c, r);
                    if (board.At(h) != Terrain.Rock || blockers == null || blockers.Length == 0) continue;
                    var yaw = themed && theme.alignBlockers ? (c + r) % 2 * 180f : (c * 53 + r * 97) % 360;
                    Place(blockers[(c * 7 + r * 13) % blockers.Length], props, Local(h, s) + Vector3.up * art.tileHeight, (themed ? theme.blockerWidth : 1.5f) * s, yaw, (themed ? 1.3f : 99f) * s, themed && theme.alignBlockers);
                }

            if (theme == null) return root;
            var lift = Vector3.up * art.tileHeight * 0.5f;
            for (var r = -Ring; r < board.Rows + Ring; r++)
                for (var c = -RingX; c < board.Cols + RingX; c++)
                {
                    if (c >= 0 && c < board.Cols && r >= 0 && r < board.Rows) continue;
                    var h = Hash(c, r);
                    var away = Mathf.Max(c < 0 ? -c : c - board.Cols + 1, r < 0 ? -r : r - board.Rows + 1);
                    var south = r >= board.Rows;
                    var jitter = new Vector3((h % 7 - 3) * 0.08f, 0f, (h / 7 % 7 - 3) * 0.08f) * s;
                    var at = Local(new Hex(c, r), s) + lift + jitter;
                    var tallOk = !south && (r < 0 || away >= 2) && theme.border != null && theme.border.Length > 0;
                    if (tallOk && h % 100 < 40)
                    {
                        var width = r <= -Ring + 1 ? 3f + h % 3 * 0.5f : 1.4f + h % 7 * 0.2f;
                        Place(theme.border[h / 100 % theme.border.Length], props, at, width * s, h % 360, (r <= -Ring + 1 ? 6f : 3.5f) * s);
                    }
                    else if (theme.scatter != null && theme.scatter.Length > 0 && h % 100 >= 40 && h % 100 < 70)
                        Place(theme.scatter[h / 100 % theme.scatter.Length], props, at, (0.35f + h % 4 * 0.07f) * s, h % 360, 0.5f * s);
                }
            return root;
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                Kill(child);
            }
            // Without this LateUpdate keeps fitting viewCamera to a board that is gone.
            Board = null;
            Stage = null;
            RenderSettings.fog = false;
        }

        static void Kill(Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

        void BuildTile(Hex h)
        {
            var terrain = Board.At(h);
            var tile = new GameObject($"Tile {h} {terrain}");
            tile.transform.SetParent(transform, false);
            tile.transform.position = HexToWorld(h) + (terrain == Terrain.Water ? Vector3.down * art.tileHeight * 0.6f : Vector3.zero);
            tile.AddComponent<MeshFilter>().sharedMesh = hexMesh;
            tile.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor(h, terrain);

            var top = HexToWorld(h) + Vector3.up * art.tileHeight;
            switch (terrain)
            {
                case Terrain.Torch:
                    if (art.brazier != null) Prop(art.brazier, tile.transform, top, 0.9f, 0f);
                    if (art.fire != null) Prop(art.fire, tile.transform, top + Vector3.up * art.hexSize * 0.55f, 0f, 0f);
                    AddLight(tile.transform, top + Vector3.up * art.hexSize * 1.2f, LightType.Point, art.torchLight, 2.5f, art.hexSize * 4f);
                    break;
                case Terrain.Sun:
                    var sun = AddLight(tile.transform, top + Vector3.up * art.hexSize * 4f, LightType.Spot, art.sunLight, 4f, art.hexSize * 8f);
                    sun.transform.rotation = Quaternion.LookRotation(Vector3.down);
                    sun.spotAngle = 28f;
                    break;
            }
        }

        Material MaterialFor(Hex h, Terrain t) => t switch
        {
            Terrain.Rock => art.rock,
            Terrain.Water => art.water,
            Terrain.Sun => art.sunlit,
            _ => Board.Lit(h) ? art.lit : ground != null ? ground : art.plain,
        };

        // fitWidth 0 keeps the prefab's own scale (used for particle FX whose bounds are meaningless).
        // stretch: a wall piece keeps its length along the row (x) while the height cap squashes only y and z, so walls stay walls instead of shrinking to stubs.
        void Prop(GameObject prefab, Transform parent, Vector3 at, float fitWidth, float yaw, float maxHeight = 99f, bool stretch = false) =>
            Place(prefab, parent, at, fitWidth * art.hexSize, yaw, maxHeight * art.hexSize, stretch);

        // Shared with CampaignMapView (campaign-map.md 2-5): sizes are in world units.
        internal static GameObject Place(GameObject prefab, Transform parent, Vector3 at, float fitWidth, float yaw, float maxHeight = 99f, bool stretch = false)
        {
            var go = Spawn(prefab, parent);
            go.name = prefab.name;
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (fitWidth <= 0f) return go;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return go;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            var size = Mathf.Max(b.size.x, b.size.z);
            if (size <= 0.001f) return go;
            var byWidth = fitWidth / size;
            var byHeight = b.size.y > 0.001f ? maxHeight / b.size.y : float.MaxValue;
            var k = Mathf.Min(byWidth, byHeight);
            go.transform.localScale = Vector3.Scale(go.transform.localScale, stretch && b.size.x > 0.001f ? new Vector3(fitWidth / b.size.x, k, k) : Vector3.one * k);
            return go;
        }

        // Out of play mode (baking) keep the prefab link, so a baked stage or map can swap or update a prop later.
        static GameObject Spawn(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.PrefabUtility.IsPartOfPrefabAsset(prefab) && UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) is GameObject linked) return linked;
#endif
            return Instantiate(prefab, parent);
        }

        internal static Light AddLight(Transform parent, Vector3 at, LightType type, Color color, float intensity, float range)
        {
            var go = new GameObject(type + " Light");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            return l;
        }

        void FitCamera()
        {
            if (viewCamera == null || Board == null) return;
            var a = HexToWorld(new Hex(0, 0));
            var b = HexToWorld(new Hex(Board.Cols - 1, Board.Rows - 1));
            var center = (a + b) * 0.5f + Vector3.back * art.hexSize * 0.6f;
            var span = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.z - a.z) * 1.6f) + art.hexSize * 2f;
            var pitch = 55f;
            var distance = span * 0.5f / Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.8f / Mathf.Max(0.1f, frame.height);
            var rot = Quaternion.Euler(pitch, 0f, 0f);
            viewCamera.transform.rotation = rot;
            fittedAspect = viewCamera.aspect;
            fittedFrame = frame;
            // Shift the projection so the camera axis lands on the frame's centre. The axis lands at NDC -p[0,2], hence the minus.
            viewCamera.ResetProjectionMatrix();
            var p = viewCamera.projectionMatrix;
            p[0, 2] = -2f * (frame.center.x - 0.5f);
            p[1, 2] = -2f * (frame.center.y - 0.5f);
            viewCamera.projectionMatrix = p;
            // A narrow frame (4:3, 5:4) crops the near rows sideways under perspective, so back off until every board corner is inside it.
            var h = art.hexSize;
            var corners = new[] { a + new Vector3(-h, 0f, h), new Vector3(b.x + h, 0f, a.z + h), new Vector3(a.x - h, 0f, b.z - h), b + new Vector3(h, 0f, -h) };
            for (var i = 0; i < 60; i++)
            {
                viewCamera.transform.position = center - rot * Vector3.forward * distance;
                if (System.Array.TrueForAll(corners, c => frame.Contains(viewCamera.WorldToViewportPoint(c)))) break;
                distance *= 1.04f;
            }
            BasePosition = viewCamera.transform.position;
            BaseLook = center;
            RenderSettings.fogStartDistance = distance * 1.1f;
            RenderSettings.fogEndDistance = distance * 2.4f;
        }

        static Mesh BuildHexMesh(float radius, float height)
        {
            var verts = new Vector3[7 + 24];
            var tris = new System.Collections.Generic.List<int>();
            verts[0] = new Vector3(0f, height, 0f);
            for (var i = 0; i < 6; i++)
            {
                var a = Mathf.Deg2Rad * 60f * i;
                verts[1 + i] = new Vector3(radius * Mathf.Cos(a), height, radius * Mathf.Sin(a));
            }
            for (var i = 0; i < 6; i++) tris.AddRange(new[] { 0, 1 + (i + 1) % 6, 1 + i });
            for (var i = 0; i < 6; i++)
            {
                var p0 = verts[1 + i];
                var p1 = verts[1 + (i + 1) % 6];
                var k = 7 + i * 4;
                verts[k] = p0; verts[k + 1] = p1;
                verts[k + 2] = new Vector3(p1.x, 0f, p1.z); verts[k + 3] = new Vector3(p0.x, 0f, p0.z);
                tris.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            var mesh = new Mesh { name = "Hex", vertices = verts, triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
