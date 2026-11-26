using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VampireSpawn.Game;
using static VampireSpawn.Game.CampaignMapView;

namespace VampireSpawn.Editor
{
    // Bakes CampaignMapLayout (terrain and its Cinemachine shots, plus the brain on the view camera) into the Camp scene (docs/dev/scene-split.md 3). After that the scene is the map: people move props in Unity.
    // Baking again replaces the whole Campaign Map object, hand edits included.
    public static class CampaignMapBaker
    {
        public const string ScenePath = "Assets/_Project/Scenes/Camp.unity", MaterialFolder = "Assets/_Project/Art/Map";

        public static string Bake()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var map = Object.FindFirstObjectByType<CampaignMapView>();
            if (map == null) return "Camp 씬에 CampaignMapView 가 없다";
            if (map.terrain != null) Object.DestroyImmediate(map.terrain.gameObject);
            System.IO.Directory.CreateDirectory(MaterialFolder);
            map.terrain = Build(map.transform, map.art, Material(map.art));
            if (map.viewCamera != null) EditorUtility.SetDirty(UseBrain(map.viewCamera));
            EditorUtility.SetDirty(map);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "구움: " + map.terrain.childCount + " 묶음, " + map.terrain.GetComponentsInChildren<Renderer>().Length + " 렌더러";
        }

        public const string CastlePrefab = MaterialFolder + "/Castle.prefab", CastleName = "Castle Map";

        // campaign-map.md 12: the castle face is Castle Map beside Campaign Map, so baking the road never touches it.
        // Only what is missing is added; points and shots people moved stay put.
        public static string BakeCastle()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var map = Object.FindFirstObjectByType<CampaignMapView>();
            if (map == null) return "Camp 씬에 CampaignMapView 가 없다";
            var root = map.castle;
            if (root == null)
            {
                root = Point(map.transform, CastleName, Vector3.zero);
                root.position = CampaignMapLayout.CastleOrigin;
                map.castle = root;
            }
            if (root.Find("Castle") == null)
            {
                var castle = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CastlePrefab), root);
                castle.transform.localPosition = CampaignMapLayout.CastleAt;
                castle.transform.localRotation = Quaternion.Euler(0f, CampaignMapLayout.CastleYaw, 0f);
            }
            if (root.Find("Floor") == null)
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
                BoardView.Kill(floor.GetComponent<Collider>());
                floor.name = "Floor";
                floor.transform.SetParent(root, false);
                floor.transform.localPosition = Vector3.down * 0.02f;
                floor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                floor.transform.localScale = new Vector3(CampaignMapLayout.CastleFloor, CampaignMapLayout.CastleFloor, 1f);
                floor.GetComponent<MeshRenderer>().sharedMaterial = Material(map.art)(CampaignMapLayout.Floor, "Map Floor");
            }
            var cams = Point(root, "Cameras", Vector3.zero);
            foreach (var (stage, at) in CampaignMapLayout.CastleSpots)
            {
                var region = Point(root, "Region " + stage, at);
                Point(region, "Marker", Vector3.up * CampaignMapLayout.CastleMarkerLift);
                Point(region, "Brazier", Vector3.zero);
                if (cams.Find("Shot " + stage) == null) NewShot(cams, "Shot " + stage, region, CampaignMapLayout.CastleViewDistance);
            }
            if (cams.Find(CastleShot) == null)
            {
                var group = new GameObject("Castle Regions").AddComponent<CinemachineTargetGroup>();
                group.transform.SetParent(cams, false);
                foreach (var (stage, _) in CampaignMapLayout.CastleSpots) group.AddMember(root.Find("Region " + stage), 1f, 3f);
                var wide = NewShot(cams, CastleShot, group.transform, CampaignMapLayout.CastleWideDistance);
                var framing = wide.gameObject.AddComponent<CinemachineGroupFraming>();
                framing.SizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly;
                framing.FramingMode = CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
            }
            EditorUtility.SetDirty(map);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "성 면: 지역 " + CampaignMapLayout.CastleSpots.Count(s => root.Find("Region " + s.Stage) != null) + ", 시점 " + cams.childCount;
        }

        // One saved material per tint, reused on the next bake so the scene keeps pointing at the same assets.
        static System.Func<Color, string, Material> Material(ArtCatalog art) => (color, name) =>
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(art.plain); AssetDatabase.CreateAsset(m, path); }
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        };

        static Vector3 World(Vector2 local, float y = 0f) => CampaignMapLayout.Origin + new Vector3(local.x, y, local.y);

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

        // Builds the fixed terrain under parent; material makes the tinted materials.
        static Transform Build(Transform parent, ArtCatalog art, System.Func<Color, string, Material> material)
        {
            var terrain = new GameObject("Campaign Map").transform;
            terrain.SetParent(parent, false);
            terrain.position = CampaignMapLayout.Origin;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            BoardView.Kill(floor.GetComponent<Collider>());
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
                BoardView.Kill(ground.GetComponent<Collider>());
                ground.name = "Ground";
                ground.transform.SetParent(region, false);
                ground.transform.localPosition = Vector3.up * 0.01f;
                ground.transform.localScale = new Vector3(r.Radius * 2f, 0.005f, r.Radius * 2f);
                var theme = art.Theme(r.Stage);
                ground.GetComponent<MeshRenderer>().sharedMaterial = material((theme != null ? theme.ground : CampaignMapLayout.Floor) * 2.5f, "Region " + r.Stage);
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
        static void BuildPoints(Transform terrain, ArtCatalog art)
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

        static float Top(GameObject go) => BoardView.Drawn(go.transform, out var b) ? b.max.y : go.transform.position.y;

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
    }
}
