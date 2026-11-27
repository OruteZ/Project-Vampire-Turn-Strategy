using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // campaign-map.md 8-4: the running map reads only the Camp scene's World Map, so every point and shot it reads must be there.
    public class CampaignMapSceneTests
    {
        [Test]
        public void TheCampSceneHoldsEveryPointTheMapReads()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Camp.unity", OpenSceneMode.Additive);
            try
            {
                var map = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CampaignMapView>(true)).FirstOrDefault();
                Assert.That(map, Is.Not.Null, "CampaignMapView in Camp.unity");
                var root = map.worldMap;
                Assert.That(root, Is.Not.Null, "World Map");
                var shots = root.Find("Cameras").Cast<Transform>().Select(c => c.name).ToList();
                foreach (var s in Campaign.Stages)
                {
                    var region = root.Find("Region " + s.Id);
                    Assert.That(region, Is.Not.Null, "Region " + s.Id);
                    foreach (var point in new[] { "Marker", "Brazier", "View" })
                        Assert.That(region.Find(point), Is.Not.Null, s.Id + " " + point);
                    Assert.AreEqual(1, shots.Count(n => n == "Shot " + s.Id), "Shot " + s.Id);
                }
                foreach (var wide in new[] { "Shot Road", "Shot Castle", "Shot Camp" }) Assert.AreEqual(1, shots.Count(n => n == wide), wide);
                Assert.That(root.Find("Camp/Unit"), Is.Not.Null, "Camp/Unit");
                Assert.That(root.Find("Fill Light")?.GetComponent<Light>(), Is.Not.Null, "Fill Light");
                var directional = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).Where(l => l.type == LightType.Directional);
                Assert.That(directional.Select(l => l.name), Is.EquivalentTo(new[] { "Fill Light" }), "the World scene lights the map; Camp keeps only the fill (7)");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
