using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using VampireSpawn.Core;

// The map baker (Editor) builds with BoardView.Place and friends.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("VampireSpawn.Editor")]

namespace VampireSpawn.Game
{
    // docs/art/campaign-map.md: the landscape is the World scene loaded over Camp; regions, camp and shots are the Camp scene's World Map.
    // The barracks layer leaves the map for a small studio built here at run time (docs/ui/upgrade.md 9).
    public sealed class CampaignMapView : MonoBehaviour
    {
        public ArtCatalog art;
        public Camera viewCamera;
        [Tooltip("Camp 씬의 지도 층 (World Map: 지역·야영지·보조광·시점). 비면 지도가 뜨지 않는다")] public Transform worldMap;
        [Tooltip("지도 레이어 WASD 이동 속도 (초당 단위)")] public float panSpeed = 40f;
        [Tooltip("지도 레이어 WASD 이동 범위: 전경 시점에서 좌우(x)·앞뒤(y)로 갈 수 있는 거리")] public Vector2 panRange = new Vector2(25f, 25f);

        public const float BlendSeconds = 0.3f;

        // upgrade.md 9: the set stands far under the map. Places are from the stand's centre in hex sizes (x right, y up, z away from the camera).
        static readonly Vector3 StudioAt = new Vector3(0f, -2000f, 0f), KeyLightAt = new Vector3(-3.5f, 5f, -4f), RimLightAt = new Vector3(3f, 3.2f, 3.5f);
        static readonly Color StudioFloor = Screens.Ui.Rgb(0x1b1f2b), StudioStand = Screens.Ui.Rgb(0x343c4e), StudioDark = Screens.Ui.Rgb(0x0b0d14),
            StudioAmbient = Screens.Ui.Rgb(0x283250), KeyLight = Screens.Ui.Rgb(0xbfd4ee);
        // The camera looks 6 degrees down at 55% of a standard unit's height through a 30 degree lens, far enough that such a unit is 59% of the screen's height.
        // The unit turns 20 degrees toward the barracks panel (screen right).
        const float StandRadius = 1.1f, StandHeight = 0.12f, BackdropAt = 14f, StudioFov = 30f, StudioPitch = 6f, UnitOnScreen = 0.59f, LookHeight = 0.55f, UnitYaw = 160f;

        Transform progress;
        Light spot;
        Campaign campaign;

        public bool Showing => worldMap != null && worldMap.gameObject.activeSelf;

