using System.Linq;
using NUnit.Framework;
using UnityEditor;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // docs/art/campaign-map.md 4-1, 10; stage-select-handoff.md 3.
    public class CampaignMapLayoutTests
    {
        static ArtCatalog Art => AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/_Project/Art/ArtCatalog.asset");

        [Test]
        public void EveryCampaignStageHasARegion()
        {
            var missing = Campaign.Stages.Where(s => CampaignMapLayout.Region(s.Id) == null).Select(s => s.Id).ToArray();
            Assert.That(missing, Is.Empty, "stages with no region on the map");
        }

        [Test]
        public void MapPropsHoldExactlyTheLayoutsPrefabs()
        {
            var have = (Art.mapProps ?? new UnityEngine.GameObject[0]).Where(p => p != null).Select(p => p.name).OrderBy(n => n).ToArray();
            var want = CampaignMapLayout.Names.ToArray();
            Assert.That(want.Except(have), Is.Empty, "names the layout uses but ArtCatalog.mapProps lacks (Tools > 지도 > 프리팹 목록 채우기)");
            Assert.That(have.Except(want), Is.Empty, "prefabs in ArtCatalog.mapProps the layout no longer uses");
        }

        [Test]
        public void TheRoadPassesThroughEveryRegionAnchor()
        {
            foreach (var r in CampaignMapLayout.Regions)
                Assert.That(CampaignMapLayout.Road.Any(p => (p - r.Anchor).sqrMagnitude < 0.01f), r.Stage + " anchor on the road");
        }
    }
}
