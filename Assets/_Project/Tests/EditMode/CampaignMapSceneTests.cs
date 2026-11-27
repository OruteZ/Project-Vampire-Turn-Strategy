using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // campaign-map.md 10: the running map reads only the baked Camp scene, so every point it reads must be there.
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
                var terrain = map.terrain;
                Assert.That(terrain, Is.Not.Null, "baked terrain");
                Assert.That(map.castle, Is.Not.Null, "castle face");
                // stage-select.md 5-4: every stage has its own region, marker and shot; none share.
                var shots = map.GetComponentsInChildren<Transform>(true).Where(t => t.parent != null && t.parent.name == "Cameras").Select(t => t.name).ToList();
                foreach (var s in Campaign.Stages)
                {
                    var road = CampaignMapLayout.Region(s.Id) != null;
                    var region = (road ? terrain : map.castle).Find("Region " + s.Id);
                    Assert.That(region, Is.Not.Null, "Region " + s.Id);
                    foreach (var point in road ? new[] { "Marker", "Brazier", "Camp" } : new[] { "Marker", "Brazier" })
                        Assert.That(region.Find(point), Is.Not.Null, s.Id + " " + point);
                    Assert.AreEqual(1, shots.Count(n => n == "Shot " + s.Id), "Shot " + s.Id);
                }
                Assert.AreEqual(1, shots.Count(n => n == "Shot Castle"), "Shot Castle");
                Assert.That(terrain.Find(CampaignMapView.StartName), Is.Not.Null, "Start");
                foreach (var path in new[] { "Road", "River", "Moat", "Side Road 0" })
                    Assert.That(terrain.Find(CampaignMapView.PathsName + "/" + path)?.childCount ?? 0, Is.GreaterThanOrEqualTo(2), path + " points");
                var camp = terrain.Find(CampaignMapView.CampTemplateName);
                Assert.That(camp, Is.Not.Null, "Camp Template");
                Assert.That(camp.gameObject.activeSelf, Is.False, "Camp Template stays hidden");
                Assert.That(camp.Find("Unit"), Is.Not.Null, "Camp Template Unit");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
