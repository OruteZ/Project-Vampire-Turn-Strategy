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
            if (art.DressingTheme(stage.Id)?.bareFloor != null)
            {
                Assert.IsNotNull(prefab.transform.Find("Floor"), stage.Id + " bare floor (stage-dressing.md 6)");
                Assert.AreEqual(0, props.childCount, stage.Id + " bare stage has no props");
                return;
            }
            var ground = prefab.GetComponentInChildren<UnityEngine.Terrain>();
            Assert.IsNotNull(ground, stage.Id + " Terrain (battle-terrain.md 5)");
            Assert.AreEqual(ground.terrainData, ground.GetComponent<TerrainCollider>()?.terrainData, stage.Id + " TerrainCollider uses the same data");
            float Height(Vector3 local) => ground.terrainData.GetInterpolatedHeight((local.x - ground.transform.localPosition.x) / ground.terrainData.size.x, (local.z - ground.transform.localPosition.z) / ground.terrainData.size.z) + ground.transform.localPosition.y;
            foreach (Transform p in props) Assert.LessOrEqual(p.localPosition.y, Height(p.localPosition) + 0.05f, stage.Id + " " + p.name + " floats above the terrain");
            var board = new Board(stage.Map);
            var spots = props.Cast<Transform>().Select(p => new Vector2(p.localPosition.x, p.localPosition.z)).ToList();
            var footprints = props.Cast<Transform>().Select(p => Footprint(props, p)).ToList();
            for (var r = 0; r < board.Rows; r++)
                for (var c = 0; c < board.Cols; c++)
                {
                    var h = new Hex(c, r);
                    var at = BoardView.Local(h, s);
                    var near = spots.Select(p => (p - new Vector2(at.x, at.z)).magnitude).DefaultIfEmpty(float.MaxValue).Min();
                    var t = board.At(h);
                    var door = stage.Doors.Any(d => d.Hexes.Contains(h));
                    if (t == Terrain.Rock && !door) Assert.IsTrue(near < 0.5f * s || footprints.Any(f => f.Contains(new Vector2(at.x, at.z))), stage.Id + " prop on rock " + h);
                    var look = t == Terrain.Gap ? BoardView.Look(stage, art.DressingTheme(stage.Id), h) : (GapLook?)null;
                    if (look == GapLook.LowWall) Assert.Less(near, 0.5f * s, stage.Id + " low wall on gap " + h + " (stage-dressing.md 8)");
                    if (look == GapLook.Flat) Assert.Greater(near, 0.9f * s, stage.Id + " prop on flat gap " + h);
                    if (look == GapLook.Pit)
                    {
                        Assert.Greater(near, 0.9f * s, stage.Id + " prop on void " + h);
                        Assert.Less(Height(at), -1f, stage.Id + " void " + h + " is a pit");
                    }
                    else if (door) Assert.Greater(near, 0.9f * s, stage.Id + " door " + h + " is a live prop, not baked (triggers.md 1-4)");
                    else if (board.Walkable(h))
                    {
                        Assert.Greater(near, 0.9f * s, stage.Id + " prop stands on walkable " + h);
                        Assert.That(Height(at), Is.InRange(-0.3f, 0.3f), stage.Id + " walkable " + h + " height (battle-terrain.md 1-4)");
                    }
                }
        }

        // One big building may cover several rock cells (stage-dressing.md 4-8): its mesh footprint, 0.3 in from the edge, counts.
        static Rect Footprint(Transform props, Transform p)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue); var max = -min;
            foreach (var mf in p.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var m = props.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var b = mf.sharedMesh.bounds;
                for (var k = 0; k < 8; k++)
                {
                    var q = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((k & 1) * 2 - 1, (k & 2) - 1, (k & 4) / 2 - 1)));
                    min = Vector2.Min(min, new Vector2(q.x, q.z)); max = Vector2.Max(max, new Vector2(q.x, q.z));
                }
            }
            return max.x - min.x < 0.6f || max.y - min.y < 0.6f ? Rect.zero : Rect.MinMaxRect(min.x + 0.3f, min.y + 0.3f, max.x - 0.3f, max.y - 0.3f);
        }

        // docs/art/battle-camera.md 13.
        [Test]
        public void APropCountsAsBlockingOnlyBetweenTheCameraAndTheUnit()
        {
            var eye = new Vector3(0f, 10f, -10f);
            var unit = Vector3.zero;
            Assert.IsTrue(BattleCamera.Blocks(new Bounds(new Vector3(0f, 5f, -5f), Vector3.one * 2f), eye, unit));
            Assert.IsFalse(BattleCamera.Blocks(new Bounds(new Vector3(0f, 0f, 3f), Vector3.one * 2f), eye, unit));
            Assert.IsFalse(BattleCamera.Blocks(new Bounds(new Vector3(5f, 5f, -5f), Vector3.one * 2f), eye, unit));
        }
    }
}
