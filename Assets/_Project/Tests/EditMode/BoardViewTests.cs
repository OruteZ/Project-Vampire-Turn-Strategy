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
                var children = Enumerable.Range(0, go.transform.childCount).Select(k => go.transform.GetChild(k)).ToList();
                var tiles = children.Where(c => c.name != "Dressing").ToList();
                var theme = view.art.Theme(stage.Id);
                Assert.IsNotNull(theme, stage.Id + " theme");
                CollectionAssert.Contains(theme.stages, stage.Id, stage.Id + " has its own theme (stage-dressing.md 2)");
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
                        _ => board.Lit(h) ? view.art.lit : null,
                    };
                    var mat = t.GetComponent<MeshRenderer>().sharedMaterial;
                    if (expected != null) Assert.AreEqual(expected, mat, stage.Id + " material of " + t.name);
                    else Assert.Less(((Vector4)(theme.ground - mat.color)).magnitude, 0.01f, stage.Id + " themed ground on " + t.name);
                    Assert.AreEqual(terrain == Terrain.Torch, t.Cast<Transform>().Any(c => c.name == view.art.brazier.name), stage.Id + " brazier on " + t.name + " only on torch");
                    Assert.AreEqual(terrain == Terrain.Torch || terrain == Terrain.Sun, t.GetComponentInChildren<Light>() != null, stage.Id + " light on " + t.name);
                }
                Assert.AreEqual(1, children.Count(c => c.name == "Dressing"), stage.Id + " baked dressing (stage-dressing.md 4-6)");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
