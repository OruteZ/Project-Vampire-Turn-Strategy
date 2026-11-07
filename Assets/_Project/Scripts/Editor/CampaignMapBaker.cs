using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VampireSpawn.Game;

namespace VampireSpawn.Editor
{
    // Bakes CampaignMapLayout into the Camp scene (docs/dev/scene-split.md 3). After that the scene is the map: people move props in Unity.
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
            Directory.CreateDirectory(MaterialFolder);
            map.terrain = CampaignMapView.Build(map.transform, map.art, Material(map.art));
            EditorUtility.SetDirty(map);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "구움: " + map.terrain.childCount + " 묶음, " + map.terrain.GetComponentsInChildren<Renderer>().Length + " 렌더러";
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
    }
}
