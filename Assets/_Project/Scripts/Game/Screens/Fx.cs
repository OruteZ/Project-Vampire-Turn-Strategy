using UnityEngine;
using UnityEngine.UI;

namespace VampireSpawn.Game.Screens
{
    // ui-style.md 12: the 1.2 s pulse (brightness ±10%) and the 0.2 s flash (+30% then back) for 1단계 changes.
    public sealed class Fx : MonoBehaviour
    {
        Graphic g;
        Color baseColor;
        float start, period, flash, fade;

        public static void Pulse(Graphic g, float period = 1.2f) => Add(g).period = period;

        public static void Flash(Graphic g, float seconds = 0.2f) => Add(g).flash = seconds;

        // ui-style.md 12 screen change: a black cover that clears in 0.3 s.
        public static void FadeIn(RectTransform root, float seconds = 0.3f)
        {
            var cover = Ui.Node(root, "FadeIn");
            Ui.Stretch(cover);
            var img = Ui.Bg(cover, Color.black, null, false);
            var fx = Add(img);
            fx.fade = seconds;
        }

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
            if (fade > 0f)
            {
                if (t >= fade) { Destroy(gameObject); return; }
                g.color = new Color(0f, 0f, 0f, 1f - t / fade);
                return;
            }
            if (period > 0f) k = 1f + 0.1f * Mathf.Sin(t * Mathf.PI * 2f / period);
            else if (t < flash) k = 1f + 0.3f * (1f - t / flash);
            else { g.color = baseColor; Destroy(this); return; }
            var c = baseColor * k;
            c.a = baseColor.a;
            g.color = c;
        }
    }
}
