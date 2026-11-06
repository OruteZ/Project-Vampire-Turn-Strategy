using System;
using UnityEngine;
using UnityEngine.UI;

namespace VampireSpawn.Game.Screens
{
    public enum Look { Normal, Primary, Ghost, Danger, DangerOn, Choice, Roster, RosterOn }

    // uGUI built from code so screens are re-rendered like the prototype's innerHTML templates. Colours are the prototype's CSS variables.
    public static class Ui
    {
        public static readonly Color Ink = C(0x15131a), Deep = C(0x0f0e13), Panel = C(0x1d1a23), Panel2 = C(0x25212d), Line = C(0x3a3442),
            HexLine = C(0x2e2a36), Bone = C(0xebe3d2), Muted = C(0xa49bab), Candle = C(0xe3a84b), Blood = C(0xc8304a), Spawn = C(0xa650c4),
            Steel = C(0x8fb0cc), Peasant = C(0xb89c72), Knight = C(0xd9dde4), Hunter = C(0x86b8a6), Sun = C(0xeadcae);

        public const int Base = 15;

        static Font body, display;

        // Same fallback chains as the prototype's --f-body and --f-display; the OS supplies whichever is installed.
        public static Font Body => body != null ? body : body = Font.CreateDynamicFontFromOSFont(new[] { "IBM Plex Sans KR", "Apple SD Gothic Neo", "Malgun Gothic", "Noto Sans CJK KR", "Arial" }, Base);
        public static Font Display => display != null ? display : display = Font.CreateDynamicFontFromOSFont(new[] { "Nanum Myeongjo", "Noto Serif KR", "Batang", "AppleMyungjo", "Malgun Gothic" }, Base);

        static Color C(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        public static Color Alpha(Color c, float a) { c.a = a; return c; }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static Image Bg(RectTransform rt, Color color, Color? border = null, bool raycast = true)
        {
            if (!rt.TryGetComponent<Image>(out var img)) img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (border is Color b)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = b;
                o.effectDistance = new Vector2(1f, -1f);
            }
            return img;
        }

        static T Group<T>(Transform parent, string name, float spacing, int pad, Color? bg, Color? border) where T : HorizontalOrVerticalLayoutGroup
        {
            var rt = Node(parent, name);
            if (bg is Color c) Bg(rt, c, border);
            var g = rt.gameObject.AddComponent<T>();
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = typeof(T) == typeof(VerticalLayoutGroup);
            g.childForceExpandHeight = false;
            return g;
        }

        public static RectTransform Col(Transform parent, string name, float spacing = 0, int pad = 0, Color? bg = null, Color? border = null) =>
            (RectTransform)Group<VerticalLayoutGroup>(parent, name, spacing, pad, bg, border).transform;

        public static RectTransform Row(Transform parent, string name, float spacing = 0, int pad = 0, Color? bg = null, Color? border = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var g = Group<HorizontalLayoutGroup>(parent, name, spacing, pad, bg, border);
            g.childAlignment = align;
            return (RectTransform)g.transform;
        }

        public static RectTransform Card(Transform parent, string name) => Col(parent, name, 6, 12, Panel, Line);

        public static LayoutElement Size(Component c, float width = -1, float height = -1, float flexW = -1, float flexH = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) le.preferredWidth = width;
            if (height >= 0) le.preferredHeight = height;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        public static Text Text(Transform parent, string s, float size = 1f, Color? color = null, bool bold = false, bool display = false, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var rt = Node(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = display ? Display : Body;
            t.fontSize = Mathf.RoundToInt(Base * size);
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = color ?? Bone;
            t.alignment = anchor;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.lineSpacing = 1.1f;
            t.text = s;
            return t;
        }

        public static Button Button(Transform parent, string label, Action click, Look look = Look.Normal, bool small = false, bool interactable = true)
        {
            var fill = look switch
            {
                Look.Primary => Candle,
                Look.Ghost => Alpha(Panel2, 0f),
                Look.DangerOn => Blood,
                Look.Choice or Look.Roster or Look.RosterOn => Panel,
                _ => Panel2,
            };
            var edge = look switch
            {
                Look.Primary => Candle,
                Look.Danger or Look.DangerOn => Blood,
                Look.RosterOn => Candle,
                _ => Line,
            };
            var pad = small ? 6 : 9;
            var rt = Row(parent, "Button " + label, 8, pad, fill, edge, TextAnchor.MiddleCenter);
            rt.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(small ? 12 : 16, small ? 12 : 16, pad, pad);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = rt.GetComponent<Image>();
            b.interactable = interactable;
            var colors = b.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            b.colors = colors;
            if (click != null) b.onClick.AddListener(() => click());
            if (label != null)
            {
                var t = Text(rt, label, small ? 0.85f : 1f, look == Look.Primary ? Deep : Bone, true, false, TextAnchor.MiddleCenter);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            return b;
        }

        public static RectTransform Scroll(Transform parent, string name, out ScrollRect scroll)
        {
            var view = Node(parent, name);
            Bg(view, Alpha(Ink, 0f));
            view.gameObject.AddComponent<RectMask2D>();
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            var content = Col(view, "Content", 10);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = view;
            scroll.content = content;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(child); else UnityEngine.Object.DestroyImmediate(child);
            }
        }

        public static Color TierColor(VampireSpawn.Core.Tier tier) => tier switch
        {
            VampireSpawn.Core.Tier.Peasant => Peasant,
            VampireSpawn.Core.Tier.Knight => Knight,
            VampireSpawn.Core.Tier.Hunter => Hunter,
            _ => Steel,
        };

        // Prototype .rg: the unit glyph in a tier-coloured badge.
        public static Text Glyph(Transform parent, VampireSpawn.Core.UnitType t, float size = 34f)
        {
            var box = Row(parent, "Glyph", 0, 0, Panel2, TierColor(t.Tier), TextAnchor.MiddleCenter);
            Size(box, size, size);
            var g = Text(box, Loc.Text(t.Glyph), 1f, TierColor(t.Tier), true, true, TextAnchor.MiddleCenter);
            return g;
        }
    }
}
