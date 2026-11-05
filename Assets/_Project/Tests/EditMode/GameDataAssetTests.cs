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
        [TearDown] public void BackToPrototype() => GameData.Use(null);

        static GameDataAsset InMemory(DataSet p)
        {
            var root = ScriptableObject.CreateInstance<GameDataAsset>();
            root.rules = ScriptableObject.CreateInstance<RulesData>();
            root.rules.Fill(p.Rules);
            root.skeleton = ScriptableObject.CreateInstance<EnemyData>();
            root.skeleton.Fill(p.Skeleton);
            root.units = p.Types.Select(t => { var u = ScriptableObject.CreateInstance<UnitTypeData>(); u.Fill(t, p.Upgrades); return u; }).ToArray();
            root.stages = p.Stages.Select(s => { var a = ScriptableObject.CreateInstance<StageData>(); a.Fill(s); return a; }).ToList();
            return root;
        }

        [Test]
        public void ShippedAssetsAreComplete()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameDataAsset>("Assets/_Project/Data/GameData.asset");
            Assert.IsNotNull(root, "Assets/_Project/Data/GameData.asset");
            Assert.IsEmpty(root.Problems(), "데이터 편집기에 나오는 문제를 고친다");
        }

        [Test]
        public void AssetsKeepEveryPrototypeValue()
        {
            var p = PrototypeData.Create();
            CollectionAssert.AreEqual(p.Describe(), InMemory(p).ToDataSet().Describe());
            Assert.IsEmpty(p.Validate());
        }

        [Test]
        public void GameReadsTheDataInUse()
        {
            var root = InMemory(PrototypeData.Create());
            root.units[(int)UnitKind.Knight].hp = 21;
            root.rules.moonWake = 2;
            root.stages[0].pop = 13;
            GameData.Use(root.ToDataSet());
            var b = new Battle(GameData.Stages[0]);
            Assert.AreEqual(21, b.Allies.First(u => u.Type == UnitKind.Knight).MaxHp);
            Assert.AreEqual(2, Rules.MoonWake);
            Assert.AreEqual(13, b.Stage.Pop);
            GameData.Use(null);
            Assert.AreEqual(14, new Battle(GameData.Stages[0]).Allies.First(u => u.Type == UnitKind.Knight).MaxHp, "Use(null) is the HTML value again");
        }

        [Test]
        public void ValidateNamesBrokenStages()
        {
            var root = InMemory(PrototypeData.Create());
            var s = root.stages[1];
            s.map[2] = s.map[2] + ".";
            s.enemies.Add(new EnemySpot { kind = EnemyKind.Vampire, col = 0, row = 0 });
            s.sample.Add(new UnitSpot { kind = UnitKind.Hunter, col = 0, row = 1 });
            root.units[0].upgrades[0].id = root.units[0].upgrades[1].id;
            var problems = string.Join("\n", root.Problems());
            StringAssert.Contains("지도 2행 길이", problems);
            StringAssert.Contains("뱀파이어는 하나여야 한다", problems);
            StringAssert.Contains("배치 구역 밖", problems);
            StringAssert.Contains("쓸 수 없는 병종", problems);
            StringAssert.Contains("강화 id", problems);
        }
    }
}
