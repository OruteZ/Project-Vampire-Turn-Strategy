using NUnit.Framework;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // docs/audio/sounds.md 1, 5.
    public class SoundTests
    {
        static SoundCatalog Catalog()
        {
            var s = ScriptableObject.CreateInstance<SoundCatalog>();
            foreach (var f in typeof(SoundCatalog).GetFields())
                if (f.FieldType == typeof(Cue)) f.SetValue(s, new Cue());
            return s;
        }

        [Test]
        public void BattleEventsPickTheirSlot()
        {
            var s = Catalog();
            Assert.AreSame(s.playerTurn, BattleSounds.Of(s, BattleEvent.Banner(BannerKind.PlayerTurn, default)));
            Assert.AreSame(s.enemyTurn, BattleSounds.Of(s, BattleEvent.Banner(BannerKind.EnemyTurn, default)));
            Assert.AreSame(s.coffin, BattleSounds.Of(s, BattleEvent.Banner(BannerKind.Coffin, default)));
            Assert.AreSame(s.fullMoon, BattleSounds.Of(s, BattleEvent.Banner(BannerKind.FullMoon, default)));
            Assert.AreSame(s.mend, BattleSounds.Of(s, BattleEvent.Popup(1, PopupKind.Heal, default)));
            Assert.AreSame(s.execute, BattleSounds.Of(s, BattleEvent.Popup(1, PopupKind.Execute, default)));
            Assert.AreSame(s.bury, BattleSounds.Of(s, BattleEvent.Popup(1, PopupKind.Bury, default)));
            Assert.AreSame(s.drain, BattleSounds.Of(s, BattleEvent.Popup(1, PopupKind.Drain, default)));
            Assert.AreSame(s.drainFailed, BattleSounds.Of(s, BattleEvent.Popup(1, PopupKind.DrainFailed, default)));
            Assert.IsNull(BattleSounds.Of(s, BattleEvent.Popup(1, PopupKind.Damage, default)));
            Assert.IsNull(BattleSounds.Of(s, BattleEvent.Popup(1, PopupKind.Feed, default)));
            Assert.AreSame(s.moonWane, BattleSounds.Of(s, BattleEvent.MoonChanged(2)));
            Assert.AreSame(s.rise, BattleSounds.Of(s, BattleEvent.Rose(1, 2, default)));
            Object.DestroyImmediate(s);
        }

        [Test]
        public void TypedKindsKeepTheOldPopupStyles()
        {
            Assert.AreEqual("dmg", BattleEvent.Popup(1, PopupKind.Damage, default).Style);
            Assert.AreEqual("blood", BattleEvent.Popup(1, PopupKind.Drain, default).Style);
            Assert.AreEqual("heal", BattleEvent.Popup(1, PopupKind.Execute, default).Style);
            Assert.AreEqual("player", BattleEvent.Banner(BannerKind.PlayerTurn, default).Style);
            Assert.AreEqual("enemy", BattleEvent.Banner(BannerKind.FullMoon, default).Style);
        }

        [Test]
        public void VariantsTakeTurnsWithoutChance()
        {
            var a = AudioClip.Create("a", 1, 1, 44100, false);
            var b = AudioClip.Create("b", 1, 1, 44100, false);
            var cue = new Cue { clips = new[] { a, b } };
            Assert.AreSame(a, cue.Next());
            Assert.AreSame(b, cue.Next());
            Assert.AreSame(a, cue.Next());
            Assert.IsNull(new Cue().Next());
        }
    }
}
