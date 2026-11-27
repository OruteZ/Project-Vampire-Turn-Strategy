using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Editor
{
    // docs/art/campaign-map.md 8: bakes CampaignMapLayout into the Camp scene's World Map. Only what is missing is added,
    // so points and shots people moved stay; delete World Map to start over.
    public static class CampaignMapBaker
    {
        public const string ScenePath = "Assets/_Project/Scenes/Camp.unity", RootName = "World Map";

        public static string Bake()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CampaignMapLayout.WorldScene) == null
                && !AssetDatabase.CopyAsset(CampaignMapLayout.WorldSource, CampaignMapLayout.WorldScene))
                return "세계 씬 복사 실패: " + CampaignMapLayout.WorldSource;
            if (EditorBuildSettings.scenes.All(s => s.path != CampaignMapLayout.WorldScene))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(CampaignMapLayout.WorldScene, true)).ToArray();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var map = Object.FindFirstObjectByType<CampaignMapView>();
            if (map == null) return "Camp 씬에 CampaignMapView 가 없다";

            var removed = 0;
            foreach (var old in new[] { "Campaign Map", "Castle Map" }.Select(n => map.transform.Find(n)).Where(t => t != null))
            {
                Object.DestroyImmediate(old.gameObject);
                removed++;
            }
            foreach (var light in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Light>(true))
                         .Where(l => l.type == LightType.Directional && !l.transform.IsChildOf(map.transform)).ToArray())
            {
                Object.DestroyImmediate(light.gameObject);
                removed++;
            }

            var root = map.transform.Find(RootName);
            if (root == null)
            {
                root = Point(map.transform, RootName, Vector3.zero);
                root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            map.worldMap = root;
            var cams = Point(root, "Cameras", Vector3.zero);

            var spots = CampaignMapLayout.Spots;
            for (var i = 0; i < spots.Length; i++)
            {
                var region = Point(root, "Region " + spots[i].Stage, spots[i].At);
                Point(region, "Marker", CampaignMapLayout.MarkerLift);
                Point(region, "Brazier", Vector3.zero);
                var view = Point(region, "View", CampaignMapLayout.ViewLift);
                if (cams.Find("Shot " + spots[i].Stage) == null) NewShot(cams, "Shot " + spots[i].Stage, view, CampaignMapLayout.Stage(CampNav.FaceOf(i)));
            }
            Wide(root, cams, "Shot Road", "Road Regions", CampFace.Road);
            Wide(root, cams, "Shot Castle", "Castle Regions", CampFace.Castle);

            var camp = root.Find("Camp");
            if (camp == null)
            {
                camp = Point(root, "Camp", CampaignMapLayout.CampAt);
                if (map.art.brazier != null) BoardView.Place(map.art.brazier, camp, camp.position, 0.9f, 0f);
                if (map.art.fire != null) BoardView.Place(map.art.fire, camp, camp.position + Vector3.up * 0.55f, 0f, 0f);
                BoardView.AddLight(camp, camp.position + Vector3.up * 1.2f, LightType.Point, map.art.torchLight, 2f, 8f);
            }
            Point(camp, "Unit", CampaignMapLayout.CampUnit);
            if (cams.Find("Shot Camp") == null) NewShot(cams, "Shot Camp", camp, CampaignMapLayout.Camp);

            if (root.Find("Fill Light") == null)
            {
                var fill = new GameObject("Fill Light").AddComponent<Light>();
                fill.transform.SetParent(root, false);
                fill.transform.rotation = Quaternion.Euler(CampaignMapLayout.FillAngles);
                fill.type = LightType.Directional;
                fill.color = CampaignMapLayout.FillColor;
                fill.intensity = CampaignMapLayout.FillIntensity;
                fill.shadows = LightShadows.None;
            }

            if (map.viewCamera != null)
            {
                // The sky is the World scene's skybox; a solid clear would paint over it.
                map.viewCamera.clearFlags = CameraClearFlags.Skybox;
                EditorUtility.SetDirty(map.viewCamera);
                EditorUtility.SetDirty(CampaignMapView.UseBrain(map.viewCamera));
            }
            EditorUtility.SetDirty(map);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "지도 층: 지역 " + spots.Count(s => root.Find("Region " + s.Stage) != null) + "/" + spots.Length
                + ", 시점 " + cams.GetComponentsInChildren<CinemachineCamera>(true).Length + ", 옛 지도·빛 지움 " + removed;
        }

        // 5-2: a wide shot frames its face's regions as a group, adjusting only distance.
        static void Wide(Transform root, Transform cams, string name, string groupName, CampFace face)
        {
            if (cams.Find(name) != null) return;
            var group = new GameObject(groupName).AddComponent<CinemachineTargetGroup>();
            group.transform.SetParent(cams, false);
            for (var i = 0; i < CampaignMapLayout.Spots.Length; i++)
                if (CampNav.FaceOf(i) == face) group.AddMember(root.Find("Region " + CampaignMapLayout.Spots[i].Stage), 1f, 3f);
            var wide = NewShot(cams, name, group.transform, CampaignMapLayout.Wide(face));
            var framing = wide.gameObject.AddComponent<CinemachineGroupFraming>();
            framing.SizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly;
            framing.FramingMode = CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
        }

        // Follows its subject at a world-space offset from the shot's pitch, yaw and distance; the composer aims it.
        static CinemachineCamera NewShot(Transform parent, string name, Transform subject, MapShot shot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var cam = go.AddComponent<CinemachineCamera>();
            cam.Lens.FieldOfView = shot.Fov;
            cam.Lens.PhysicalProperties.GateFit = Camera.GateFitMode.Vertical;
            cam.Lens.FarClipPlane = 1000f;
            cam.Follow = cam.LookAt = subject;
            var follow = go.AddComponent<CinemachineFollow>();
            follow.FollowOffset = shot.Offset;
            follow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping = Vector3.zero;
            var composer = go.AddComponent<CinemachineRotationComposer>();
            composer.Damping = Vector2.zero;
            cam.enabled = false;
            return cam;
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
    }
}
