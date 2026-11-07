using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace VampireSpawn.Game.Screens
{
    // Shared UI Toolkit pieces for screens built from UXML/USS (docs/dev/ui-toolkit-trial.md 4, 6). Colours are the Ui tokens.
    public static class ToolkitKit
    {
        // UIDocument builds its tree in OnEnable, so the assets go in before the object wakes.
        public static UIDocument Document(Transform host, string name, PanelSettings panel, VisualTreeAsset tree)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetParent(host, false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = tree;
            go.SetActive(true);
            // The root and the template container cover the whole screen; left pickable they make every click "over UI" (PanelRaycaster).
            doc.rootVisualElement.pickingMode = PickingMode.Ignore;
            foreach (var c in doc.rootVisualElement.Children()) c.pickingMode = PickingMode.Ignore;
            UseFonts(doc.rootVisualElement);
            return doc;
        }

        public static void UseFonts(VisualElement root)
        {
            root.style.unityFontDefinition = FontDefinition.FromSDFFont(OsFont("font.body"));
            foreach (var d in root.Query(className: "display").ToList()) d.style.unityFontDefinition = FontDefinition.FromSDFFont(OsFont("font.display"));
        }

        public static void Picture(VisualElement e, Sprite s, Color? tint = null)
        {
            if (e == null) return;
            e.style.backgroundImage = new StyleBackground(s);
            if (tint.HasValue) e.style.unityBackgroundImageTintColor = tint.Value;
        }

        // ui-style.md 14-3-1: [icon][number], the label lives in the tooltip.
        public static VisualElement Value(VisualElement row, ArtCatalog art, string icon, string value, Color iconColor, Color textColor, string tip = null, Label tipLabel = null)
        {
            var v = new VisualElement();
            v.AddToClassList("value");
            var i = new VisualElement { pickingMode = PickingMode.Ignore };
            i.AddToClassList("value-icon");
            Picture(i, art.Ui(icon), iconColor);
            var t = new Label(value) { pickingMode = PickingMode.Ignore };
            t.AddToClassList("value-text");
            t.style.color = textColor;
            v.Add(i);
            v.Add(t);
            row.Add(v);
            if (tip != null && tipLabel != null) Tip(v, tip, tipLabel, 300);
            return v;
        }

        // HoverTip's frame.tip: shown above the owner after the pointer rests; kept on screen.
        public static void Tip(VisualElement owner, string text, Label tip, long delayMs = 600)
        {
            if (!string.IsNullOrEmpty(text)) Tip(owner, () => text, tip, delayMs);
        }

        // text is read when the tip opens, so a screen that refills in place can change it without binding again.
        public static void Tip(VisualElement owner, Func<string> text, Label tip, long delayMs = 600)
        {
            if (tip == null) return;
            IVisualElementScheduledItem pending = null;
            owner.RegisterCallback<PointerEnterEvent>(_ =>
            {
                pending = owner.schedule.Execute(() =>
                {
                    var now = text();
                    if (string.IsNullOrEmpty(now)) return;
                    tip.text = now;
                    tip.style.display = DisplayStyle.Flex;
                    var b = owner.worldBound;
                    var local = tip.parent.WorldToLocal(new Vector2(b.xMin, b.yMin));
                    var room = tip.parent.contentRect;
                    tip.style.left = Mathf.Clamp(local.x, 8f, Mathf.Max(8f, room.width - 440f));
                    tip.style.top = local.y > 60f ? local.y - 44f : local.y + b.height + 8f;
                }).StartingIn(delayMs);
            });
            owner.RegisterCallback<PointerLeaveEvent>(_ => { pending?.Pause(); tip.style.display = DisplayStyle.None; });
            owner.RegisterCallback<DetachFromPanelEvent>(_ => pending?.Pause());
        }

        // A Synty INTERFACE uGUI prefab redrawn as stacked VisualElements: same sprites, tints, rects, mirroring, scaled into the host's box.
        public static void Prefab(VisualElement host, GameObject prefab, Color? tint = null)
        {
            if (host == null || prefab == null) return;
            var root = (RectTransform)prefab.transform;
            var size = root.rect.size;
            host.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                host.Clear();
                var scale = Mathf.Min(host.contentRect.width / size.x, host.contentRect.height / size.y);
                if (scale <= 0f) return;
                foreach (var img in prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    if (img.sprite == null || img.color.a <= 0f || !img.gameObject.activeSelf) continue;
                    var rt = (RectTransform)img.transform;
                    var center = (Vector2)root.InverseTransformPoint(rt.TransformPoint(rt.rect.center));
                    var x = (Vector2)root.InverseTransformVector(rt.right);
                    var y = (Vector2)root.InverseTransformVector(rt.up);
                    var w = rt.rect.width * x.magnitude * scale;
                    var h = rt.rect.height * y.magnitude * scale;
                    var e = new VisualElement { pickingMode = PickingMode.Ignore };
                    e.style.position = Position.Absolute;
                    e.style.width = w;
                    e.style.height = h;
                    e.style.left = host.contentRect.width * 0.5f + center.x * scale - w * 0.5f;
                    e.style.top = host.contentRect.height * 0.5f - center.y * scale - h * 0.5f;
                    e.style.backgroundImage = new StyleBackground(img.sprite);
                    e.style.unityBackgroundImageTintColor = tint.HasValue ? img.color * tint.Value : img.color;
                    var mirrored = x.x * y.y - x.y * y.x < 0f;
                    if (mirrored) e.style.scale = new Scale(new Vector2(-1f, 1f));
                    e.style.rotate = new Rotate(-Vector2.SignedAngle(Vector2.right, mirrored ? -x : x));
                    host.Add(e);
                }
            });
        }

        static readonly Dictionary<string, FontAsset> fonts = new Dictionary<string, FontAsset>();

        // UI Toolkit cannot draw uGUI's OS dynamic Font, so the same pick (first installed name in the chain) is loaded from its file.
        static FontAsset OsFont(string key)
        {
            var chain = Loc.T(key);
            if (fonts.TryGetValue(chain, out var cached)) return cached;
            var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
            var family = chain.Split(',').Select(n => n.Trim()).FirstOrDefault(installed.Contains);
            if (family == null) return fonts[chain] = null;
            // By name works for static faces (Malgun Gothic); variable fonts (NotoSerifKR-VF) only load from their file.
            var byName = FontAsset.CreateFontAsset(family, "Regular");
            if (byName != null) return fonts[chain] = byName;
            var file = Font.GetPathsToOSFonts()
                .Where(p => System.IO.Path.GetFileNameWithoutExtension(p).Replace(" ", "").StartsWith(family.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => System.IO.Path.GetFileNameWithoutExtension(p).Length).FirstOrDefault();
            return fonts[chain] = file == null ? null : FontAsset.CreateFontAsset(new Font(file));
        }
    }
}
