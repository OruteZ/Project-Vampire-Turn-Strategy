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
                Assert.AreEqual(stage.Map.Any(line => line.Contains("~")), view.art.Dressing(stage.Id).transform.Find("Water") != null, stage.Id + " water plane only where the stage has water (battle-terrain.md 2-2)");
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
                    if (terrain != Terrain.Water) Assert.AreEqual(view.Stand(h).y, t.position.y, 0.001f, stage.Id + " " + t.name + " stands on the terrain");
                    Assert.AreEqual(terrain == Terrain.Torch, t.Cast<Transform>().Any(c => c.name == view.art.brazier.name), stage.Id + " brazier on " + t.name + " only on torch");
                    Assert.AreEqual(terrain == Terrain.Torch || terrain == Terrain.Sun, t.GetComponentInChildren<Light>() != null, stage.Id + " light on " + t.name);
                    Assert.AreEqual(terrain == Terrain.Sun, t.Cast<Transform>().Any(c => c.name == view.art.sunBeam.name), stage.Id + " sunbeam on " + t.name + " only on sun (battle-terrain.md 3-1)");
                }
                // battle-terrain.md 3: 6 edges per walkable cell, an edge shared by two walkable cells drawn once, 4 quad pieces per edge.
                var cells = Enumerable.Range(0, board.Rows).SelectMany(r => Enumerable.Range(0, board.Cols).Select(c => new Hex(c, r))).Where(board.Walkable).ToList();
                var shared = cells.Sum(x => board.Neighbors(x).Count(board.Walkable)) / 2;
                var grid = children.Single(c => c.name == "Grid").GetComponent<MeshFilter>().sharedMesh;
                var fills = cells.Count(x => board.At(x) == Terrain.Rough);
                Assert.AreEqual((cells.Count * 6 - shared) * 4 * 4 + fills * 6 * 4 * 3, grid.vertexCount, stage.Id + " grid vertices");
                Assert.GreaterOrEqual(grid.vertices.Min(v => v.y), BoardView.HologramRise + 0.04f - 0.001f, stage.Id + " grid never dips below the hologram floor (battle-terrain.md 3)");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
