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
                Assert.AreEqual(1, children.Count(c => c.name == "Dressing"), stage.Id + " baked dressing (stage-dressing.md 4-6)");
                Assert.IsNotNull(view.Ground, stage.Id + " terrain (battle-terrain.md 1-1)");
                var look = view.art.Dressing(stage.Id).GetComponent<StageDressing>();
                Assert.AreEqual(look.fog, RenderSettings.fogColor, stage.Id + " fog from the stage prefab (stage-dressing.md 4-10)");
                Assert.AreEqual(look.ambient, RenderSettings.ambientLight, stage.Id + " ambient from the stage prefab");
                var tiles = children.Where(c => c.name.StartsWith("Tile ")).ToList();
                var theme = view.art.Theme(stage.Id);
                CollectionAssert.Contains(theme.stages, stage.Id, stage.Id + " has its own theme (stage-dressing.md 2)");
                Assert.AreEqual(stage.Map.Sum(line => line.Length), tiles.Count, stage.Id + " tile count");
                var board = new Board(stage.Map);
                foreach (var t in tiles)
                {
                    var parts = t.name.Split(' ');
                    var h = new Hex(int.Parse(parts[1].Split(',')[0]), int.Parse(parts[1].Split(',')[1]));
                    var terrain = board.At(h);
                    Assert.AreEqual(terrain.ToString(), parts[2], stage.Id + " " + t.name);
                    Assert.AreEqual(terrain == Terrain.Water, t.GetComponent<MeshRenderer>() != null, stage.Id + " only water keeps a surface on " + t.name);
                    if (terrain != Terrain.Water) Assert.AreEqual(view.Stand(h).y, t.position.y, 0.001f, stage.Id + " " + t.name + " stands on the terrain");
                    Assert.AreEqual(terrain == Terrain.Torch, t.Cast<Transform>().Any(c => c.name == view.art.brazier.name), stage.Id + " brazier on " + t.name + " only on torch");
                    Assert.AreEqual(terrain == Terrain.Torch || terrain == Terrain.Sun, t.GetComponentInChildren<Light>() != null, stage.Id + " light on " + t.name);
                }
                // battle-terrain.md 3: one outline of 6 edges x 4 pieces per walkable cell, each piece a quad.
                var walkable = tiles.Count(t => board.Walkable(new Hex(int.Parse(t.name.Split(' ')[1].Split(',')[0]), int.Parse(t.name.Split(' ')[1].Split(',')[1]))));
                var grid = children.Single(c => c.name == "Grid").GetComponent<MeshFilter>().sharedMesh;
                var fills = Enumerable.Range(0, board.Rows).SelectMany(r => Enumerable.Range(0, board.Cols).Select(c => new Hex(c, r))).Count(x => board.Walkable(x) && (board.At(x) == Terrain.Sun || board.Lit(x)));
                Assert.AreEqual(walkable * 6 * 4 * 4 + fills * 6 * 4 * 3, grid.vertexCount, stage.Id + " grid vertices");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
