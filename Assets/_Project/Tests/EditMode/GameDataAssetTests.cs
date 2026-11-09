using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Tests
{
    public class GameDataAssetTests
    {
        [TearDown] public void BackToShipped() => GameData.Use(null);

        static GameDataAsset Shipped => AssetDatabase.LoadAssetAtPath<GameDataAsset>("Assets/_Project/Data/GameData.asset");

        // Copies every asset so a test can change values without touching the shipped files.
        static GameDataAsset Copy()
        {
            var root = Object.Instantiate(Shipped);
            root.rules = Object.Instantiate(root.rules);
            root.units = root.units.Select(Object.Instantiate).ToList();
            root.minions = root.minions.Select(Object.Instantiate).ToList();
            root.stages = root.stages.Select(Object.Instantiate).ToList();
            return root;
        }

        [Test]
        public void ShippedAssetsAreComplete()
        {
            Assert.IsNotNull(Shipped, "Assets/_Project/Data/GameData.asset");
            Assert.IsEmpty(Shipped.Problems(), "데이터 편집기에 나오는 문제를 고친다");
        }

        [Test]
        public void EveryUnitTypeHasThreeBranchesOfOnePrice()
        {
            foreach (var t in GameData.Types)
            {
                var b = GameData.Branches(t.Key);
                Assert.AreEqual(3, b.Length, t.Key);
                Assert.AreEqual(1, b.Select(u => u.Cost).Distinct().Count(), t.Key + " branches share a price");
                Assert.IsTrue(b[0].Inherit, t.Key + " branch 1 carries over to a spawn");
                Assert.IsFalse(b[1].Inherit || b[2].Inherit, t.Key + " branches 2 and 3 do not");
            }
        }

        [Test]
        public void GameReadsTheDataInUse()
        {
            var root = Copy();
            root.units.First(u => u.key == "knight").hp = 21;
            root.rules.moonWake = 2;
            root.stages[0].pop = 13;
            GameData.Use(root.ToDataSet());
            var b = new Battle(GameData.Stages[0]);
            Assert.AreEqual(21, b.Allies.First(u => u.Type == "knight").MaxHp);
            Assert.AreEqual(2, Rules.MoonWake);
            Assert.AreEqual(13, b.Stage.Pop);
            GameData.Use(null);
            Assert.AreEqual(Shipped.units.First(u => u.key == "knight").hp, new Battle(GameData.Stages[0]).Allies.First(u => u.Type == "knight").MaxHp, "Use(null) reads the assets again");
        }

        [Test]
        public void ValidateNamesBrokenData()
        {
            var root = Copy();
            var s = root.stages[1];
            s.map[2] = s.map[2] + ".";
            s.enemies.Add(new EnemySpot { kind = EnemyType.Vampire, col = 0, row = 0 });
            s.enemies.Add(new EnemySpot { kind = "ghost", col = 1, row = 0 });
            s.sample.Add(new UnitSpot { kind = "hunter", col = 0, row = 1 });
            s.units.Add("dragoon");
            root.units[0].upgrades[0].id = root.units[0].upgrades[1].id;
            var problems = string.Join("\n", root.Problems());
            StringAssert.Contains("지도 2행 길이", problems);
            StringAssert.Contains("뱀파이어는 하나여야 한다", problems);
            StringAssert.Contains("없는 적 ghost", problems);
            StringAssert.Contains("없는 병종 dragoon", problems);
            StringAssert.Contains("배치 구역 밖", problems);
            StringAssert.Contains("쓸 수 없는 병종", problems);
            StringAssert.Contains("강화 id", problems);
        }
    }
}
