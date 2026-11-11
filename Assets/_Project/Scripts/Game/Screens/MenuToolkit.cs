using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    // docs/ui/main-menu.md. Layout and look are Assets/_Project/UI/Menu.uxml and .uss;
    // this only fills text, pictures and clicks. Pictures come from ArtCatalog by name so tools/assets/scan.mjs still sees them.
    public static class MenuToolkit
    {
        // Crest_*_Large_01 is a demon skull; Large_03 has none, so the hunters' menu wears it (ui-style.md 5-3).
        public const string Crest = "Crest_DarkFantasyMenus_Large_03";

        public static UIDocument Build(Transform host, ArtCatalog art, Campaign campaign, RenderTexture backdrop, Action demo, Action camp, int selected, Action<int> hover)
        {
            var doc = ToolkitKit.Document(host, "Menu (UI Toolkit)", art.panel, art.menuDocument);
            var root = doc.rootVisualElement;
            if (backdrop != null) root.Q("backdrop").style.backgroundImage = new StyleBackground(Background.FromRenderTexture(backdrop));
            ToolkitKit.Prefab(root.Q("crest"), art.UiPrefab(Crest));
            ToolkitKit.Prefab(root.Q("break"), art.UiPrefab("Component_DarkFantasyMenus_Section_Break_02"));
            var tip = root.Q<Label>("tip");

            var s0 = GameData.Stages[0];
            var demoMeta = Card(root.Q("demo"), art, Kit.Icon.Sword, Loc.T("menu.demo"), Loc.T("menu.demoDesc"), demo, selected == 0, () => hover?.Invoke(0), tip);
            ToolkitKit.Value(demoMeta, art, Kit.Icon.Pop, s0.Pop.ToString(), Ui.Candle, Ui.Candle);
            if (s0.Silver > 0) ToolkitKit.Value(demoMeta, art, Kit.Icon.Silver, s0.Silver.ToString(), Ui.Candle, Ui.Candle);
            var campMeta = Card(root.Q("camp"), art, Kit.Icon.Home, Loc.T("menu.campaign"), Loc.T("menu.campaignDesc"), camp, selected == 1, () => hover?.Invoke(1), tip);
            ToolkitKit.Value(campMeta, art, Kit.Icon.Flag, Campaign.Stages.Count(campaign.IsCleared) + "/" + Campaign.Stages.Length, Ui.Steel, Ui.Bone);
            ToolkitKit.Value(campMeta, art, Kit.Icon.Gold, campaign.Gold.ToString(), Ui.Candle, Ui.Candle);

            Languages(root.Q("languages"));
            root.Q<Label>("version").text = "v" + Application.version;
            return doc;
        }

        static VisualElement Card(VisualElement card, ArtCatalog art, string icon, string title, string desc, Action click, bool on, Action enter, Label tip)
        {
            card.EnableInClassList("on", on);
            ToolkitKit.Picture(card, art.Ui("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"));
            ToolkitKit.Picture(card.Q(className: "card-frame"), art.Ui("SPR_DarkFantasyMenus_Frame_Box_Medium_02"));
            ToolkitKit.Picture(card.Q(className: "card-icon"), art.Ui(icon));
            card.Q<Label>(className: "card-name").text = title;
            UiSound.Add(card);
            card.RegisterCallback<ClickEvent>(_ => click?.Invoke());
            ToolkitKit.Tip(card, desc, tip);
            card.RegisterCallback<PointerEnterEvent>(_ => enter?.Invoke());
            return card.Q(className: "card-meta");
        }

        static void Languages(VisualElement row)
        {
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales.OrderBy(l => l.SortOrder))
            {
                var label = new Label(locale.LocaleName);
                label.AddToClassList("lang");
                label.EnableInClassList("current", locale == Loc.Current);
                var l = locale;
                UiSound.Add(label);
                label.RegisterCallback<ClickEvent>(_ => { if (l != Loc.Current) LocalizationSettings.SelectedLocale = l; });
                row.Add(label);
            }
        }
    }
}
