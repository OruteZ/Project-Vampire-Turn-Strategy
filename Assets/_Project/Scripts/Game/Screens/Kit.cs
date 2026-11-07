using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    public enum BtnKind { Primary, Normal, Danger, DangerOn, Ghost }

    // Shared pieces in the docs/art/ui-style.md tokens: text sizes in px, frame.*, btn.* and unit weapon icons.
    public sealed class Kit
    {
        public const float FramePpu = 9f;

        readonly ArtCatalog art;

        public Kit(ArtCatalog art) => this.art = art;

        public Sprite S(string name) => art != null ? art.Ui(name) : null;

        public static Text T(Transform p, string s, float px, Color c, bool bold = false, bool display = false, TextAnchor a = TextAnchor.UpperLeft)
        {
            var t = Ui.Text(p, s, px / Ui.Base, c, bold, display, a);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        public static Text Wrap(Text t) { t.horizontalOverflow = HorizontalWrapMode.Wrap; return t; }

        public static readonly Dictionary<UnitKind, string> Weapon = new Dictionary<UnitKind, string>
        {
            [UnitKind.PSpear] = "ICON_SM_Wep_Pitchfork_01_DarkFantasy", [UnitKind.PArcher] = "ICON_SM_Wep_Crossbow_01_Arrow_01_DarkFantasy",
            [UnitKind.Sword] = "ICON_SM_Wep_Sword_01_DarkFantasy", [UnitKind.Archer] = "ICON_SM_Wep_Crossbow_01_Arrow_01_DarkFantasy",
            [UnitKind.Knight] = "ICON_SM_Wep_Shield_Heater_01_Dungeon", [UnitKind.Hunter] = "ICON_SM_Wep_Crossbow_01_DarkFantasy",
        };

        public void Frame(RectTransform rt, Color edge, float ppu = FramePpu) =>
            Ui.Frame(rt, S("SPR_DarkFantasyMenus_Frame_Box_Medium_02"), S("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"), edge, ppu);

        public Button Btn(Transform parent, string label, Action click, BtnKind kind, Vector2 size, bool enabled = true, string tip = null)
        {
            var rt = Ui.Node(parent, "Button " + label);
            rt.sizeDelta = size;
            Ui.Size(rt, size.x, size.y);
            Image target;
            if (kind == BtnKind.Ghost) target = Ui.Bg(rt, Ui.Alpha(Ui.Ink, 0f));
            else
            {
                var fill = kind == BtnKind.Primary ? Ui.Steel : Ui.Alpha(Ui.Panel, 0.92f);
                var edge = kind == BtnKind.Primary ? Ui.Bone : kind == BtnKind.Danger ? Ui.BloodDim : kind == BtnKind.DangerOn ? Ui.Blood : Ui.SteelDim;
                target = Ui.Sprite(rt, "Fill", S("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"), fill, FramePpu);
                Ui.Stretch(target.rectTransform);
                target.raycastTarget = true;
                Ui.Stretch(Ui.Sprite(rt, "Frame", S("SPR_DarkFantasyMenus_Frame_Box_Medium_02"), edge, FramePpu).rectTransform);
            }
            var color = kind == BtnKind.Primary ? Ui.Ink : kind == BtnKind.Ghost ? Ui.Muted : kind is BtnKind.Danger or BtnKind.DangerOn ? Ui.Blood : Ui.Bone;
            if (!enabled) color = Ui.Dim;
            Ui.Stretch(T(rt, label, 17, color, kind == BtnKind.Primary, false, TextAnchor.MiddleCenter).rectTransform);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = target;
            btn.interactable = enabled;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.97f, 0.97f, 0.97f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            if (click != null) btn.onClick.AddListener(() => click());
            if (tip != null) HoverTip.Add(rt, tip, 0f);
            return btn;
        }

        // Kit decorations are prefabs; scale one to the wanted width and keep it out of layout and raycasts.
        public RectTransform Prefab(Transform parent, string name, Vector2 anchor, Vector2 pos, float width, Color tint)
        {
            var prefab = art != null ? art.UiPrefab(name) : null;
            if (prefab == null) return null;
            var rt = (RectTransform)UnityEngine.Object.Instantiate(prefab, parent, false).transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            var w = rt.rect.width > 1f ? rt.rect.width : 100f;
            rt.localScale = Vector3.one * (width / w);
            foreach (var g in rt.GetComponentsInChildren<Graphic>(true)) { g.raycastTarget = false; g.color *= tint; }
            if (!rt.TryGetComponent<LayoutElement>(out var le)) le = rt.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            return rt;
        }

        public static void Hotkey(RectTransform button, string key)
        {
            var box = Ui.Place(button, "Key", new Vector2(1f, 0f), new Vector2(4f, -4f), new Vector2(key.Length > 1 ? 46f : 18f, 18f));
            Ui.Bg(box, Ui.Deep, Ui.Line, false);
            Ui.Stretch(T(box, key, 14, Ui.Muted, false, false, TextAnchor.MiddleCenter).rectTransform);
        }
    }
}
