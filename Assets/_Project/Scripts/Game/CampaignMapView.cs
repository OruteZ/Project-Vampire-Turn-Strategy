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
    public sealed class CampaignMapView : MonoBehaviour
    {
        public ArtCatalog art;
        public Camera viewCamera;
        [Tooltip("Camp 씬의 지도 층 (World Map: 지역·야영지·보조광·시점). 비면 지도가 뜨지 않는다")] public Transform worldMap;
        [Tooltip("지도 레이어 WASD 이동 속도 (초당 단위)")] public float panSpeed = 40f;
        [Tooltip("지도 레이어 WASD 이동 범위: 전경 시점에서 좌우(x)·앞뒤(y)로 갈 수 있는 거리")] public Vector2 panRange = new Vector2(25f, 25f);

        public const float BlendSeconds = 0.3f;

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
            cutWanted = layer != CampLayer.Barracks && face == CampFace.Castle;
            ApplyCut();
            var chosen = layer == CampLayer.Barracks ? Shot("Shot Camp") : layer == CampLayer.Info && r != null ? Shot("Shot " + Campaign.Stages[stage].Id)
                : Shot(face == CampFace.Castle ? "Shot Castle" : "Shot Road");
            foreach (var cam in Shots()) cam.enabled = cam == chosen;
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
            foreach (var cam in Shots())
                if (cam.enabled && !Mathf.Approximately(cam.Lens.PhysicalProperties.SensorSize.x, 24f * viewCamera.aspect)) Shift(cam);
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

        // The barracks tab's unit (index into GameData.Types, -1 none) stands at Camp/Unit facing Shot Camp.
        public void ShowUnit(int type)
        {
            if (type == campType) return;
            if (campUnit != null) BoardView.Kill(campUnit.gameObject);
            campUnit = null;
            campType = type;
            var camp = worldMap != null ? worldMap.Find("Camp") : null;
            if (type < 0 || camp == null) return;
            var shot = Shot("Shot Camp");
            var toCamera = shot != null && shot.TryGetComponent<CinemachineFollow>(out var follow) ? follow.FollowOffset : Vector3.back;
            var yaw = Mathf.Atan2(toCamera.x, toCamera.z) * Mathf.Rad2Deg - camp.eulerAngles.y;
            campUnit = UnitView.Create(art, art.Look(GameData.Types[type].Key), camp, -1, PointOr(camp, "Unit", camp.position), yaw, true);
        }
    }
}