        public void Show(Campaign c)
        {
            campaign = c;
            if (worldMap == null) { Debug.LogError("Camp 씬에 지도 층이 없다 (도구 모음 > 지도 > 지도 층 굽기)"); return; }
            if (spot == null)
            {
                spot = BoardView.AddLight(transform, Vector3.zero, LightType.Spot, CampaignMapLayout.Moon, 3f, 30f);
                spot.spotAngle = 40f;
                spot.enabled = false;
            }
            worldMap.gameObject.SetActive(true);
            pan = Vector2.zero;
            var world = SceneManager.GetSceneByPath(CampaignMapLayout.WorldScene);
            // After a progress reset the world is already set up; collecting again would miss the renderers the cut has off.
            if (world.isLoaded) { if (cut == null) UseWorld(world); }
            else
            {
                SceneManager.sceneLoaded -= OnLoaded;
                SceneManager.sceneLoaded += OnLoaded;
                SceneManager.LoadScene(CampaignMapLayout.WorldScene, LoadSceneMode.Additive);
            }
            if (viewCamera != null) UseBrain(viewCamera);
            if (studio == null) BuildStudio();
            if (progress != null) BoardView.Kill(progress.gameObject);
            progress = new GameObject("Progress").transform;
            progress.SetParent(worldMap, false);
            for (var i = 0; i < Campaign.Stages.Length; i++)
            {
                var r = RegionOf(i);
                if (r == null || !campaign.IsCleared(Campaign.Stages[i])) continue;
                var at = PointOr(r, "Brazier", r.position);
                if (art.brazier != null) BoardView.Place(art.brazier, progress, at, 0.9f, 0f);
                if (art.fire != null) BoardView.Place(art.fire, progress, at + Vector3.up * 0.55f, 0f, 0f);
                BoardView.AddLight(progress, at + Vector3.up * 3f, LightType.Point, art.torchLight, 60f, 20f);
            }
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnLoaded;

        void OnLoaded(Scene s, LoadSceneMode _)
        {
            if (s.path != CampaignMapLayout.WorldScene) return;
            SceneManager.sceneLoaded -= OnLoaded;
            UseWorld(s);
        }

        // Sky, fog and ambient light come from the active scene only; the Camp camera draws, so the world's own camera and listener go.
        void UseWorld(Scene s)
        {
            SceneManager.SetActiveScene(s);
            RenderSettings.fogEndDistance = CampaignMapLayout.FogEnd;
            var roots = s.GetRootGameObjects();
            foreach (var cam in roots.SelectMany(r => r.GetComponentsInChildren<Camera>())) cam.enabled = false;
            foreach (var ear in roots.SelectMany(r => r.GetComponentsInChildren<AudioListener>())) ear.enabled = false;
            cut = new List<Renderer>();
            var cathedral = roots.FirstOrDefault(r => r.name == "Cathedral");
            if (cathedral != null)
                foreach (Transform group in cathedral.transform)
                {
                    var byLowest = CampaignMapLayout.CutByLowestPoint.Contains(group.name);
                    cut.AddRange(group.GetComponentsInChildren<Renderer>().Where(r => r.enabled && (byLowest ? r.bounds.min.y : r.bounds.center.y) > CampaignMapLayout.CutHeight));
                }
            cutApplied = null;
            ApplyCut();
            // The barracks can open before this scene is in: its air is the one to put back, and its sun lights the ones to turn off.
            if (inStudio) { inStudio = false; Studio(true); }
        }

        Transform studio;
        CinemachineCamera studioShot;
        float studioDistance;

        void BuildStudio()
        {
            var s = art.hexSize;
            studio = new GameObject("Studio").transform;
            studio.SetParent(transform, false);
            studio.position = worldMap.position + StudioAt;
            Slab("Floor", PrimitiveType.Cube, new Vector3(0f, -0.05f, 0f) * s, new Vector3(40f, 0.1f, 40f) * s, StudioFloor);
            Slab("Backdrop", PrimitiveType.Cube, new Vector3(0f, 12f, BackdropAt) * s, new Vector3(60f, 24f, 0.1f) * s, StudioFloor);
            // Unity's cylinder is 2 high and 1 across.
            Slab("Stand", PrimitiveType.Cylinder, Vector3.up * StandHeight * 0.5f * s, new Vector3(StandRadius * 2f, StandHeight * 0.5f, StandRadius * 2f) * s, StudioStand);
            var chest = studio.position + Vector3.up * (StandHeight + LookHeight * art.unitHeight) * s;
            StudioLight(KeyLightAt * s, KeyLight, 70f, 16f * s, 55f, chest);
            StudioLight(RimLightAt * s, art.torchLight, 70f, 12f * s, 60f, chest);
            var shot = new GameObject("Shot Studio").transform;
            shot.SetParent(studio, false);
            shot.rotation = Quaternion.Euler(StudioPitch, 0f, 0f);
            shot.position = chest - shot.forward * (art.unitHeight * s / UnitOnScreen / (2f * Mathf.Tan(StudioFov * 0.5f * Mathf.Deg2Rad)));
            studioShot = shot.gameObject.AddComponent<CinemachineCamera>();
            studioShot.Lens.FieldOfView = StudioFov;
            studioShot.enabled = false;
            studioDistance = (chest - shot.position).magnitude;
        }

        void Slab(string name, PrimitiveType shape, Vector3 at, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(studio, false);
            go.transform.localPosition = at;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = new Material(art.plain) { color = color, name = "Studio " + name };
        }

        void StudioLight(Vector3 at, Color color, float intensity, float range, float angle, Vector3 aim)
        {
            var l = BoardView.AddLight(studio, studio.position + at, LightType.Spot, color, intensity, range);
            l.spotAngle = angle;
            l.shadows = LightShadows.Soft;
            l.transform.rotation = Quaternion.LookRotation(aim - l.transform.position);
        }

        // What the active scene's RenderSettings held outside the studio.
        struct Air
        {
            bool fog;
            FogMode mode;
            Color fogColor, light, sky, equator, ground;
            float start, end, density, intensity;
            UnityEngine.Rendering.AmbientMode ambient;
            UnityEngine.Rendering.SphericalHarmonicsL2 probe;

            public static Air Now() => new Air
            {
                fog = RenderSettings.fog, mode = RenderSettings.fogMode, fogColor = RenderSettings.fogColor, start = RenderSettings.fogStartDistance,
                end = RenderSettings.fogEndDistance, density = RenderSettings.fogDensity, ambient = RenderSettings.ambientMode, light = RenderSettings.ambientLight,
                sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor,
                intensity = RenderSettings.ambientIntensity, probe = RenderSettings.ambientProbe,
            };

            public void Restore()
            {
                RenderSettings.fog = fog;
                RenderSettings.fogMode = mode;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogStartDistance = start;
                RenderSettings.fogEndDistance = end;
                RenderSettings.fogDensity = density;
                RenderSettings.ambientMode = ambient;
                RenderSettings.ambientLight = light;
                RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = equator;
                RenderSettings.ambientGroundColor = ground;
                RenderSettings.ambientIntensity = intensity;
                RenderSettings.ambientProbe = probe;
            }
        }

        bool inStudio;
        Air outside;
        Light[] suns = System.Array.Empty<Light>();

        // upgrade.md 9-7: in the studio only its two spots light the unit. The world's sun lights go off, the ambient light drops to a dark flat blue,
        // and fog the colour of the dark swallows the far floor and the backdrop so no horizon shows behind the unit. Leaving puts the world's air back.
        void Studio(bool on)
        {
            if (on == inStudio) return;
            inStudio = on;
            if (!on)
            {
                foreach (var l in suns) if (l != null) l.enabled = true;
                outside.Restore();
                return;
            }
            outside = Air.Now();
            suns = FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional && l.enabled).ToArray();
            foreach (var l in suns) l.enabled = false;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = StudioDark;
            RenderSettings.fogStartDistance = studioDistance + StandRadius * art.hexSize;
            RenderSettings.fogEndDistance = studioDistance + (BackdropAt - 2f) * art.hexSize;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = StudioAmbient;
        }

