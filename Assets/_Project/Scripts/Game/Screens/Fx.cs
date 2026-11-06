using UnityEngine;
using UnityEngine.UI;

namespace VampireSpawn.Game.Screens
{
    // ui-style.md 12: the 1.2 s pulse (brightness ±10%) and the 0.2 s flash (+30% then back) for 1단계 changes.
    public sealed class Fx : MonoBehaviour
    {
        Graphic g;
        Color baseColor;
        float start, period, flash;

        public static void Pulse(Graphic g, float period = 1.2f) => Add(g).period = period;

        public static void Flash(Graphic g, float seconds = 0.2f) => Add(g).flash = seconds;

        static Fx Add(Graphic g)
        {
            var fx = g.gameObject.AddComponent<Fx>();
            fx.g = g;
            fx.baseColor = g.color;
            fx.start = Time.unscaledTime;
            return fx;
        }

        void Update()
        {
            var t = Time.unscaledTime - start;
            var k = 1f;
            if (period > 0f) k = 1f + 0.1f * Mathf.Sin(t * Mathf.PI * 2f / period);
            else if (t < flash) k = 1f + 0.3f * (1f - t / flash);
            else { g.color = baseColor; Destroy(this); return; }
            var c = baseColor * k;
            c.a = baseColor.a;
            g.color = c;
        }
    }
}
