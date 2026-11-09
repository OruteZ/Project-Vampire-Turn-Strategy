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
    // docs/art/stage-dressing.md 4-8: hand-edited dressing prefabs still keep props off walkable cells and on every rock cell.
    public class StageDressingTests
    {
        static IEnumerable<int> StageIndexes() => Enumerable.Range(0, GameData.Stages.Length);

        [TestCaseSource(nameof(StageIndexes))]
        public void TheStagePrefabKeepsWalkableCellsClearAndCoversRock(int i)
        {
            var stage = GameData.Stages[i];
            var art = AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/_Project/Art/ArtCatalog.asset");
            var prefab = art.Dressing(stage.Id);
            Assert.IsNotNull(prefab, stage.Id + " in ArtCatalog.stageDressings");
            Assert.AreEqual(stage.Id, prefab.GetComponent<StageDressing>()?.stageId, stage.Id + " StageDressing.stageId");
            var props = prefab.transform.Find("Props");
            Assert.IsNotNull(props, stage.Id + " Props");
            var s = art.hexSize;
            var board = new Board(stage.Map);
            var spots = props.Cast<Transform>().Select(p => new Vector2(p.localPosition.x, p.localPosition.z)).ToList();
            for (var r = 0; r < board.Rows; r++)
                for (var c = 0; c < board.Cols; c++)
                {
                    var h = new Hex(c, r);
                    var at = BoardView.Local(h, s);
                    var near = spots.Select(p => (p - new Vector2(at.x, at.z)).magnitude).DefaultIfEmpty(float.MaxValue).Min();
                    var t = board.At(h);
                    if (t == Terrain.Rock) Assert.Less(near, 0.5f * s, stage.Id + " prop on rock " + h);
                    else if (board.Walkable(h)) Assert.Greater(near, 0.9f * s, stage.Id + " prop stands on walkable " + h);
                }
        }
    }
}
