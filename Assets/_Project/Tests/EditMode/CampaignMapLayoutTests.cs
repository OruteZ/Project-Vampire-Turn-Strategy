using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // docs/art/campaign-map.md 4, 8-4.
    public class CampaignMapLayoutTests
    {
        [Test]
        public void EveryCampaignStageHasOneSpotInStageOrder()
        {
            Assert.That(CampaignMapLayout.Spots.Select(s => s.Stage), Is.EqualTo(Campaign.Stages.Select(s => s.Id)));
        }
    }
}
