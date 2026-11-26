using NUnit.Framework;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // docs/art/battle-terrain.md 6: the battle night is blue.
    public class BattleLightTests
    {
        [Test]
        public void NightAmbientIsBlueAndBrighterThanTheStagesOwn()
        {
            var stage = new Color(0.23f, 0.18f, 0.21f);
            var night = BattleLight.Night(stage);
            Assert.Greater(night.b, night.r);
            Assert.Greater(night.grayscale, stage.grayscale);
        }
    }
}
