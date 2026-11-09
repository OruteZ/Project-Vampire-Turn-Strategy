using System;
using System.Collections.Generic;
using System.Linq;
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

        public string Weapon(string key) => art != null ? art.Icon(key) : null;

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
            UiSound.Add(btn);
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

        // ui-style.md 14-3: the key sits inside the button's bottom-right corner, never past its frame.
        public static void Hotkey(RectTransform button, string key)
        {
            var box = Ui.Place(button, "Key", new Vector2(1f, 0f), new Vector2(-7f, 7f), new Vector2(key.Length > 1 ? 8f + key.Length * 8f : 18f, 18f));
            box.pivot = new Vector2(1f, 0f);
            box.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Ui.Bg(box, Ui.Alpha(Ui.Deep, 0.85f), Ui.Line, false);
            Ui.Stretch(T(box, key, 14, Ui.Muted, false, false, TextAnchor.MiddleCenter).rectTransform);
            // Long labels (English "Begin Battle") otherwise run under the key.
            if (button.Find("Text") is RectTransform label) label.offsetMax = new Vector2(-(box.sizeDelta.x + 10f), label.offsetMax.y);
        }

        // ui-style.md 9 and 14-2.
        public static class Icon
        {
            public const string Hp = "ICON_DarkFantasy_Stat_Health_01_Clean", Mov = "ICON_DarkFantasy_Stat_Speed_01_Clean", Atk = "ICON_DarkFantasy_Stat_Strength_01_Clean",
                Def = "ICON_DarkFantasy_Status_Defense_01_Clean", Rng = "ICON_DarkFantasy_Stat_Accuracy_01_Clean", Fang = "ICON_DarkFantasy_Status_Vampiric_01_Clean",
                Dead = "ICON_DarkFantasy_Status_Dead_01_Clean", BoardDef = "ICON_DarkFantasy_Status_Defense_01_Stroke", Target = "ICON_DarkFantasy_Status_Targeted_01_Stroke",
                Gold = "ICON_DarkFantasyMenus_Currency_Coin_01_Clean", Silver = "ICON_DarkFantasy_Inventory_Currency_01_Clean", Pop = "ICON_DarkFantasy_Inventory_Helmets_01_Clean",
                Boss = "ICON_DarkFantasy_Map_Monster_01_Clean", Star = "ICON_DarkFantasy_Map_Star_01_Clean", Skulls = "ICON_DarkFantasy_Map_Skull_01_Clean",
                Turn = "ICON_DarkFantasy_Status_Time_01_Clean", Lock = "ICON_DarkFantasyMenus_Menu_Lock_Closed_01_Clean", Trophy = "ICON_DarkFantasyMenus_Menu_Trophy_01_Clean",
                Retry = "ICON_DarkFantasyMenus_Menu_Refresh_01_Clean", Leave = "ICON_DarkFantasyMenus_Menu_Quit_01_Clean", Rules = "ICON_DarkFantasy_Inventory_Notes_01_Clean",
                Up = "ICON_DarkFantasy_Status_Up_01_Clean", Wait = "ICON_DarkFantasy_Status_Down_01_Clean", Flag = "ICON_DarkFantasy_Map_Flag_01_Clean", Home = "ICON_DarkFantasy_Map_Home_01_Clean", Sword = "ICON_SM_Wep_Sword_01_DarkFantasy",
                Heal = "ICON_DarkFantasy_Status_Health_01_Clean";

            public static readonly string[] All = { Hp, Mov, Atk, Def, Rng, Fang, Dead, Target, Gold, Silver, Pop, Boss, Star, Skulls, Turn, Lock, Trophy, Retry, Leave, Rules, Up, Wait, Flag, Home, Sword, Heal };
        }

        // docs/content/enemies.md 7: "Minions n, boss" then the count of each minion kind present.
        public static string EnemyList(Stage s)
        {
            var minions = s.Minions;
            var kinds = GameData.Minions.Select(m => (m, n: minions.Count(e => e.Kind == m.Key))).Where(x => x.n > 0).Select(x => Loc.Text(x.m.Name) + " " + x.n);
            return Loc.T("battle.enemyList", minions.Length, s.BossName) + " · " + string.Join(" · ", kinds);
        }

        // ui-style.md 14-3-1: [icon][number], the label lives in the tooltip.
        public RectTransform Value(Transform parent, string icon, string value, float px, Color iconColor, Color valueColor, string tip = null, bool bold = true)
        {
            var row = Ui.Row(parent, "Value", 5, 0, null, null, TextAnchor.MiddleLeft);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(Ui.Sprite(row, "Icon", S(icon), iconColor), px + 6f, px + 6f);
            T(row, value, px, valueColor, bold, false, TextAnchor.MiddleLeft);
            HoverTip.Add(row, tip, 0.3f);
            return row;
        }

        public RectTransform Stats(Transform parent, Unit u, float px, Color icon, Color value)
        {
            var row = Ui.Row(parent, "Stats", 14, 0, null, null, TextAnchor.MiddleLeft);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Value(row, Icon.Hp, u.Hp.ToString(), px, icon, value, Loc.T("unit.card.hp"));
            Value(row, Icon.Atk, u.Atk + (u.Bane > 0 ? "+" + u.Bane : ""), px, icon, value, Loc.T("unit.card.atk"));
            Value(row, Icon.Def, u.Def.ToString(), px, icon, value, Loc.T("unit.card.def"));
            Value(row, Icon.Mov, u.Mov.ToString(), px, icon, value, Loc.T("unit.card.mov"));
            Value(row, Icon.Rng, u.Rng.ToString(), px, icon, value, Loc.T("unit.card.rng"));
            return row;
        }

        // ui-style.md 14-3-2: a stage number in a ring.
        public RectTransform Badge(Transform parent, string text, float d, Color ring, Color fill, Color ink)
        {
            var rt = Ui.Node(parent, "Badge");
            rt.sizeDelta = new Vector2(d, d);
            Ui.Size(rt, d, d);
            Ui.Stretch(Ui.Sprite(rt, "Fill", S("SPR_DarkFantasyMenus_Frame_Ring_01_Background"), fill).rectTransform);
            Ui.Stretch(Ui.Sprite(rt, "Ring", S("SPR_DarkFantasyMenus_Frame_Ring_01"), ring).rectTransform, -2, -2, -2, -2);
            if (text != null) Ui.Stretch(T(rt, text, Mathf.Max(14f, d * 0.5f), ink, true, false, TextAnchor.MiddleCenter).rectTransform);
            return rt;
        }

        public Button IconBtn(Transform parent, string icon, string tip, Action click, float size = 40f, Color? tint = null, bool enabled = true)
        {
            var rt = Ui.Node(parent, "IconBtn");
            rt.sizeDelta = new Vector2(size, size);
            Ui.Size(rt, size, size);
            Ui.Bg(rt, Ui.Alpha(Ui.Ink, 0f));
            var img = Ui.Sprite(rt, "Icon", S(icon), tint ?? Ui.Muted);
            Ui.Stretch(img.rectTransform, 6, 6, 6, 6);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = enabled;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.4f, 1.4f, 1.4f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            UiSound.Add(btn);
            if (click != null) btn.onClick.AddListener(() => click());
            HoverTip.Add(rt, tip, 0.3f);
            return btn;
        }
    }
}
