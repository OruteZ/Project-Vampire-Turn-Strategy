using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    public class GameDataParityTests
    {
        [Serializable] class Constants { public int drainHeal, moonMax, eatRange, moonWake; }
        [Serializable] class TypeRow { public string key, name, glyph, tier, desc; public int pop, silver, hp, atk, def, mov, rng, bane, unlockAt; }
        [Serializable] class UpgradeRow { public string unit, id, name, desc, abil; public int cost, hp, atk, def, mov, rng, bane; public bool inherit; }
        [Serializable] class EnemyRow { public string name, glyph; public int hp, atk, def, mov, rng; }
        [Serializable] class BossRow { public string name; public int hp, atk, def, mov, heal; }
        [Serializable] class PlaceRow { public string kind; public int c, r; }
        [Serializable] class StageRow
        {
            public string id, label, title, brief, bonusType, bonusText;
            public string[] units, map;
            public int pop, silver, reward, bonusVal, bonusGold;
            public BossRow boss;
            public PlaceRow[] enemies, sample;
        }
        [Serializable] class Golden { public Constants constants; public TypeRow[] types; public UpgradeRow[] upgrades; public EnemyRow skeleton; public StageRow[] stages; }

        static Golden golden;

        [OneTimeSetUp]
        public void Load()
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "tools", "golden", "out", "data.json");
            golden = JsonUtility.FromJson<Golden>(File.ReadAllText(file));
            Assert.That(golden.stages, Is.Not.Null.And.Not.Empty, "data.json did not parse: " + file);
        }

        static string Key(UnitKind k) => GameData.Type(k).Key;
        static string Snake(Enum e) => char.ToLowerInvariant(e.ToString()[0]) + e.ToString().Substring(1);

        [Test]
        public void ConstantsMatch()
        {
            Assert.AreEqual(golden.constants.drainHeal, Rules.DrainHeal, "DRAIN_HEAL");
            Assert.AreEqual(golden.constants.moonMax, Rules.MoonMax, "MOON_MAX");
            Assert.AreEqual(golden.constants.eatRange, Rules.EatRange, "EAT_RANGE");
            Assert.AreEqual(golden.constants.moonWake, Rules.MoonWake, "MOON_WAKE");
        }

        [Test]
        public void UnitTypesMatch()
        {
            Assert.AreEqual(golden.types.Length, GameData.Types.Length, "type count");
            for (var i = 0; i < golden.types.Length; i++)
            {
                var g = golden.types[i];
                var t = GameData.Types[i];
                var at = "types." + g.key + ".";
                Assert.AreEqual(i, (int)t.Kind, at + "kind order");
                Assert.AreEqual(g.key, t.Key, at + "key");
                Assert.AreEqual(g.name, Loc.Text(t.Name), at + "name");
                Assert.AreEqual(g.glyph, Loc.Text(t.Glyph), at + "glyph");
                Assert.AreEqual(g.tier, Snake(t.Tier), at + "tier");
                Assert.AreEqual(g.desc, Loc.Text(t.Desc), at + "desc");
                Assert.AreEqual(new[] { g.pop, g.silver, g.hp, g.atk, g.def, g.mov, g.rng, g.bane, g.unlockAt },
                    new[] { t.Pop, t.Silver, t.Hp, t.Atk, t.Def, t.Mov, t.Rng, t.Bane, t.UnlockAt }, at + "pop,silver,hp,atk,def,mov,rng,bane,unlockAt");
            }
        }

        [Test]
        public void UpgradesMatch()
        {
            Assert.AreEqual(golden.upgrades.Length, GameData.Upgrades.Length, "upgrade count");
            for (var i = 0; i < golden.upgrades.Length; i++)
            {
                var g = golden.upgrades[i];
                var u = GameData.Upgrades[i];
                var at = "upgrades." + g.id + ".";
                Assert.AreEqual(g.unit, Key(u.Unit), at + "unit");
                Assert.AreEqual(g.id, u.Id, at + "id");
                Assert.AreEqual(g.name, Loc.Text(u.Name), at + "name");
                Assert.AreEqual(g.desc, Loc.Text(u.Desc), at + "desc");
                Assert.AreEqual(g.cost, u.Cost, at + "cost");
                Assert.AreEqual(g.inherit, u.Inherit, at + "inherit");
                Assert.AreEqual(new[] { g.hp, g.atk, g.def, g.mov, g.rng, g.bane },
                    new[] { u.Mod.Hp, u.Mod.Atk, u.Mod.Def, u.Mod.Mov, u.Mod.Rng, u.Mod.Bane }, at + "hp,atk,def,mov,rng,bane");
                Assert.AreEqual(g.abil, u.Ability == Ability.None ? "" : Snake(u.Ability), at + "abil");
            }
        }

        [Test]
        public void SkeletonMatches()
        {
            var g = golden.skeleton;
            var s = GameData.Skeleton;
            Assert.AreEqual(g.name, Loc.Text(s.Name), "skeleton.name");
            Assert.AreEqual(g.glyph, Loc.Text(s.Glyph), "skeleton.glyph");
            Assert.AreEqual(new[] { g.hp, g.atk, g.def, g.mov, g.rng }, new[] { s.Hp, s.Atk, s.Def, s.Mov, s.Rng }, "skeleton.hp,atk,def,mov,rng");
        }

        [Test]
        public void StagesMatch()
        {
            Assert.AreEqual(golden.stages.Length, GameData.Stages.Length, "stage count");
            for (var i = 0; i < golden.stages.Length; i++)
            {
                var g = golden.stages[i];
                var s = GameData.Stages[i];
                var at = "stages." + g.id + ".";
                Assert.AreEqual(g.id, s.Id, at + "id");
                Assert.AreEqual(g.label, Loc.Text(s.Label), at + "label");
                Assert.AreEqual(g.title, Loc.Text(s.Title), at + "title");
                Assert.AreEqual(g.brief, Loc.Text(s.Brief), at + "brief");
                Assert.AreEqual(g.units, s.Units.Select(Key).ToArray(), at + "units");
                Assert.AreEqual(new[] { g.pop, g.silver, g.reward }, new[] { s.Pop, s.Silver, s.Reward }, at + "pop,silver,reward");
                Assert.AreEqual(g.bonusType, Snake(s.Bonus.Type), at + "bonus.type");
                Assert.AreEqual(new[] { g.bonusVal, g.bonusGold }, new[] { s.Bonus.Value, s.Bonus.Gold }, at + "bonus.val,gold");
                Assert.AreEqual(g.bonusText, Loc.Text(s.Bonus.Text), at + "bonusText");
                Assert.AreEqual(g.boss.name, Loc.Text(s.BossName), at + "boss.name");
                Assert.AreEqual(new[] { g.boss.hp, g.boss.atk, g.boss.def, g.boss.mov, g.boss.heal },
                    new[] { s.Boss.Hp, s.Boss.Atk, s.Boss.Def, s.Boss.Mov, s.Boss.Heal }, at + "boss.hp,atk,def,mov,heal");
                Assert.AreEqual(g.map, s.Map, at + "map");
                Assert.AreEqual(g.enemies.Select(e => $"{e.kind}@{e.c},{e.r}").ToArray(),
                    s.Enemies.Select(e => $"{Snake(e.Kind)}@{e.Col},{e.Row}").ToArray(), at + "enemies");
                Assert.AreEqual(g.sample.Select(e => $"{e.kind}@{e.c},{e.r}").ToArray(),
                    s.Sample.Select(e => $"{Key(e.Kind)}@{e.Col},{e.Row}").ToArray(), at + "sample");
            }
        }
    }
}