        List<Renderer> cut;
        bool cutWanted;
        bool? cutApplied;

        // campaign-map.md 3: the castle face shows the cathedral's ground floor; renderers go off at once, so their shadows go too.
        void ApplyCut()
        {
            if (cut == null || cutApplied == cutWanted) return;
            cutApplied = cutWanted;
            foreach (var r in cut) if (r != null) r.enabled = !cutWanted;
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

        // empty and margin are normalized viewport units (stage-select.md 2-3). Only the chosen shot is enabled; a lens shift slides
        // its subject to the empty area's centre, because the composer's screen offset is angular and lands about 35% short sideways.
        public void Look(CampLayer layer, int stage, Rect empty, Vector2 margin, bool snap = false)
        {
            var r = RegionOf(stage);
            var face = CampNav.FaceOf(stage);
            var barracks = layer == CampLayer.Barracks;
            cutWanted = !barracks && face == CampFace.Castle;
            ApplyCut();
            var chosen = barracks ? studioShot : layer == CampLayer.Info && r != null ? Shot("Shot " + Campaign.Stages[stage].Id)
                : Shot(face == CampFace.Castle ? "Shot Castle" : "Shot Road");
            foreach (var cam in Shots()) cam.enabled = cam == chosen;
            if (studioShot != null) studioShot.enabled = barracks;
            Studio(barracks);
            this.empty = empty;
            if (layer == CampLayer.Map && chosen != mapShot) { pan = Vector2.zero; mapShot = chosen; }
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

        // Sensor at the screen's aspect with a vertical gate, so the vertical FOV holds and a shift of 1 is one screen width.
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

        void LateUpdate()
        {
            if (!Showing || viewCamera == null) return;
            foreach (var cam in Shots().Append(studioShot))
                if (cam != null && cam.enabled && !Mathf.Approximately(cam.Lens.PhysicalProperties.SensorSize.x, 24f * viewCamera.aspect)) Shift(cam);
        }

        CinemachineCamera panning, mapShot;
        Vector2 pan;

        // 5-3: right is the camera's right, forward is the camera's forward laid flat; the offset is camera-local, tilted by the shot's own pitch.
        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (!Showing || panning == null || kb == null) return;
            var move = new Vector2((kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f), (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f));
            pan += move.normalized * panSpeed * Time.unscaledDeltaTime;
            pan = new Vector2(Mathf.Clamp(pan.x, -panRange.x, panRange.x), Mathf.Clamp(pan.y, -panRange.y, panRange.y));
            if (!panning.TryGetComponent<CinemachineCameraOffset>(out var offset)) offset = panning.gameObject.AddComponent<CinemachineCameraOffset>();
            var rad = PitchOf(panning) * Mathf.Deg2Rad;
            offset.Offset = new Vector3(pan.x, pan.y * Mathf.Sin(rad), pan.y * Mathf.Cos(rad));
        }

        static float PitchOf(CinemachineCamera cam)
        {
            if (!cam.TryGetComponent<CinemachineFollow>(out var follow)) return cam.transform.eulerAngles.x;
            var o = follow.FollowOffset;
            return Mathf.Atan2(o.y, new Vector2(o.x, o.z).magnitude) * Mathf.Rad2Deg;
        }

        Transform RegionOf(int stageIndex) =>
            worldMap == null || stageIndex < 0 || stageIndex >= Campaign.Stages.Length ? null : worldMap.Find("Region " + Campaign.Stages[stageIndex].Id);

        static Vector3 PointOr(Transform parent, string name, Vector3 fallback)
        {
            var t = parent.Find(name);
            return t != null ? t.position : fallback;
        }

        CinemachineCamera[] shots;

        CinemachineCamera[] Shots()
        {
            if (shots != null) return shots;
            var cams = worldMap != null ? worldMap.Find("Cameras") : null;
            return shots = cams != null ? cams.GetComponentsInChildren<CinemachineCamera>(true) : System.Array.Empty<CinemachineCamera>();
        }

        CinemachineCamera Shot(string name) => Shots().FirstOrDefault(c => c.name == name);

        UnitView campUnit;
        int campType = -1;

        // upgrade.md 9-3: the barracks tab's unit (index into GameData.Types, -1 none) stands on the studio's stand.
        public void ShowUnit(int type)
        {
            if (type == campType) return;
            if (campUnit != null) BoardView.Kill(campUnit.gameObject);
            campUnit = null;
            campType = type;
            if (type < 0 || studio == null) return;
            campUnit = UnitView.Create(art, art.Look(GameData.Types[type].Key), studio, -1, studio.position + Vector3.up * StandHeight * art.hexSize, UnitYaw, true);
        }

        // upgrade.md 9-6: dragging turns the unit where it stands; a new unit starts from UnitYaw again.
        public void TurnUnit(float degrees)
        {
            if (campUnit != null) campUnit.transform.Rotate(0f, degrees, 0f);
        }
    }
}
