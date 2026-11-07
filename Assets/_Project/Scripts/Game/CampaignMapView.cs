using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // docs/art/campaign-map.md, first pass (1-1). The fixed terrain is baked into the Camp scene (Tools > 지도 > 씬에 굽기) so people can
    // move props in Unity; regions are found by name, so markers, braziers and the camera follow a moved region. Show rebuilds only progress.
    // Look moves the shared view camera and the spotlight together so the UI markers never lag a frame behind.
    public sealed class CampaignMapView : MonoBehaviour
    {
        public ArtCatalog art;
        public Camera viewCamera;
        [Tooltip("씬에 구운 지형 (Campaign Map). 비면 실행 중에 짓는다")] public Transform terrain;

        const float Pitch = 50f, MoveSeconds = 0.3f, CampDistance = 28f, WideStart = 80f, WideStep = 1.04f;

        Transform progress;
        Light spot;
        Campaign campaign;
        Shot from, to;
        float moveStart = -1f;
        CampLayer layer;
        int stage;
        Rect empty;
        Vector2 margin;
        float fittedAspect;

        struct Shot { public Vector3 Target; public float Distance; public Vector2 Centre; }

        public bool Showing => terrain != null && terrain.gameObject.activeSelf;

        static Vector3 World(Vector2 local, float y = 0f) => CampaignMapLayout.Origin + new Vector3(local.x, y, local.y);

        public void Show(Campaign c)
        {
            campaign = c;
            if (terrain == null) terrain = Build(transform, art, (c, n) => new Material(art.plain) { color = c, name = n });
            if (spot == null)
            {
                spot = BoardView.AddLight(transform, Vector3.zero, LightType.Spot, CampaignMapLayout.Moon, 3f, 30f);
                spot.spotAngle = 40f;
                spot.enabled = false;
            }
            terrain.gameObject.SetActive(true);
            if (progress != null) Kill(progress.gameObject);
            progress = new GameObject("Progress").transform;
            progress.SetParent(terrain, false);
            BuildProgress();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = CampaignMapLayout.Ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = CampaignMapLayout.Fog;
            if (viewCamera != null)
            {
                viewCamera.backgroundColor = CampaignMapLayout.Fog;
                viewCamera.targetTexture = null;
                viewCamera.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        public void Hide()
        {
            if (terrain != null) terrain.gameObject.SetActive(false);
            moveStart = -1f;
            RenderSettings.fog = false;
        }

        // empty and margin are normalized viewport units (stage-select.md 2-3, campaign-map.md 9: markers stay 48 px inside).
        public void Look(CampLayer layer, int stage, Rect empty, Vector2 margin, bool snap = false)
        {
            this.layer = layer;
            this.stage = stage;
            this.empty = empty;
            this.margin = margin;
            var next = ShotFor(layer, stage);
            from = snap || moveStart < 0f ? next : Current();
            to = next;
            moveStart = snap ? Time.unscaledTime - MoveSeconds : Time.unscaledTime;
            if (spot != null)
            {
                spot.enabled = layer == CampLayer.Info;
                var r = RegionOf(stage);
                if (r != null)
                {
                    var a = AnchorOf(r);
                    spot.transform.position = a + new Vector3(0f, 16f, -4f);
                    spot.transform.rotation = Quaternion.LookRotation(a - spot.transform.position);
                }
            }
            Apply();
        }

        public Vector3? MarkerPoint(int stageIndex)
        {
            var r = RegionOf(stageIndex);
            return r == null ? (Vector3?)null : AnchorOf(r) + Vector3.up * r.MarkerHeight;
        }

        // Viewport point of a region's marker, or null behind the camera.
        public Vector2? ScreenPoint(int stageIndex)
        {
            var p = MarkerPoint(stageIndex);
            if (p == null || viewCamera == null) return null;
            var v = viewCamera.WorldToViewportPoint(p.Value);
            return v.z <= 0f ? (Vector2?)null : new Vector2(v.x, v.y);
        }

        void LateUpdate()
        {
            if (!Showing || viewCamera == null || moveStart < 0f) return;
            if (!Mathf.Approximately(viewCamera.aspect, fittedAspect)) { to = ShotFor(layer, stage); from = to; }
            if (Time.unscaledTime - moveStart <= MoveSeconds + Time.unscaledDeltaTime) Apply();
        }

        static MapRegion RegionOf(int stageIndex) =>
            stageIndex >= 0 && stageIndex < Campaign.Stages.Length ? CampaignMapLayout.Region(Campaign.Stages[stageIndex].Id) : null;

        Shot Current()
        {
            var t = Mathf.Clamp01((Time.unscaledTime - moveStart) / MoveSeconds);
            t = t * t * (3f - 2f * t);
            return new Shot { Target = Vector3.Lerp(from.Target, to.Target, t), Distance = Mathf.Lerp(from.Distance, to.Distance, t), Centre = Vector2.Lerp(from.Centre, to.Centre, t) };
        }

        void Apply()
        {
            var s = Current();
            Place(s);
            RenderSettings.fogStartDistance = s.Distance * 0.9f;
            RenderSettings.fogEndDistance = s.Distance * 2.2f;
        }

        void Place(Shot s)
        {
            var rot = Quaternion.Euler(Pitch, 0f, 0f);
            viewCamera.transform.rotation = rot;
            viewCamera.transform.position = s.Target - rot * Vector3.forward * s.Distance;
            // Same off-centre projection as BoardView.FitCamera: the camera axis lands on the empty area's centre.
            viewCamera.ResetProjectionMatrix();
            var p = viewCamera.projectionMatrix;
            p[0, 2] = -2f * (s.Centre.x - 0.5f);
            p[1, 2] = -2f * (s.Centre.y - 0.5f);
            viewCamera.projectionMatrix = p;
            fittedAspect = viewCamera.aspect;
        }

        Shot ShotFor(CampLayer l, int stageIndex)
        {
            var centre = empty.center;
            if (l == CampLayer.Barracks)
            {
                var camp = CampSpot();
                return new Shot { Target = camp, Distance = CampDistance, Centre = centre };
            }
            if (l == CampLayer.Info && RegionOf(stageIndex) is MapRegion r)
                return new Shot { Target = AnchorOf(r) + Vector3.forward * 2f, Distance = r.ViewDistance, Centre = centre };
            return Wide(centre);
        }

        // campaign-map.md 9 wide shot: start at 80 and back off until every anchor and marker point sits inside the empty area.
        Shot Wide(Vector2 centre)
        {
            var anchors = CampaignMapLayout.Regions.Select(AnchorOf).ToArray();
            var min = new Vector3(anchors.Min(a => a.x), anchors.Min(a => a.y), anchors.Min(a => a.z));
            var max = new Vector3(anchors.Max(a => a.x), anchors.Max(a => a.y), anchors.Max(a => a.z));
            var shot = new Shot { Target = (min + max) * 0.5f, Distance = WideStart, Centre = centre };
            if (viewCamera == null) return shot;
            var points = CampaignMapLayout.Regions.SelectMany(r => new[] { AnchorOf(r), AnchorOf(r) + Vector3.up * r.MarkerHeight }).ToArray();
            var inner = Rect.MinMaxRect(empty.xMin + margin.x, empty.yMin + margin.y, empty.xMax - margin.x, empty.yMax - margin.y);
            for (var i = 0; i < 60; i++)
            {
                Place(shot);
                if (points.All(p => inner.Contains(viewCamera.WorldToViewportPoint(p)))) break;
                shot.Distance *= WideStep;
            }
            return shot;
        }

        // 1-1: the camp itself is deferred, so the barracks view looks at the last cleared region (stage 1 if none).
        Vector3 CampSpot()
        {
            var last = -1;
            for (var i = 0; i < Campaign.Stages.Length; i++) if (campaign != null && campaign.IsCleared(Campaign.Stages[i])) last = i;
            var r = RegionOf(Mathf.Max(0, last));
            return r != null ? AnchorOf(r) : World(CampaignMapLayout.Start);
        }

        // A region baked into the scene may have been moved by hand; its object is the truth, the layout table the fallback.
        Vector3 AnchorOf(MapRegion r)
        {
            var root = terrain != null ? terrain.Find("Region " + r.Stage) : null;
            return root != null ? root.position : World(r.Anchor);
        }

        // Builds the fixed terrain under parent. material makes the tinted materials: runtime copies here, saved assets when baking.
        public static Transform Build(Transform parent, ArtCatalog art, System.Func<Color, string, Material> material)
        {
            var terrain = new GameObject("Campaign Map").transform;
            terrain.SetParent(parent, false);
            terrain.position = CampaignMapLayout.Origin;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Kill(floor.GetComponent<Collider>());
            floor.name = "Floor";
            floor.transform.SetParent(terrain, false);
            floor.transform.localPosition = new Vector3(0f, 0f, 50f);
            floor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = new Vector3(400f, 400f, 1f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = material(CampaignMapLayout.Floor, "Map Floor");

            foreach (var r in CampaignMapLayout.Regions)
            {
                var region = new GameObject("Region " + r.Stage).transform;
                region.SetParent(terrain, false);
                region.position = World(r.Anchor);
                // A flattened built-in cylinder, not a generated mesh: a mesh made at bake time would not survive saving the scene.
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Kill(ground.GetComponent<Collider>());
                ground.name = "Ground";
                ground.transform.SetParent(region, false);
                ground.transform.localPosition = Vector3.up * 0.01f;
                ground.transform.localScale = new Vector3(r.Radius * 2f, 0.005f, r.Radius * 2f);
                var theme = art.Theme(r.Stage);
                ground.GetComponent<MeshRenderer>().sharedMaterial = material((theme != null ? theme.ground : CampaignMapLayout.Floor) * 1.25f, "Region " + r.Stage);
                foreach (var p in r.Props)
                {
                    var prefab = art.MapProp(p.Prefab);
                    if (prefab == null) { Debug.LogWarning("지도 프리팹 없음: " + p.Prefab + " (도구 모음 > 지도 > 프리팹 목록 채우기)"); continue; }
                    BoardView.Place(prefab, region, World(r.Anchor + new Vector2(p.Dx, p.Dz), p.Lift), p.Width, p.Yaw, p.MaxHeight > 0f ? p.MaxHeight : 99f);
                }
                foreach (var l in r.Lights)
                    BoardView.AddLight(region, World(r.Anchor + new Vector2(l.At.x, l.At.z), l.At.y), LightType.Point, l.Blood ? CampaignMapLayout.Blood : art.torchLight, l.Intensity, l.Range);
            }
            BuildForest(terrain, art);
            return terrain;
        }

        void BuildProgress()
        {
            var lastOpen = 0;
            for (var i = 0; i < Campaign.Stages.Length; i++) if (campaign.StageOpen(i)) lastOpen = i;
            var split = RegionOf(lastOpen) is MapRegion open ? System.Array.FindIndex(CampaignMapLayout.Road, p => (p - open.Anchor).sqrMagnitude < 0.01f) : 0;
            var lit = CampaignMapLayout.Road.Take(split + 1).ToArray();
            var dark = CampaignMapLayout.Road.Skip(split).ToArray();
            if (lit.Length > 1) Ribbon("Road Lit", lit, CampaignMapLayout.RoadLit, 0.04f);
            if (dark.Length > 1) Ribbon("Road Dark", dark, CampaignMapLayout.RoadDark, 0.039f);
            for (var i = 0; i < Campaign.Stages.Length; i++)
            {
                if (!campaign.IsCleared(Campaign.Stages[i]) || !(RegionOf(i) is MapRegion r)) continue;
                var at = AnchorOf(r) + new Vector3(r.Brazier.x, 0f, r.Brazier.y);
                var brazier = art.MapProp(CampaignMapLayout.Brazier);
                if (brazier != null) BoardView.Place(brazier, progress, at, 0.9f, 0f);
                var fire = art.MapProp(CampaignMapLayout.Fire);
                if (fire != null) BoardView.Place(fire, progress, at + Vector3.up * 0.55f, 0f, 0f);
                BoardView.AddLight(progress, at + Vector3.up * 1.5f, LightType.Point, art.torchLight, 2.2f, 10f);
            }
        }

        // 3-2: one prop in 45% of 6-unit cells, kept clear of the road, regions, camp spots and the camera edge.
        static void BuildForest(Transform terrain, ArtCatalog art)
        {
            var forest = new GameObject("Forest").transform;
            forest.SetParent(terrain, false);
            var road = Path(CampaignMapLayout.Road);
            var camps = CampaignMapLayout.Regions.Select(r => r.Camp).Append(CampaignMapLayout.Start).ToArray();
            var cell = CampaignMapLayout.ForestCell;
            for (var x = CampaignMapLayout.ForestMinX; x < CampaignMapLayout.ForestMaxX; x += cell)
            for (var z = CampaignMapLayout.ForestMinZ; z < CampaignMapLayout.ForestMaxZ; z += cell)
            {
                var cx = Mathf.RoundToInt(x / cell);
                var cz = Mathf.RoundToInt(z / cell);
                var h = BoardView.Hash(cx + 1000, cz + 1000);
                if (h % 100 >= CampaignMapLayout.ForestShare * 100f) continue;
                var p = new Vector2(x + cell * 0.5f + (h / 7 % 31 - 15) * 0.1f, z + cell * 0.5f + (h / 13 % 31 - 15) * 0.1f);
                if (p.y < CampaignMapLayout.ForestNearEdge) continue;
                if (road.Any(q => (q - p).sqrMagnitude < CampaignMapLayout.ClearOfRoad * CampaignMapLayout.ClearOfRoad)) continue;
                if (CampaignMapLayout.Regions.Any(r => (r.Anchor - p).magnitude < (r.Radius > 6f ? CampaignMapLayout.ClearOfCastle : CampaignMapLayout.ClearOfRegion))) continue;
                if (camps.Any(c => (c - p).magnitude < CampaignMapLayout.ClearOfCamp)) continue;
                var band = CampaignMapLayout.Forest.FirstOrDefault(b => p.y >= b.MinZ && p.y < b.MaxZ);
                if (band.Trees == null) continue;
                var tree = h / 101 % 10 < 7;
                var names = tree ? band.Trees : band.Smalls;
                var width = tree ? band.TreeWidth : band.SmallWidth;
                var prefab = art.MapProp(names[h / 1009 % names.Length]);
                if (prefab == null) continue;
                BoardView.Place(prefab, forest, World(p), Mathf.Lerp(width.x, width.y, h / 10007 % 100 / 99f), h % 360);
            }
        }

        // Catmull-Rom through the control points, one sample per unit (5-1).
        static Vector2[] Path(Vector2[] points)
        {
            var result = new List<Vector2>();
            for (var i = 0; i < points.Length - 1; i++)
            {
                var p0 = points[Mathf.Max(0, i - 1)];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = points[Mathf.Min(points.Length - 1, i + 2)];
                var steps = Mathf.Max(1, Mathf.CeilToInt((p2 - p1).magnitude));
                for (var s = 0; s < steps; s++)
                {
                    var t = s / (float)steps;
                    var t2 = t * t;
                    var t3 = t2 * t;
                    result.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3));
                }
            }
            result.Add(points[points.Length - 1]);
            return result.ToArray();
        }

        void Ribbon(string name, Vector2[] controls, Color color, float y)
        {
            var path = Path(controls);
            var verts = new Vector3[path.Length * 2];
            var tris = new List<int>();
            var half = CampaignMapLayout.RoadWidth * 0.5f;
            for (var i = 0; i < path.Length; i++)
            {
                var dir = (path[Mathf.Min(path.Length - 1, i + 1)] - path[Mathf.Max(0, i - 1)]).normalized;
                var side = new Vector2(-dir.y, dir.x) * half;
                verts[i * 2] = new Vector3(path[i].x - side.x, y, path[i].y - side.y);
                verts[i * 2 + 1] = new Vector3(path[i].x + side.x, y, path[i].y + side.y);
                if (i == 0) continue;
                var k = i * 2;
                tris.AddRange(new[] { k - 2, k - 1, k, k - 1, k + 1, k });
            }
            var mesh = new Mesh { name = name, vertices = verts, triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            var go = new GameObject(name);
            go.transform.SetParent(progress, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Tinted(color, name);
        }

        Material Tinted(Color c, string name) => new Material(art.plain) { color = c, name = name };

        static void Kill(Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
    }
}
