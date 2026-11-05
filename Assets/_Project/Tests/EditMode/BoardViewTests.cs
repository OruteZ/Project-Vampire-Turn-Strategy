using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;
using Terrain = VampireSpawn.Core.Terrain;

namespace VampireSpawn.Tests
{
    public class BoardViewTests
    {
        static IEnumerable<int> StageIndexes() => Enumerable.Range(0, GameData.Stages.Length);

        [TestCaseSource(nameof(StageIndexes))]
        public void EveryMapCellBecomesOneTileWithItsTerrain(int i)
        {
            var stage = GameData.Stages[i];
            var go = new GameObject("BoardViewTest");
            try
            {
                var view = go.AddComponent<BoardView>();
                view.art = AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/_Project/Art/ArtCatalog.asset");
                Assert.IsNotNull(view.art, "ArtCatalog asset");
                view.Show(stage);
                var tiles = Enumerable.Range(0, go.transform.childCount).Select(k => go.transform.GetChild(k)).ToList();
                var cells = stage.Map.Sum(line => line.Length);
                Assert.AreEqual(cells, tiles.Count, stage.Id + " tile count");
                var board = new Board(stage.Map);
                foreach (var t in tiles)
                {
                    var parts = t.name.Split(' ');
                    var h = new Hex(int.Parse(parts[1].Split(',')[0]), int.Parse(parts[1].Split(',')[1]));
                    var terrain = board.At(h);
                    Assert.AreEqual(terrain.ToString(), parts[2], stage.Id + " " + t.name);
                    var expected = terrain switch
                    {
                        Terrain.Rock => view.art.rock,
                        Terrain.Water => view.art.water,
                        Terrain.Sun => view.art.sunlit,
                        _ => board.Lit(h) ? view.art.lit : view.art.plain,
                    };
                    Assert.AreEqual(expected, t.GetComponent<MeshRenderer>().sharedMaterial, stage.Id + " material of " + t.name);
                    Assert.AreEqual(terrain == Terrain.Rock, t.Cast<Transform>().Any(c => c.name.StartsWith("SM_Env_Rock")), stage.Id + " rock prop on " + t.name + " only on rock");
                    Assert.AreEqual(terrain == Terrain.Torch, t.Cast<Transform>().Any(c => c.name == view.art.brazier.name), stage.Id + " brazier on " + t.name + " only on torch");
                    Assert.AreEqual(terrain == Terrain.Torch || terrain == Terrain.Sun, t.GetComponentInChildren<Light>() != null, stage.Id + " light on " + t.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
