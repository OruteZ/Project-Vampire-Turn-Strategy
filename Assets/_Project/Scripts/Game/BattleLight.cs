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

        // ui-style.md 7-2 "보드 조명": moon.blood scaled to full red so a tint can keep the light's brightness.
        static readonly Color Blood = Screens.Ui.MoonBlood / Screens.Ui.MoonBlood.maxColorComponent;
        const float BloodAtFull = 0.6f, BlendSeconds = 0.6f;

        BattleView view;
        Light moon, rim, pool;
        (int Moon, bool Full) played;
        float blood, from, to, since;

        public static Color Night(Color stage) => Color.Lerp(stage, NightBlue, NightShare);

        public static float BloodShare(int moon, int max, bool full)
        {
            if (full) return 1f;
            if (max <= 0) return 0f;
            var t = Mathf.Clamp01((float)moon / max);
            return t * t * BloodAtFull;
        }

        // The model is ahead of the screen during playback (docs/areas/ui.md), so the light counts what has played.
        public static (int Moon, bool Full) Played((int Moon, bool Full) s, BattleEvent e, int max) => e.Kind switch
        {
            BattleEventKind.Died => (System.Math.Min(max, s.Moon + 1), s.Full),
            BattleEventKind.MoonChanged => (e.Value, s.Full),
            BattleEventKind.Banner when e.BannerKind == BannerKind.FullMoon => (s.Moon, true),
            _ => s,
        };

        public static Color Toward(Color light, float share)
        {
            var red = Blood * (light.grayscale / Blood.grayscale);
            red.a = light.a;
            return Color.Lerp(light, red, share);
        }

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
            view.Played += OnPlayed;
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
            played = (view.Battle.Moon, false);
            blood = from = to = Target();
            Apply();
        }

        void OnDestroy() { if (view != null) view.Played -= OnPlayed; }

        void OnPlayed(BattleEvent e) => played = Played(played, e, Rules.MoonMax);

        float Target() => view.Battle.Stage.HasBoss ? BloodShare(played.Moon, Rules.MoonMax, played.Full) : 0f;

        void Update()
        {
            if (view == null || view.Battle == null || pool == null) return;
            if (!view.IsBusy) played = (view.Battle.Moon, false);
            var target = Target();
            if (!Mathf.Approximately(target, to)) { from = blood; to = target; since = Time.unscaledTime; }
            blood = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - since) / BlendSeconds));
            Apply();
        }

        void Apply()
        {
            RenderSettings.ambientLight = Toward(view.board.Ambient, blood);
            pool.color = Toward(MoonBlue, blood);
            if (moon != null) moon.color = pool.color;
            rim.color = Toward(RimBlue, blood);
        }
    }
}
