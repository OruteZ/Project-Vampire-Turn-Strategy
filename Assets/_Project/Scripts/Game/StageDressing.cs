using UnityEngine;
using VampireSpawn.Core;
using Terrain = VampireSpawn.Core.Terrain;

namespace VampireSpawn.Game
{
    // Root of a stage's baked dressing prefab (docs/art/stage-dressing.md 4). Draws the stage's cells while editing so props stay off walkable ones.
    public sealed class StageDressing : MonoBehaviour
    {
        public string stageId;

#if UNITY_EDITOR
        static ArtCatalog art;

        void OnDrawGizmos()
        {
            if (art == null) art = UnityEditor.AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/_Project/Art/ArtCatalog.asset");
            Stage stage;
            try { stage = System.Array.Find(GameData.Stages, s => s.Id == stageId); }
            catch (System.InvalidOperationException) { return; }
            if (art == null || stage == null) return;
            var board = new Board(stage.Map);
            var lift = Vector3.up * (art.tileHeight + 0.02f);
            var corners = new Vector3[6];
            for (var r = 0; r < board.Rows; r++)
                for (var c = 0; c < board.Cols; c++)
                {
                    var h = new Hex(c, r);
                    var t = board.At(h);
                    Gizmos.color = t == Terrain.Rock ? Color.red : t == Terrain.Water ? Color.cyan : Color.white;
                    var centre = BoardView.Local(h, art.hexSize) + lift;
                    for (var i = 0; i < 6; i++)
                    {
                        var a = Mathf.Deg2Rad * 60f * i;
                        corners[i] = transform.TransformPoint(centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * art.hexSize * 0.95f);
                    }
                    Gizmos.DrawLineStrip(corners, true);
                }
        }
#endif
    }
}
