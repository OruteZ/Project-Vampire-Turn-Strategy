using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace VampireSpawn.Game.Screens
{
    // ui-style.md 12 motion on UI Toolkit: screen fade-in, panel appear, the 1.2 s pulse (brightness ±10%) and the 0.2 s flash (+30% then back).
    public static class Fx
    {
        // A black cover that clears in 0.3 s (.fade-cover transition in Theme.uss).
        public static void FadeIn(VisualElement root)
        {
            var cover = ToolkitKit.E(root, "fade-cover");
            cover.RegisterCallback<TransitionEndEvent>(_ => cover.RemoveFromHierarchy());
            Later(cover, () => cover.AddToClassList("gone"));
        }

        // A panel shows in 0.15 s rising 8 px (.appear in Theme.uss).
        public static void Appear(VisualElement e)
        {
            e.AddToClassList("appear");
            e.AddToClassList("enter");
            Later(e, () => e.RemoveFromClassList("enter"));
        }

        // Two scheduler ticks: the first frame must resolve the start style, or there is nothing to transition from.
        public static void Later(VisualElement e, Action then) => e.schedule.Execute(() => e.schedule.Execute(then));

        public static void FadeOut(VisualElement e, float seconds)
        {
            var start = Time.unscaledTime;
            e.schedule.Execute(() => e.style.opacity = 1f - (Time.unscaledTime - start) / seconds).Every(16).ForDuration((long)(seconds * 1000f));
            e.schedule.Execute(e.RemoveFromHierarchy).StartingIn((long)(seconds * 1000f));
        }

        public static float PulseK(float period = 1.2f) => 1f + 0.1f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / period);

        public static Color Bright(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        // Tints the element's picture; pause the returned item before recolouring it.
        public static IVisualElementScheduledItem Pulse(VisualElement e, Color c, float period = 1.2f) =>
            e.schedule.Execute(() => e.style.unityBackgroundImageTintColor = Bright(c, PulseK(period))).Every(16);

        public static void Flash(Label l, float seconds = 0.2f)
        {
            var c = l.style.color.keyword == StyleKeyword.Undefined ? l.style.color.value : l.resolvedStyle.color;
            var start = Time.unscaledTime;
            l.schedule.Execute(() =>
            {
                var t = Time.unscaledTime - start;
                l.style.color = t < seconds ? Bright(c, 1f + 0.3f * (1f - t / seconds)) : c;
            }).Every(16).ForDuration((long)(seconds * 1000f) + 32);
        }
    }
}
