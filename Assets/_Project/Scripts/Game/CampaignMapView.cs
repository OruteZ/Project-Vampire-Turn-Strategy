using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // docs/art/campaign-map.md, first pass (1-1). The fixed terrain and its Cinemachine shots are baked into the Camp scene
    // (Tools > 지도 > Camp 씬에 지도 굽기) so people can move props and tune each shot in Unity. Regions are found by name, so markers,
    // braziers and shots follow a moved region. Show rebuilds only progress; Look picks a shot and the brain blends to it.
    public sealed class CampaignMapView : MonoBehaviour
    {
        public ArtCatalog art;
        public Camera viewCamera;
        [Tooltip("씬에 구운 지형 (Campaign Map, 그 아래 Cameras 의 Cinemachine 시점들). 비면 실행 중에 짓는다")] public Transform terrain;

        // campaign-map.md 9: pitch 50, the scene's FOV 40, camp distance 28, 0.3 s between shots.
        public const float Pitch = 50f, Fov = 40f, CampDistance = 28f, BlendSeconds = 0.3f, WideDistance = 80f;

        Transform progress;
        Light spot;
        Campaign campaign;

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
                UseBrain(viewCamera);
            }
            var camp = Shot("Shot Camp");
            if (camp != null) camp.Follow = camp.LookAt = CampSpot();
        }

        public static CinemachineBrain UseBrain(Camera cam)
        {
            var brain = cam.GetComponent<CinemachineBrain>();
            if (brain == null) brain = cam.gameObject.AddComponent<CinemachineBrain>();
            // With LensModeOverride off the brain only writes lens shift into a camera that is already physical.
            cam.usePhysicalProperties = true;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, BlendSeconds);
            return brain;
        }

        public void Hide()
        {
            if (terrain != null) terrain.gameObject.SetActive(false);
            RenderSettings.fog = false;
        }

        // empty and margin are normalized viewport units (stage-select.md 2-3, campaign-map.md 9: markers stay 48 px inside).
        // Only the chosen shot is enabled; it aims at its subject and a lens shift slides the picture so the subject sits at the empty
        // area's centre. The composer's screen offset is angular and lands about 35% short sideways, so it is not used for this.
        public void Look(CampLayer layer, int stage, Rect empty, Vector2 margin, bool snap = false)
        {
            var r = RegionOf(stage);
            var chosen = layer == CampLayer.Barracks ? Shot("Shot Camp") : layer == CampLayer.Info && r != null ? Shot("Shot " + r.Stage) : Shot("Shot Wide");
            foreach (var cam in Shots()) cam.enabled = cam == chosen;
            this.empty = empty;
            if (chosen != null)
            {
                Shift(chosen);
                if (chosen.TryGetComponent<CinemachineGroupFraming>(out var framing))
                    framing.FramingSize = Mathf.Min(empty.width - 2f * margin.x, empty.height - 2f * margin.y);
            }
            if (snap && viewCamera != null && viewCamera.TryGetComponent<CinemachineBrain>(out var brain)) brain.ResetState();
            if (spot != null)
            {
                spot.enabled = layer == CampLayer.Info;
                if (r != null)
                {
                    var a = AnchorOf(r);
                    spot.transform.position = a + new Vector3(0f, 16f, -4f);
                    spot.transform.rotation = Quaternion.LookRotation(a - spot.transform.position);
                }
            }
        }

        Rect empty = new Rect(0f, 0f, 1f, 1f);

        // Sensor at the screen's aspect with a vertical gate, so the vertical FOV stays 40 and a shift of 1 is one screen width.
        void Shift(CinemachineCamera cam)
        {
            var aspect = viewCamera != null ? viewCamera.aspect : 16f / 9f;
            cam.Lens.PhysicalProperties.SensorSize = new Vector2(24f * aspect, 24f);
            cam.Lens.PhysicalProperties.LensShift = new Vector2(0.5f - empty.center.x, 0.5f - empty.center.y);
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

        // Fog follows the camera's distance (campaign-map.md 8: start x0.9, end x2.2), read off its height at the fixed pitch.
        void LateUpdate()
        {
            if (!Showing || viewCamera == null) return;
            foreach (var cam in Shots())
                if (cam.enabled && !Mathf.Approximately(cam.Lens.PhysicalProperties.SensorSize.x, 24f * viewCamera.aspect)) Shift(cam);
            var distance = Mathf.Max(1f, (viewCamera.transform.position.y - CampaignMapLayout.Origin.y) / Mathf.Sin(Pitch * Mathf.Deg2Rad));
            RenderSettings.fogStartDistance = distance * 0.9f;
            RenderSettings.fogEndDistance = distance * 2.2f;
        }

        static MapRegion RegionOf(int stageIndex) =>
            stageIndex >= 0 && stageIndex < Campaign.Stages.Length ? CampaignMapLayout.Region(Campaign.Stages[stageIndex].Id) : null;

        IEnumerable<CinemachineCamera> Shots()
        {
            var cams = terrain != null ? terrain.Find("Cameras") : null;
            return cams != null ? cams.GetComponentsInChildren<CinemachineCamera>(true) : Enumerable.Empty<CinemachineCamera>();
        }

        CinemachineCamera Shot(string name) => Shots().FirstOrDefault(c => c.name == name);

        // 1-1: the camp itself is deferred, so the barracks view looks at the last cleared region (stage 1 if none).
        Transform CampSpot()
        {
            var last = -1;
            for (var i = 0; i < Campaign.Stages.Length; i++) if (campaign != null && campaign.IsCleared(Campaign.Stages[i])) last = i;
            var r = RegionOf(Mathf.Max(0, last));
            return r != null && terrain != null ? terrain.Find("Region " + r.Stage) : null;
        }

        // A region baked into the scene may have been moved by hand; its object is the truth, the layout table the fallback.
        Vector3 AnchorOf(MapRegion r)
        {
            var root = terrain != null ? terrain.Find("Region " + r.Stage) : null;
            return root != null ? root.position : World(r.Anchor);
        }

        // One Cinemachine shot: follows its subject at the given distance and pitch; the composer aims it (campaign-map.md 9).
        static CinemachineCamera NewShot(Transform parent, string name, Transform subject, float distance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var cam = go.AddComponent<CinemachineCamera>();
            cam.Lens.FieldOfView = Fov;
            cam.Lens.PhysicalProperties.GateFit = Camera.GateFitMode.Vertical;
            cam.Lens.FarClipPlane = 1000f;
            cam.Follow = cam.LookAt = subject;
            var follow = go.AddComponent<CinemachineFollow>();
            var rad = Pitch * Mathf.Deg2Rad;
            follow.FollowOffset = new Vector3(0f, Mathf.Sin(rad) * distance, -Mathf.Cos(rad) * distance);
            follow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping = Vector3.zero;
            var composer = go.AddComponent<CinemachineRotationComposer>();
            composer.Damping = Vector2.zero;
            cam.enabled = false;
            return cam;
        }

        static void BuildShots(Transform terrain)
        {
            var cams = new GameObject("Cameras").transform;
            cams.SetParent(terrain, false);
            var group = new GameObject("Regions").AddComponent<CinemachineTargetGroup>();
            group.transform.SetParent(cams, false);
            foreach (var r in CampaignMapLayout.Regions)
            {
                var root = terrain.Find("Region " + r.Stage);
                if (root != null) group.AddMember(root, 1f, r.Radius);
            }
            var wide = NewShot(cams, "Shot Wide", group.transform, WideDistance);
            var framing = wide.gameObject.AddComponent<CinemachineGroupFraming>();
            framing.SizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly;
            framing.FramingMode = CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
            foreach (var r in CampaignMapLayout.Regions)
            {
                var root = terrain.Find("Region " + r.Stage);
                if (root != null) NewShot(cams, "Shot " + r.Stage, root, r.ViewDistance);
            }
            NewShot(cams, "Shot Camp", null, CampDistance);
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
            BuildShots(terrain);
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
