using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using VampireSpawn.Core;

// The map baker (Editor) builds with BoardView.Place and friends.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("VampireSpawn.Editor")]

namespace VampireSpawn.Game
{
    // docs/art/campaign-map.md. The fixed terrain and its Cinemachine shots are baked into the Camp scene
    // (Tools > 지도 > Camp 씬에 지도 굽기) so people can move props, points and shots in Unity; the scene is the map's only truth at run time.
    // Show rebuilds only progress (lit road, braziers, camp) from the baked points; Look picks a shot and the brain blends to it.
    public sealed class CampaignMapView : MonoBehaviour
    {
        public ArtCatalog art;
        public Camera viewCamera;
        [Tooltip("씬에 구운 지형 (Campaign Map, 그 아래 Cameras 의 Cinemachine 시점들). 비면 지도가 뜨지 않는다")] public Transform terrain;
        [Tooltip("지도 레이어 WASD 이동 속도 (초당 단위)")] public float panSpeed = 40f;
        [Tooltip("지도 레이어 WASD 이동 범위: 전경 시점에서 동서(x)·남북(y)으로 갈 수 있는 거리")] public Vector2 panRange = new Vector2(30f, 45f);

        // campaign-map.md 9: pitch 50, the scene's FOV 40, camp distance 28, 0.3 s between shots.
        public const float Pitch = 50f, Fov = 40f, CampDistance = 28f, BlendSeconds = 0.3f, WideDistance = 80f;

        Transform progress;
        Light spot;
        Campaign campaign;

        public bool Showing => terrain != null && terrain.gameObject.activeSelf;

        public void Show(Campaign c)
        {
            campaign = c;
            if (terrain == null) { Debug.LogError("Camp 씬에 구운 지도가 없다 (도구 모음 > 지도 > Camp 씬에 지도 굽기)"); return; }
            if (spot == null)
            {
                spot = BoardView.AddLight(transform, Vector3.zero, LightType.Spot, CampaignMapLayout.Moon, 3f, 30f);
                spot.spotAngle = 40f;
                spot.enabled = false;
            }
            terrain.gameObject.SetActive(true);
            fogDistance = 0f;
            pan = Vector2.zero;
            if (progress != null) BoardView.Kill(progress.gameObject);
            progress = new GameObject("Progress").transform;
            progress.SetParent(terrain, false);
            BuildProgress();
            if (terrain.Find("Water") == null) BuildWater();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = CampaignMapLayout.AmbientSky;
            RenderSettings.ambientEquatorColor = CampaignMapLayout.AmbientEquator;
            RenderSettings.ambientGroundColor = CampaignMapLayout.AmbientGround;
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

        // empty and margin are normalized viewport units (stage-select.md 2-3, campaign-map.md 9: markers stay 48 px inside).
        // Only the chosen shot is enabled; it aims at its subject and a lens shift slides the picture so the subject sits at the empty
        // area's centre. The composer's screen offset is angular and lands about 35% short sideways, so it is not used for this.
        public void Look(CampLayer layer, int stage, Rect empty, Vector2 margin, bool snap = false)
        {
            var r = RegionOf(stage);
            var chosen = layer == CampLayer.Barracks ? Shot("Shot Camp") : layer == CampLayer.Info && r != null ? Shot("Shot " + Campaign.Stages[RegionStage(stage)].Id) : Shot("Shot Wide");
            foreach (var cam in Shots()) cam.enabled = cam == chosen;
            this.empty = empty;
            panning = layer == CampLayer.Map ? chosen : null;
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
                    var a = r.position;
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
            return r == null ? (Vector3?)null : PointOr(r, "Marker", r.position);
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
            var distance = Mathf.Max(1f, (viewCamera.transform.position.y - terrain.position.y) / Mathf.Sin(Pitch * Mathf.Deg2Rad));
            if (Mathf.Approximately(distance, fogDistance)) return;
            fogDistance = distance;
            RenderSettings.fogStartDistance = distance * 0.9f;
            RenderSettings.fogEndDistance = distance * 2.2f;
        }

        float fogDistance;

        CinemachineCamera panning;
        Vector2 pan;

        // Arrows stay with marker focus (CampFlow). The offset is camera-local after Aim, so it slides the wide shot without re-aiming.
        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (!Showing || panning == null || kb == null) return;
            var move = new Vector2((kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f), (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f));
            pan += move.normalized * panSpeed * Time.unscaledDeltaTime;
            pan = new Vector2(Mathf.Clamp(pan.x, -panRange.x, panRange.x), Mathf.Clamp(pan.y, -panRange.y, panRange.y));
            if (!panning.TryGetComponent<CinemachineCameraOffset>(out var offset)) offset = panning.gameObject.AddComponent<CinemachineCameraOffset>();
            var rad = Pitch * Mathf.Deg2Rad;
            offset.Offset = new Vector3(pan.x, pan.y * Mathf.Sin(rad), pan.y * Mathf.Cos(rad));
        }

