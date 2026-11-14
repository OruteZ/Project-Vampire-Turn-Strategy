using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace VampireSpawn.Game.Screens
{
    public enum BtnKind { Primary, Normal, Danger, DangerOn, Ghost }

    // Shared UI Toolkit pieces (docs/dev/ui-toolkit-trial.md 4, 6). Shared classes live in Assets/_Project/UI/Theme.uss; colours are the Ui tokens.
    public static class ToolkitKit
    {
        public const string FrameBox = "SPR_DarkFantasyMenus_Frame_Box_Medium_02", FrameBack = "SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background",
            Ring = "SPR_DarkFantasyMenus_Frame_Ring_01", RingBack = "SPR_DarkFantasyMenus_Frame_Ring_01_Background";

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
            // The root and the template container cover the whole screen; left pickable they make every click "over UI".
            doc.rootVisualElement.pickingMode = PickingMode.Ignore;
            foreach (var c in doc.rootVisualElement.Children()) c.pickingMode = PickingMode.Ignore;
            UseFonts(doc.rootVisualElement);
            // With Input System as the only input backend, runtime panels get pointer events only through an EventSystem (measured: no clicks without it).
            if (UnityEngine.EventSystems.EventSystem.current == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule)).transform.SetParent(host, false);
            return doc;
        }

        // ui-style.md 15-1 on Panel.asset (Scale With Screen Size, Expand, 1920x1080): the panel never shrinks below 1920x1080, the spare axis grows.
        // Computed, not read from layout, so the first frame (before UI Toolkit lays out) already knows it.
        public static Vector2 PanelSize(float zoom = 1f)
        {
            var scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f) * zoom;
            return scale > 0f ? new Vector2(Screen.width / scale, Screen.height / scale) : new Vector2(1920f, 1080f) / zoom;
        }

        // A runtime copy lets one screen draw smaller without touching the shared Panel.asset; a bigger reference resolution shrinks every px.
        public static PanelSettings Zoomed(PanelSettings panel, float zoom)
        {
            var p = UnityEngine.Object.Instantiate(panel);
            p.referenceResolution = new Vector2Int(Mathf.RoundToInt(panel.referenceResolution.x / zoom), Mathf.RoundToInt(panel.referenceResolution.y / zoom));
            return p;
        }

        // True when the mouse is over an element that takes clicks; the screen-wide root and layers are Ignore.
        public static bool PointerOverUi(UIDocument doc)
        {
            var panel = doc != null ? doc.rootVisualElement.panel : null;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (panel == null || mouse == null) return false;
            var p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(mouse.position.x.ReadValue(), Screen.height - mouse.position.y.ReadValue()));
            var hit = panel.Pick(p);
            return hit != null && hit.pickingMode == PickingMode.Position;
        }

        public static void UseFonts(VisualElement root)
        {
            root.style.unityFontDefinition = FontDefinition.FromSDFFont(OsFont("font.body"));
            var display = FontDefinition.FromSDFFont(OsFont("font.display"));
            foreach (var d in root.Query(className: "display").ToList()) d.style.unityFontDefinition = display;
        }

        public static VisualElement E(VisualElement parent, string classes = null, bool pick = false)
        {
            var e = new VisualElement { pickingMode = pick ? PickingMode.Position : PickingMode.Ignore };
            Classes(e, classes);
            parent?.Add(e);
            return e;
        }

        public static Label L(VisualElement parent, string text, string classes = null, Color? color = null)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            Classes(l, classes);
            if (color.HasValue) l.style.color = color.Value;
            parent?.Add(l);
            return l;
        }

        static void Classes(VisualElement e, string classes)
        {
            if (classes == null) return;
            foreach (var c in classes.Split(' ')) if (c.Length > 0) e.AddToClassList(c);
        }

        public static void Picture(VisualElement e, Sprite s, Color? tint = null)
        {
            if (e == null) return;
            e.style.backgroundImage = new StyleBackground(s);
            if (tint.HasValue) e.style.unityBackgroundImageTintColor = tint.Value;
        }

        public static VisualElement Pic(VisualElement parent, ArtCatalog art, string sprite, Color tint, string classes = "icon")
        {
            var e = E(parent, classes);
            Picture(e, art.Ui(sprite), tint);
            return e;
        }

        // ui-style.md 5 frame.panel: the box background in panel tint; the frame keeps the pack's own metal colours with only a 20% hint of the edge colour (#118).
        public static void Frame(VisualElement panel, ArtCatalog art, Color edge)
        {
            Picture(panel, art.Ui(FrameBack), Ui.Alpha(Ui.Panel, 0.92f));
            var frame = panel.Q(className: "panel-frame");
            if (frame == null) { frame = E(null, "panel-frame"); panel.Insert(0, frame); }
            Picture(frame, art.Ui(FrameBox), Edge(edge));
        }

        public static Color Edge(Color c) => Color.Lerp(Color.white, c, 0.2f);

        public static VisualElement Chip(VisualElement parent, ArtCatalog art, Color edge, Sprite icon, Color iconColor, float iconSize = 36f)
        {
            var chip = E(parent, "chip", true);
            Picture(chip, art.Ui(FrameBack), Ui.Alpha(Ui.Panel, 0.92f));
            Picture(E(chip, "chip-frame"), art.Ui(FrameBox), Edge(edge));
            var i = E(chip, "chip-icon");
            i.style.width = iconSize;
            i.style.height = iconSize;
            Picture(i, icon, iconColor);
            return chip;
        }

        // ui-style.md 14-3-2: a number in a ring.
        public static VisualElement RingBadge(VisualElement parent, ArtCatalog art, string text, float d, Color ring, Color fill, Color ink)
        {
            var b = E(parent, "ring-badge");
            b.style.width = d;
            b.style.height = d;
            Picture(E(b, "ring-fill"), art.Ui(RingBack), fill);
            Picture(E(b, "ring"), art.Ui(Ring), ring);
            if (text != null) L(b, text, "ring-text", ink).style.fontSize = Mathf.Max(13f, d * 0.45f);
            return b;
        }

        public static VisualElement Btn(VisualElement parent, ArtCatalog art, string text, BtnKind kind, string key = null, Action click = null, bool enabled = true)
        {
            var b = E(parent, "btn", true);
            E(b, "btn-frame");
            L(b, text, "btn-text");
            if (key != null) L(b, key, "key");
            Style(b, art, kind);
            Click(b, click, enabled);
            return b;
        }

        // ui-style.md 14-3: the key sits inside the button's bottom-right corner; the label keeps clear of it.
        public static void KeepClearOfKey(VisualElement b, string key)
        {
            b.AddToClassList("has-key");
            b.Q<Label>(className: "btn-text").style.right = (key.Length > 1 ? 8f + key.Length * 8f : 18f) + 10f;
        }

        public static VisualElement IconBtn(VisualElement parent, ArtCatalog art, string icon, Action click, bool enabled = true)
        {
            var b = E(parent, "icon-btn", true);
            Picture(E(b, "icon"), art.Ui(icon));
            Click(b, click, enabled);
            return b;
        }

        public static void Click(VisualElement b, Action click, bool enabled = true)
        {
            b.EnableInClassList("disabled", !enabled);
            UiSound.Add(b);
            if (click != null) b.RegisterCallback<ClickEvent>(_ => { if (!b.ClassListContains("disabled")) click(); });
        }

        // ui-style.md 3 button table: fill and frame by kind; ghost draws neither.
        public static void Style(VisualElement b, ArtCatalog art, BtnKind kind)
        {
            foreach (var k in new[] { "ghost", "normal", "primary", "danger" }) b.RemoveFromClassList(k);
            b.AddToClassList(kind == BtnKind.Ghost ? "ghost" : kind == BtnKind.Primary ? "primary" : kind is BtnKind.Danger or BtnKind.DangerOn ? "danger" : "normal");
            var frame = b.Q(className: "btn-frame");
            if (kind == BtnKind.Ghost)
            {
                b.style.backgroundImage = StyleKeyword.None;
                if (frame != null) frame.style.backgroundImage = StyleKeyword.None;
                return;
            }
            Picture(b, art.Ui(FrameBack), kind == BtnKind.Primary ? Ui.SteelDim : Ui.Alpha(Ui.Panel, 0.92f));
            if (frame != null) Picture(frame, art.Ui(FrameBox), kind == BtnKind.DangerOn ? Ui.Blood : Edge(kind == BtnKind.Danger ? Ui.Blood : Ui.Steel));
        }

        // ui-style.md 14-3-1: [icon][number], the label lives in the tooltip.
        public static VisualElement Value(VisualElement row, ArtCatalog art, string icon, string value, Color iconColor, Color textColor, string tip = null, Label tipLabel = null)
        {
            var v = E(row, "value", true);
            Picture(E(v, "value-icon"), art.Ui(icon), iconColor);
            L(v, value, "value-text", textColor);
            if (tip != null && tipLabel != null) Tip(v, tip, tipLabel, 300);
            return v;
        }

        // frame.tip: shown above the owner after the pointer rests; kept on screen.
        public static void Tip(VisualElement owner, string text, Label tip, long delayMs = 600)
        {
            if (!string.IsNullOrEmpty(text)) Tip(owner, () => text, tip, delayMs);
        }

        // text is read when the tip opens, so a screen that refills in place can change it without binding again.
        public static void Tip(VisualElement owner, Func<string> text, Label tip, long delayMs = 600)
        {
            if (tip == null) return;
            if (owner.pickingMode == PickingMode.Ignore) owner.pickingMode = PickingMode.Position;
            IVisualElementScheduledItem pending = null;
            owner.RegisterCallback<PointerEnterEvent>(_ =>
            {
                pending = owner.schedule.Execute(() =>
                {
                    var now = text();
                    if (string.IsNullOrEmpty(now)) return;
                    tip.text = now;
                    tip.style.display = DisplayStyle.Flex;
                    tip.BringToFront();
                    var b = owner.worldBound;
                    var local = tip.parent.WorldToLocal(new Vector2(b.xMin, b.yMin));
                    var room = tip.parent.contentRect;
                    tip.style.left = Mathf.Clamp(local.x, 8f, Mathf.Max(8f, room.width - 440f));
                    tip.style.top = local.y > 60f ? local.y - 44f : local.y + b.height + 8f;
                }).StartingIn(delayMs);
            });
            owner.RegisterCallback<PointerLeaveEvent>(_ => { pending?.Pause(); tip.style.display = DisplayStyle.None; });
            owner.RegisterCallback<DetachFromPanelEvent>(_ => { pending?.Pause(); tip.style.display = DisplayStyle.None; });
        }

        // A Synty INTERFACE uGUI prefab redrawn as stacked VisualElements: same sprites, tints, rects, mirroring, scaled into the host's box.
        // The prefabs are only read as data; nothing of uGUI is drawn.
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

        static Texture2D wideFade;

        // stage-dressing.md 1-6: the world runs behind the HUD, so the outer quarter darkens to keep HUD text readable.
        public static void Shade(VisualElement e)
        {
            if (e == null) return;
            e.style.backgroundImage = new StyleBackground(wideFade ??= FadeTexture(64, (u, v) => Mathf.Clamp01(1f - Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) / 0.25f)));
            e.style.unityBackgroundImageTintColor = Ui.Alpha(Ui.Ink, 0.55f);
            e.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Length) { x = Length.Percent(100), y = Length.Percent(100) };
        }

        public static Texture2D FadeTexture(int n, Func<float, float, float> alpha)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var a = alpha((x + 0.5f) / n, (y + 0.5f) / n);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return tex;
        }

        static readonly Dictionary<string, FontAsset> fonts = new Dictionary<string, FontAsset>();

        // The language's chain (Text table font.body / font.display): bundled fonts in Resources/Fonts first, then installed OS fonts; the first that resolves draws, the rest are its fallbacks for missing glyphs.
        static FontAsset OsFont(string key)
        {
            var chain = Loc.T(key);
            if (fonts.TryGetValue(chain, out var cached)) return cached;
            var found = chain.Split(',').Select(n => Resolve(n.Trim())).Where(f => f != null).ToList();
            if (found.Count == 0) return fonts[chain] = null;
            found[0].fallbackFontAssetTable = found.Skip(1).ToList();
            return fonts[chain] = found[0];
        }

        static FontAsset Resolve(string name)
        {
            var bundled = Resources.Load<Font>("Fonts/" + name);
            if (bundled != null) return FontAsset.CreateFontAsset(bundled);
            if (!Font.GetOSInstalledFontNames().Contains(name)) return null;
            // By name works for static faces (Malgun Gothic); variable fonts (NotoSerifKR-VF) only load from their file.
            var byName = FontAsset.CreateFontAsset(name, "Regular");
            if (byName != null) return byName;
            var file = Font.GetPathsToOSFonts()
                .Where(p => System.IO.Path.GetFileNameWithoutExtension(p).Replace(" ", "").StartsWith(name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => System.IO.Path.GetFileNameWithoutExtension(p).Length).FirstOrDefault();
            return file == null ? null : FontAsset.CreateFontAsset(new Font(file));
        }
    }
}
