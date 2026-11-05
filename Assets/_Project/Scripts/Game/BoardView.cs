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

        static readonly float Sqrt3 = Mathf.Sqrt(3f);

        public Vector3 HexToWorld(Hex h) =>
            transform.TransformPoint(new Vector3(1.5f * art.hexSize * h.Col, 0f, -Sqrt3 * art.hexSize * (h.Row + 0.5f * (h.Col & 1))));

        public float TileTop => art.tileHeight;

        public void Show(Stage stage)
        {
            Clear();
            Stage = stage;
            Board = new Board(stage.Map);
            if (hexMesh == null) hexMesh = BuildHexMesh(art.hexSize * 0.97f, art.tileHeight);
            for (var r = 0; r < Board.Rows; r++)
                for (var c = 0; c < Board.Cols; c++)
                    BuildTile(new Hex(c, r));
            FitCamera();
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }

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
                case Terrain.Rock:
                    var props = art.rockProps;
                    if (props != null && props.Length > 0) Prop(props[(h.Col * 7 + h.Row * 13) % props.Length], tile.transform, top, 1.5f, (h.Col * 53 + h.Row * 97) % 360);
                    break;
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
            _ => Board.Lit(h) ? art.lit : art.plain,
        };

        // fitWidth 0 keeps the prefab's own scale (used for particle FX whose bounds are meaningless).
        void Prop(GameObject prefab, Transform parent, Vector3 at, float fitWidth, float yaw)
        {
            var go = Instantiate(prefab, parent);
            go.name = prefab.name;
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (fitWidth <= 0f) return;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            var size = Mathf.Max(b.size.x, b.size.z);
            if (size > 0.001f) go.transform.localScale *= fitWidth * art.hexSize / size;
        }

        static Light AddLight(Transform parent, Vector3 at, LightType type, Color color, float intensity, float range)
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
            if (viewCamera == null) return;
            var a = HexToWorld(new Hex(0, 0));
            var b = HexToWorld(new Hex(Board.Cols - 1, Board.Rows - 1));
            var center = (a + b) * 0.5f;
            var span = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.z - a.z) * 1.6f) + art.hexSize * 2f;
            var pitch = 55f;
            var distance = span * 0.5f / Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.72f;
            var rot = Quaternion.Euler(pitch, 0f, 0f);
            viewCamera.transform.rotation = rot;
            viewCamera.transform.position = center - rot * Vector3.forward * distance;
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
