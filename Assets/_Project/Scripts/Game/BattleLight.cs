using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // docs/art/battle-terrain.md 6: one rule lights every battle. The night is blue rather than dark; braziers and sun stay warm.
    public sealed class BattleLight : MonoBehaviour
    {
        static readonly Color NightBlue = new Color(0.42f, 0.52f, 0.82f), MoonBlue = new Color(0.62f, 0.72f, 0.95f), RimBlue = new Color(0.55f, 0.65f, 0.9f);
        const float NightShare = 0.8f, MoonIntensity = 1.3f, RimIntensity = 0.6f;
        // The pool sits over the board so the world around it stays a step darker; intensity is per squared hex of distance (URP spots fall off with 1/d²).
        const float PoolIntensity = 1.9f, PoolMargin = 1.5f, PoolDistance = 3f;
        // From behind and above the camera so unit fronts catch it; the rim comes from across the board.
        static readonly Quaternion MoonAngle = Quaternion.Euler(40f, 20f, 0f), RimAngle = Quaternion.Euler(25f, 200f, 0f);

        BattleView view;
        Light moon, rim, pool;

        public static Color Night(Color stage) => Color.Lerp(stage, NightBlue, NightShare);

        public void Attach(BattleView battleView)
        {
            view = battleView;
            // Edit-mode tests run in whatever scene is open; leave its light alone.
            moon = Application.isPlaying ? RenderSettings.sun : null;
            if (moon == null && Application.isPlaying)
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { moon = l; break; }
            if (moon != null)
            {
                moon.intensity = MoonIntensity;
                moon.shadows = LightShadows.Soft;
                moon.transform.rotation = MoonAngle;
            }
            rim = Add("Rim Light", LightType.Directional, RimAngle);
            rim.intensity = RimIntensity;
            pool = Add("Moon Pool", LightType.Spot, MoonAngle);
        }

        Light Add(string name, LightType type, Quaternion angle)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.rotation = angle;
            l.type = type;
            l.shadows = LightShadows.None;
            return l;
        }

        // After BoardView.Show: aim the pool at the new board.
        public void Fit()
        {
            var board = view.board;
            if (board.Board == null || pool == null) return;
            var s = board.art.hexSize;
            var a = board.HexToWorld(new Hex(0, 0));
            var b = board.HexToWorld(new Hex(board.Board.Cols - 1, board.Board.Rows - 1));
            var radius = Vector3.Distance(a, b) * 0.5f + PoolMargin * s;
            var distance = radius * PoolDistance;
            pool.transform.position = (a + b) * 0.5f - MoonAngle * Vector3.forward * distance;
            pool.range = distance * 2f;
            pool.spotAngle = 2f * Mathf.Atan(radius / distance) * Mathf.Rad2Deg;
            pool.innerSpotAngle = pool.spotAngle * 0.7f;
            pool.intensity = PoolIntensity * distance * distance / (s * s);
            if (moon != null) moon.color = pool.color = MoonBlue;
            rim.color = RimBlue;
        }
    }
}