        // The running map reads only the scene: regions, their Marker/Brazier/Camp points, Paths and the Camp Template are baked
        // objects people may move by hand. CampaignMapLayout only feeds the bake (campaign-map.md 10).
        public const string PathsName = "Paths", StartName = "Start", CampTemplateName = "Camp Template";

        // Stages past the last region on the map are inside the castle and share its region (docs/areas/battle.md, 20 stages).
        // -1 when the stage is out of range or nothing is baked.
        public int RegionStage(int stageIndex)
        {
            if (terrain == null || stageIndex < 0 || stageIndex >= Campaign.Stages.Length) return -1;
            for (var i = stageIndex; i >= 0; i--) if (terrain.Find("Region " + Campaign.Stages[i].Id) != null) return i;
            return -1;
        }

        Transform RegionOf(int stageIndex)
        {
            var i = RegionStage(stageIndex);
            return i < 0 ? null : terrain.Find("Region " + Campaign.Stages[i].Id);
        }

        static Vector3 PointOr(Transform parent, string name, Vector3 fallback)
        {
            var t = parent.Find(name);
            return t != null ? t.position : fallback;
        }

        Vector2 Local(Vector3 world)
        {
            var p = terrain.InverseTransformPoint(world);
            return new Vector2(p.x, p.z);
        }

        Vector2[] PathPoints(Transform path) => path.Cast<Transform>().Select(t => Local(t.position)).ToArray();

        IEnumerable<Transform> Paths(string prefix)
        {
            var paths = terrain.Find(PathsName);
            return paths == null ? Enumerable.Empty<Transform>() : paths.Cast<Transform>().Where(t => t.name.StartsWith(prefix));
        }

        CinemachineCamera[] shots;
        Transform shotsOf;

        CinemachineCamera[] Shots()
        {
            if (shots != null && shotsOf == terrain) return shots;
            shotsOf = terrain;
            var cams = terrain != null ? terrain.Find("Cameras") : null;
            return shots = cams != null ? cams.GetComponentsInChildren<CinemachineCamera>(true) : System.Array.Empty<CinemachineCamera>();
        }

        CinemachineCamera Shot(string name) => Shots().FirstOrDefault(c => c.name == name);

        Transform CampSpot() => progress != null ? progress.Find("Camp") : null;

        UnitView campUnit;
        int campType = -1;

        // upgrade.md 후속: the barracks tab's unit (index into GameData.Types, -1 none) stands by the campfire, facing the camera (south).
        public void ShowUnit(int type)
        {
            if (type == campType) return;
            if (campUnit != null) BoardView.Kill(campUnit.gameObject);
            campUnit = null;
            campType = type;
            var camp = CampSpot();
            if (type < 0 || camp == null) return;
            var t = GameData.Types[type];
            campUnit = UnitView.Create(art, art.Look(t.Key), camp, -1, PointOr(camp, "Unit", camp.position), 180f - camp.eulerAngles.y, true);
        }

