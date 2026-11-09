using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // docs/art/campaign-map.md. The fixed terrain and its Cinemachine shots are baked into the Camp scene
    // (Tools > 지도 > Camp 씬에 지도 굽기) so people can move props, points and shots in Unity; the scene is the map's only truth at run time.
    // Show rebuilds only progress (lit road, braziers, camp) from the baked points; Look picks a shot and the brain blends to it.
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
            fogDistance = 0f;
            if (progress != null) Kill(progress.gameObject);
            progress = new GameObject("Progress").transform;
            progress.SetParent(terrain, false);
            BuildProgress();
            if (terrain.Find("Water") == null) BuildWater();
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
            var chosen = layer == CampLayer.Barracks ? Shot("Shot Camp") : layer == CampLayer.Info && r != null ? Shot("Shot " + Campaign.Stages[stage].Id) : Shot("Shot Wide");
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

        // The running map reads only the scene: regions, their Marker/Brazier/Camp points, Paths and the Camp Template are baked
        // objects people may move by hand. CampaignMapLayout only feeds the bake (campaign-map.md 10).
        public const string PathsName = "Paths", StartName = "Start", CampTemplateName = "Camp Template";

        Transform RegionOf(int stageIndex) =>
            terrain != null && stageIndex >= 0 && stageIndex < Campaign.Stages.Length ? terrain.Find("Region " + Campaign.Stages[stageIndex].Id) : null;

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
            if (campUnit != null) Kill(campUnit.gameObject);
            campUnit = null;
            campType = type;
            var camp = CampSpot();
            if (type < 0 || camp == null) return;
            var t = GameData.Types[type];
            campUnit = UnitView.Create(art, art.Look(t.Key), camp, -1, PointOr(camp, "Unit", camp.position), 180f - camp.eulerAngles.y, art.TierRing(t.Tier), true);
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

            var roads = Roads();
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
                GameObject last = null;
                foreach (var p in r.Props)
                {
                    var prefab = Prefab(art, p.Prefab);
                    if (prefab == null) continue;
                    var y = p.OnTop && last != null ? Top(last) - CampaignMapLayout.Origin.y : p.Lift;
                    last = BoardView.Place(prefab, region, World(r.Anchor + new Vector2(p.Dx, p.Dz), y), p.Width, p.Yaw, p.MaxHeight > 0f ? p.MaxHeight : 99f);
                }
                if (r.Fill != null) BuildFill(region, art, r, roads);
                foreach (var l in r.Lights)
                    BoardView.AddLight(region, World(r.Anchor + new Vector2(l.At.x, l.At.z), l.At.y), LightType.Point, l.Blood ? CampaignMapLayout.Blood : art.torchLight, l.Intensity, l.Range);
            }
            var lineProps = BuildLines(terrain, art, roads);
            BuildForest(terrain, art, lineProps);
            BuildShots(terrain);
            BuildPoints(terrain, art);
            return terrain;
        }

        // Adds the points the running map reads; ones already in the scene stay where people put them.
        public static void BuildPoints(Transform terrain, ArtCatalog art)
        {
            foreach (var r in CampaignMapLayout.Regions)
            {
                var region = terrain.Find("Region " + r.Stage);
                if (region == null) continue;
                Point(region, "Marker", Vector3.up * r.MarkerHeight);
                Point(region, "Brazier", new Vector3(r.Brazier.x, 0f, r.Brazier.y));
                Point(region, "Camp", new Vector3(r.Camp.x - r.Anchor.x, 0f, r.Camp.y - r.Anchor.y));
            }
            Point(terrain, StartName, new Vector3(CampaignMapLayout.Start.x, 0f, CampaignMapLayout.Start.y));
            var paths = Point(terrain, PathsName, Vector3.zero);
            PathNode(paths, "Road", CampaignMapLayout.Road);
            PathNode(paths, "River", CampaignMapLayout.River);
            PathNode(paths, "Moat", CampaignMapLayout.Moat);
            for (var i = 0; i < CampaignMapLayout.SideRoads.Length; i++) PathNode(paths, "Side Road " + i, CampaignMapLayout.SideRoads[i]);
            if (terrain.Find(CampTemplateName) != null) return;
            var camp = Point(terrain, CampTemplateName, Vector3.zero);
            foreach (var p in CampaignMapLayout.CampProps)
            {
                var prefab = Prefab(art, p.Prefab);
                if (prefab != null) BoardView.Place(prefab, camp, camp.TransformPoint(new Vector3(p.Dx, p.Lift, p.Dz)), p.Width, camp.eulerAngles.y + p.Yaw);
            }
            var l = CampaignMapLayout.CampLight;
            BoardView.AddLight(camp, camp.TransformPoint(l.At), LightType.Point, art.torchLight, l.Intensity, l.Range);
            Point(camp, "Unit", CampaignMapLayout.CampUnit);
            camp.gameObject.SetActive(false);
        }

        static Transform Point(Transform parent, string name, Vector3 local)
        {
            var t = parent.Find(name);
            if (t != null) return t;
            t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = local;
            return t;
        }

        static void PathNode(Transform paths, string name, Vector2[] points)
        {
            if (paths.Find(name) != null) return;
            var path = Point(paths, name, Vector3.zero);
            for (var i = 0; i < points.Length; i++) Point(path, i.ToString(), new Vector3(points[i].x, 0f, points[i].y));
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
                if (!campaign.IsCleared(Campaign.Stages[i]) || r == null) continue;
                var at = PointOr(r, "Brazier", r.position);
                var brazier = art.MapProp(CampaignMapLayout.Brazier);
                if (brazier != null) BoardView.Place(brazier, progress, at, 0.9f, 0f);
                var fire = art.MapProp(CampaignMapLayout.Fire);
                if (fire != null) BoardView.Place(fire, progress, at + Vector3.up * 0.55f, 0f, 0f);
                BoardView.AddLight(progress, at + Vector3.up * 1.5f, LightType.Point, art.torchLight, 2.2f, 10f);
            }
            campUnit = null;
            campType = -1;
            BuildCamp();
        }

        // 3-2: one prop in 45% of 6-unit cells, kept clear of the road, regions, camp spots, water, line props and the camera edge.
        static void BuildForest(Transform terrain, ArtCatalog art, List<Vector2> lineProps)
        {
            var water = Water();
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
                if (Near(water, p, CampaignMapLayout.ClearOfWater) || Near(lineProps, p, CampaignMapLayout.ClearOfLine)) continue;
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

        static GameObject Prefab(ArtCatalog art, string name)
        {
            var prefab = art.MapProp(name);
            if (prefab == null) Debug.LogWarning("지도 프리팹 없음: " + name + " (도구 모음 > 지도 > 프리팹 목록 채우기)");
            return prefab;
        }

        static float Top(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return go.transform.position.y;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b.max.y;
        }

        static bool Near(IEnumerable<Vector2> points, Vector2 p, float d) => points.Any(q => (q - p).sqrMagnitude < d * d);

        static Vector2[] Roads() => Path(CampaignMapLayout.Road).Concat(CampaignMapLayout.SideRoads.SelectMany(Path)).ToArray();

        static Vector2[] Water() => Path(CampaignMapLayout.River).Concat(Path(CampaignMapLayout.Moat)).ToArray();

        // 4-2 채움: hash-picked spots in the ring; a spot too near the road, water or another prop is skipped for the next one.
        static void BuildFill(Transform region, ArtCatalog art, MapRegion r, Vector2[] roads)
        {
            var f = r.Fill;
            var water = Water();
            var taken = r.Props.Select(p => r.Anchor + new Vector2(p.Dx, p.Dz)).ToList();
            var seed = int.Parse(r.Stage.Substring(1));
            for (int tries = 0, placed = 0; placed < f.Count && tries < f.Count * 8; tries++)
            {
                var h = BoardView.Hash(seed * 100, tries + 1);
                var angle = h % 360 * Mathf.Deg2Rad;
                var radius = Mathf.Lerp(f.R1, f.R2, h / 360 % 100 / 99f);
                var d = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
                var p = r.Anchor + d;
                if (d.y >= f.SouthOf || Near(roads, p, CampaignMapLayout.FillClearOfRoad) || Near(water, p, CampaignMapLayout.FillClearOfWater)
                    || Near(taken, p, CampaignMapLayout.FillClearOfProp)) continue;
                var (name, width) = f.Names[h / 36000 % f.Names.Length];
                var prefab = Prefab(art, name);
                if (prefab == null) continue;
                BoardView.Place(prefab, region, World(p), width, h / 7 % 360);
                taken.Add(p);
                placed++;
            }
        }

        // 3-1: props every spacing along each line, skipping the road; walls stand along the line and stretch to join up.
        static List<Vector2> BuildLines(Transform terrain, ArtCatalog art, Vector2[] roads)
        {
            var root = new GameObject("Lines").transform;
            root.SetParent(terrain, false);
            var placed = new List<Vector2>();
            foreach (var l in CampaignMapLayout.Lines)
            {
                var line = new GameObject(l.Name).transform;
                line.SetParent(root, false);
                var span = l.To - l.From;
                var count = Mathf.FloorToInt(span.magnitude / l.Spacing) + 1;
                var along = -Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg;
                for (var i = 0; i < count; i++)
                {
                    var p = count == 1 ? l.From : l.From + span.normalized * (l.Spacing * i);
                    if (Near(roads, p, CampaignMapLayout.LineClearOfRoad + l.Width * 0.5f)) continue;
                    var prefab = Prefab(art, l.Prefabs[i % l.Prefabs.Length]);
                    if (prefab == null) continue;
                    var yaw = l.Wall ? along + i % 2 * 180f : BoardView.Hash(Mathf.RoundToInt(p.x) + 1000, Mathf.RoundToInt(p.y) + 1000) % 360;
                    BoardView.Place(prefab, line, World(p), l.Width, yaw, l.MaxHeight, l.Wall);
                    placed.Add(p);
                }
            }
            return placed;
        }

        // 3 water and 5-2 side roads are generated meshes, which a baked scene cannot keep, so they are built when the map shows.
        void BuildWater()
        {
            var water = new GameObject("Water").transform;
            water.SetParent(terrain, false);
            var mat = art.water != null ? art.water : Tinted(CampaignMapLayout.Fog, "Water");
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

        static void Kill(Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
    }
}
