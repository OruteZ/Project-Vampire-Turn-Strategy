using UnityEngine;
using VampireSpawn.Core;
using Terrain = VampireSpawn.Core.Terrain;

namespace VampireSpawn.Game
{
    // Root of a stage's baked dressing prefab (docs/art/stage-dressing.md 4). Draws the stage's cells while editing so props stay off walkable ones.
    public sealed class StageDressing : MonoBehaviour
    {
        public string stageId;
        [Tooltip("이 장의 주변광 (stage-dressing.md 4-10)")] public Color ambient = new Color(0.25f, 0.25f, 0.32f);
        [Tooltip("이 장의 안개·배경색")] public Color fog = new Color(0.07f, 0.07f, 0.1f);

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
            var ground = GetComponentInChildren<UnityEngine.Terrain>();
            var corners = new Vector3[6];
            for (var r = 0; r < board.Rows; r++)
                for (var c = 0; c < board.Cols; c++)
                {
                    var h = new Hex(c, r);
                    var t = board.At(h);
                    Gizmos.color = t == Terrain.Rock ? Color.red : t == Terrain.Water ? Color.cyan : Color.white;
                    for (var i = 0; i < 6; i++)
                    {
                        var p = transform.TransformPoint(HexLines.Corner(BoardView.Local(h, art.hexSize), art.hexSize * 0.95f, i));
                        p.y = (ground != null ? ground.SampleHeight(p) + ground.transform.position.y : transform.position.y + art.tileHeight) + 0.05f;
                        corners[i] = p;
                    }
                    Gizmos.DrawLineStrip(corners, true);
                }
        }
#endif
    }
}