        void BuildProgress()
        {
            var lastOpen = 0;
            for (var i = 0; i < Campaign.Stages.Length; i++) if (campaign.StageOpen(i)) lastOpen = i;
            var roadNode = Paths("Road").FirstOrDefault();
            var road = roadNode != null ? PathPoints(roadNode) : System.Array.Empty<Vector2>();
            var open = RegionOf(lastOpen);
            var split = 0;
            if (open != null && road.Length > 0)
            {
                var o = Local(open.position);
                for (var i = 1; i < road.Length; i++) if ((road[i] - o).sqrMagnitude < (road[split] - o).sqrMagnitude) split = i;
            }
            var lit = road.Take(split + 1).ToArray();
            var dark = road.Skip(split).ToArray();
            if (lit.Length > 1) Ribbon(progress, "Road Lit", lit, CampaignMapLayout.RoadWidth, Tinted(CampaignMapLayout.RoadLit, "Road Lit"), 0.04f);
            if (dark.Length > 1) Ribbon(progress, "Road Dark", dark, CampaignMapLayout.RoadWidth, Tinted(CampaignMapLayout.RoadDark, "Road Dark"), 0.039f);
            for (var i = 0; i < Campaign.Stages.Length; i++)
            {
                var r = RegionOf(i);
                if (!campaign.IsCleared(Campaign.Stages[i]) || r == null || RegionStage(i) != i) continue;
                var at = PointOr(r, "Brazier", r.position);
                var brazier = art.MapProp(CampaignMapLayout.Brazier);
                if (brazier != null) BoardView.Place(brazier, progress, at, 0.9f, 0f);
                var fire = art.MapProp(CampaignMapLayout.Fire);
                if (fire != null) BoardView.Place(fire, progress, at + Vector3.up * 0.55f, 0f, 0f);
                BoardView.AddLight(progress, at + Vector3.up * 3f, LightType.Point, art.torchLight, 60f, 20f);
            }
            campUnit = null;
            campType = -1;
            BuildCamp();
        }

        // 3 water and 5-2 side roads are generated meshes, which a baked scene cannot keep, so they are built when the map shows.
        void BuildWater()
        {
            var water = new GameObject("Water").transform;
            water.SetParent(terrain, false);
            // Battle's water, darker and glossier so the north moon light glints on it (campaign-map.md 8).
            var mat = art.water != null ? new Material(art.water) { color = CampaignMapLayout.Water, name = "Map Water" } : Tinted(CampaignMapLayout.Water, "Water");
            mat.SetFloat("_Smoothness", 0.92f);
            foreach (var p in Paths("River")) Ribbon(water, "River", PathPoints(p), CampaignMapLayout.RiverWidth, mat, 0.02f);
            foreach (var p in Paths("Moat")) Ribbon(water, "Moat", PathPoints(p), CampaignMapLayout.MoatWidth, mat, 0.02f);
            var side = Tinted(CampaignMapLayout.SideRoad, "Side Road");
            foreach (var p in Paths("Side Road")) Ribbon(water, "Side Road", PathPoints(p), CampaignMapLayout.SideRoadWidth, side, 0.035f);
        }

        // 7: the camp sits at the last cleared region's camp spot (the start if none), turned toward the next region.
        void BuildCamp()
        {
            var last = -1;
            for (var i = 0; i < Campaign.Stages.Length; i++) if (campaign.IsCleared(Campaign.Stages[i])) last = i;
            var done = RegionOf(last);
            var start = terrain.Find(StartName);
            var at = done != null ? PointOr(done, "Camp", done.position) : start != null ? start.position : terrain.position;
            var next = RegionOf(last + 1);
            var face = next != null ? Local(next.position) - Local(at) : Vector2.up;
            var template = terrain.Find(CampTemplateName);
            var camp = template != null ? Instantiate(template, progress).transform : new GameObject().transform;
            camp.name = "Camp";
            camp.SetParent(progress, false);
            camp.position = at;
            camp.rotation = terrain.rotation * Quaternion.Euler(0f, Mathf.Atan2(face.x, face.y) * Mathf.Rad2Deg, 0f);
            camp.gameObject.SetActive(true);
        }

        // Catmull-Rom through the control points, one sample per unit (5-1).
        internal static Vector2[] Path(Vector2[] points)
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

        static void Ribbon(Transform parent, string name, Vector2[] controls, float width, Material material, float y)
        {
            if (controls.Length < 2) return;
            var path = Path(controls);
            var verts = new Vector3[path.Length * 2];
            var tris = new List<int>();
            var half = width * 0.5f;
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
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        Material Tinted(Color c, string name) => new Material(art.plain) { color = c, name = name };
    }
}
