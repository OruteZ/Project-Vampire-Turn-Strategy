using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    // docs/ui/main-menu.md. Sizes are 1920x1080 px.
    public static class MenuScreen
    {
        public const string Crest = "Crest_DarkFantasyMenus_Large_03";

        public static string CampMeta(Campaign c) =>
            Loc.T("menu.campMeta", Campaign.Stages.Count(c.IsCleared), Campaign.Stages.Length, c.Gold);

        static Text T(Transform p, string s, float px, Color c, bool bold = false, bool display = false, TextAnchor a = TextAnchor.UpperCenter) => Kit.T(p, s, px, c, bold, display, a);

        // selected: 0 demo, 1 campaign (main-menu.md 11).
        public static void Build(RectTransform root, Campaign campaign, Action demo, Action camp, ArtCatalog art = null, int selected = 1, Action<int> hover = null)
        {
            var kit = new Kit(art);
            var page = Ui.Node(root, "Menu");
            Ui.Stretch(page);
            // main-menu.md 후속: the 1080-tall layout hangs from one centre anchor, so taller canvases (5:4) keep it in the middle.
            var block = Ui.Place(page, "Block", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            // Crest_*_Large_01 is a demon skull; Large_03 has none, so the hunters' menu wears it (ui-style.md 5-3).
            kit.Prefab(block, Crest, new Vector2(0.5f, 1f), new Vector2(0f, -120f), 160f, Ui.Steel);
            var title = T(Ui.Place(block, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -304f), new Vector2(1400f, 72f)), "PROJECT VAMPIRE SPAWN", 56, Ui.Bone, true, true, TextAnchor.MiddleCenter);
            Ui.Stretch(title.rectTransform);
            title.gameObject.AddComponent<Outline>().effectColor = Ui.Ink;
            var brk = Ui.Place(block, "Break", new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(600f, 24f));
            if (kit.Prefab(brk, "Component_DarkFantasyMenus_Section_Break_02", new Vector2(0.5f, 0.5f), Vector2.zero, 600f, Ui.Line) == null)
                Ui.Bg(Ui.Place(brk, "Line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 1f)), Ui.Line, null, false);

            var s0 = GameData.Stages[0];
            var demoCard = Card(block, kit, new Vector2(-200f, -460f), Kit.Icon.Sword, Loc.T("menu.demo"), Loc.T("menu.demoDesc"), demo, selected == 0, () => hover?.Invoke(0));
            kit.Value(demoCard, Kit.Icon.Pop, s0.Pop.ToString(), 20, Ui.Candle, Ui.Candle);
            if (s0.Silver > 0) kit.Value(demoCard, Kit.Icon.Silver, s0.Silver.ToString(), 20, Ui.Candle, Ui.Candle);
            var campCard = Card(block, kit, new Vector2(200f, -460f), Kit.Icon.Home, Loc.T("menu.campaign"), Loc.T("menu.campaignDesc"), camp, selected == 1, () => hover?.Invoke(1));
            kit.Value(campCard, Kit.Icon.Flag, Campaign.Stages.Count(campaign.IsCleared) + "/" + Campaign.Stages.Length, 20, Ui.Steel, Ui.Bone);
            kit.Value(campCard, Kit.Icon.Gold, campaign.Gold.ToString(), 20, Ui.Candle, Ui.Candle);

            Languages(page);
        }

        // ui-style.md 14-4: a big icon and a name; the description is the card's tooltip. Returns the row for its icon values.
        static RectTransform Card(RectTransform page, Kit kit, Vector2 pos, string icon, string title, string desc, Action click, bool on, Action enter)
        {
            var rt = Ui.Place(page, "Choice " + title, new Vector2(0.5f, 1f), pos, new Vector2(360f, 280f));
            rt.pivot = new Vector2(0.5f, 1f);
            kit.Frame(rt, on ? Ui.Steel : Ui.SteelDim);
            var back = rt.GetComponentInChildren<Image>();
            back.raycastTarget = true;
            if (on) back.color *= 1.1f;
            var pic = Ui.Sprite(rt, "Picture", kit.S(icon), on ? Ui.Steel : Ui.Muted);
            var pr = pic.rectTransform;
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 1f);
            pr.anchoredPosition = new Vector2(0f, -36f);
            pr.sizeDelta = new Vector2(104f, 104f);
            var name = T(Ui.Place(rt, "Name", new Vector2(0.5f, 1f), new Vector2(0f, -156f), new Vector2(320f, 44f)), title, 32, Ui.Bone, true, true, TextAnchor.MiddleCenter);
            ((RectTransform)name.transform.parent).pivot = new Vector2(0.5f, 1f);
            Ui.Stretch(name.rectTransform);
            var meta = Ui.Row(rt, "Meta", 20, 0, null, null, TextAnchor.MiddleCenter);
            meta.anchorMin = new Vector2(0f, 0f);
            meta.anchorMax = new Vector2(1f, 0f);
            meta.pivot = new Vector2(0.5f, 0f);
            meta.offsetMin = new Vector2(24f, 26f);
            meta.offsetMax = new Vector2(-24f, 60f);
            meta.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            HoverTip.Add(rt, desc, 0.6f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = back;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.97f, 0.97f, 0.97f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            UiSound.Add(btn);
            if (click != null) btn.onClick.AddListener(() => click());
            OnHover.Add(rt, enter);
            return meta;
        }

        // docs/ui/localization.md 6~8 and main-menu.md 12: top-right row, one entry per locale, switches at once; the package saves the choice.
        static void Languages(RectTransform page)
        {
            var row = Ui.Row(page, "Languages", 16, 0, null, null, TextAnchor.UpperRight);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(1f, 1f);
            row.anchoredPosition = new Vector2(-24f, -24f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            row.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales.OrderBy(l => l.SortOrder))
            {
                var current = locale == Loc.Current;
                var cell = Ui.Col(row, "Lang " + locale.Identifier.Code, 2);
                var t = T(cell, locale.LocaleName, 14, current ? Ui.Bone : Ui.Dim, current);
                t.raycastTarget = true;
                if (current) Ui.Size(Ui.Bg(Ui.Node(cell, "Underline"), Ui.Steel, null, false), -1, 2);
                var l = locale;
                UiSound.Add(t.gameObject.AddComponent<Button>()).onClick.AddListener(() => { if (l != Loc.Current) LocalizationSettings.SelectedLocale = l; });
            }
        }
    }
}
