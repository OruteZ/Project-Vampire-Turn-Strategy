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
            // Crest_*_Large_01 is a demon skull; Large_03 has none, so the hunters' menu wears it (ui-style.md 5-3).
            kit.Prefab(page, Crest, new Vector2(0.5f, 1f), new Vector2(0f, -120f), 160f, Ui.Steel);
            var title = T(Ui.Place(page, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -304f), new Vector2(1400f, 72f)), "PROJECT VAMPIRE SPAWN", 56, Ui.Bone, true, true, TextAnchor.MiddleCenter);
            Ui.Stretch(title.rectTransform);
            title.gameObject.AddComponent<Outline>().effectColor = Ui.Ink;
            var tag = Kit.Wrap(T(Ui.Place(page, "Tagline", new Vector2(0.5f, 1f), new Vector2(0f, -392f), new Vector2(900f, 52f)), Loc.T("menu.tagline"), 17, Ui.Muted));
            Ui.Stretch(tag.rectTransform);
            var brk = Ui.Place(page, "Break", new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(600f, 24f));
            if (kit.Prefab(brk, "Component_DarkFantasyMenus_Section_Break_02", new Vector2(0.5f, 0.5f), Vector2.zero, 600f, Ui.Line) == null)
                Ui.Bg(Ui.Place(brk, "Line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 1f)), Ui.Line, null, false);

            Card(page, kit, new Vector2(-226f, -540f), Loc.T("menu.demo"), Loc.T("menu.demoDesc"), CampaignScreen.Resources(GameData.Stages[0]), demo, selected == 0, () => hover?.Invoke(0));
            Card(page, kit, new Vector2(226f, -540f), Loc.T("menu.campaign"), Loc.T("menu.campaignDesc"), CampMeta(campaign), camp, selected == 1, () => hover?.Invoke(1));

            Languages(page);
        }

        static void Card(RectTransform page, Kit kit, Vector2 pos, string title, string desc, string meta, Action click, bool on, Action enter)
        {
            var rt = Ui.Place(page, "Choice " + title, new Vector2(0.5f, 1f), pos, new Vector2(420f, 220f));
            rt.pivot = new Vector2(0.5f, 1f);
            kit.Frame(rt, on ? Ui.Steel : Ui.SteelDim);
            var back = rt.GetComponentInChildren<Image>();
            back.raycastTarget = true;
            if (on) back.color *= 1.1f;
            var col = Ui.Col(rt, "Body", 8, 32);
            Ui.Stretch(col);
            col.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            T(col, title, 32, Ui.Bone, true, true, TextAnchor.UpperLeft);
            Kit.Wrap(T(col, desc, 17, Ui.Muted, false, false, TextAnchor.UpperLeft));
            var m = T(Ui.Place(rt, "Meta", new Vector2(0f, 0f), new Vector2(32f, 28f), new Vector2(356f, 24f)), meta, 20, Ui.Candle, true, false, TextAnchor.LowerLeft);
            ((RectTransform)m.transform.parent).pivot = Vector2.zero;
            Ui.Stretch(m.rectTransform);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = back;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.97f, 0.97f, 0.97f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            if (click != null) btn.onClick.AddListener(() => click());
            OnHover.Add(rt, enter);
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
                t.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (l != Loc.Current) LocalizationSettings.SelectedLocale = l; });
            }
        }
    }
}
