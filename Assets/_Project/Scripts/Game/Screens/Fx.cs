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
            FadeOut(Ui.Bg(cover, Color.black, null, false), seconds);
        }

        public static void FadeOut(Graphic g, float seconds) => Add(g).fade = seconds;

        // ui-style.md 12: a panel shows in 0.15 s, rising 8 px.
        public static void Appear(RectTransform rt) => rt.gameObject.AddComponent<PanelAppear>();

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
                g.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - t / fade));
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

    public sealed class PanelAppear : MonoBehaviour
    {
        const float Seconds = 0.15f, Rise = 8f;
        CanvasGroup group;
        Vector2 end;
        float start;

        void Start()
        {
            var rt = (RectTransform)transform;
            end = rt.anchoredPosition;
            group = gameObject.AddComponent<CanvasGroup>();
            start = Time.unscaledTime;
            Update();
        }

        void Update()
        {
            var k = Mathf.Clamp01((Time.unscaledTime - start) / Seconds);
            group.alpha = k;
            ((RectTransform)transform).anchoredPosition = end - new Vector2(0f, Rise * (1f - k));
            if (k < 1f) return;
            Destroy(group);
            Destroy(this);
        }
    }
}
