using NUnit.Framework;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // docs/art/battle-terrain.md 6, ui-style.md 7-2: the board light follows the moon the screen has played.
    public class BattleLightTests
    {
        [Test]
        public void BloodGrowsWithTheSquareOfTheMoonAndIsWholeOnTheFullMoonTurn()
        {
            Assert.AreEqual(0f, BattleLight.BloodShare(0, 4, false));
            Assert.AreEqual(0.35f * 9f / 16f, BattleLight.BloodShare(3, 4, false), 1e-5f);
            Assert.AreEqual(0.35f, BattleLight.BloodShare(4, 4, false), 1e-5f);
            Assert.AreEqual(0.6f, BattleLight.BloodShare(2, 4, true), 1e-5f);
            Assert.AreEqual(0f, BattleLight.BloodShare(3, 0, false), "no moon gauge, always blue");
        }

        [Test]
        public void PlayedMoonCountsDeathsAsTheyPlayNotAsTheModelHasThem()
        {
            var s = (Moon: 2, Full: false);
            s = BattleLight.Played(s, BattleEvent.Died(1, new Hex(0, 0)), 4);
            Assert.AreEqual((3, false), s);
            s = BattleLight.Played(s, BattleEvent.Died(2, new Hex(0, 0)), 4);
            s = BattleLight.Played(s, BattleEvent.Died(3, new Hex(0, 0)), 4);
            Assert.AreEqual((4, false), s, "never past the full moon");
            s = BattleLight.Played(s, BattleEvent.Banner(BannerKind.FullMoon, new Message("banner.fullMoon")), 4);
            Assert.IsTrue(s.Full);
            s = BattleLight.Played((3, false), BattleEvent.MoonChanged(2), 4);
            Assert.AreEqual((2, false), s);
        }

        [Test]
        public void NightAmbientIsBlueAndBrighterThanTheStagesOwn()
        {
            var stage = new Color(0.23f, 0.18f, 0.21f);
            var night = BattleLight.Night(stage);
            Assert.Greater(night.b, night.r);
            Assert.Greater(night.grayscale, stage.grayscale);
        }

        [Test]
        public void BloodKeepsTheBrightnessOfTheLightItTints()
        {
            var blue = new Color(0.5f, 0.6f, 0.9f);
            var red = BattleLight.Toward(blue, 1f);
            Assert.Greater(red.r, red.b);
            Assert.AreEqual(blue.grayscale, red.grayscale, 0.02f);
        }
    }
}
