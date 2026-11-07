using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    // UI Toolkit trial of the main menu (#44, docs/dev/ui-toolkit-trial.md). Layout and look are Assets/_Project/UI/Menu.uxml and .uss;
    // this only fills text, pictures and clicks. Pictures come from ArtCatalog by name so tools/assets/scan.mjs still sees them.
    public static class MenuToolkit
    {
        public const string PrefKey = "ui.toolkitMenu";

        public static bool On => PlayerPrefs.GetInt(PrefKey, 0) == 1;

        public static UIDocument Build(Transform host, ArtCatalog art, Campaign campaign, Action demo, Action camp, int selected, Action<int> hover)
        {
            var go = new GameObject("Menu (UI Toolkit)");
            go.SetActive(false);
            go.transform.SetParent(host, false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = art.panel;
            doc.visualTreeAsset = art.menuDocument;
            // UIDocument builds its tree in OnEnable, so the assets go in before the object wakes.
            go.SetActive(true);
            var root = doc.rootVisualElement;
            root.style.unityFontDefinition = FontDefinition.FromSDFFont(OsFont("font.body"));
            foreach (var d in root.Query(className: "display").ToList()) d.style.unityFontDefinition = FontDefinition.FromSDFFont(OsFont("font.display"));

            Prefab(root.Q("crest"), art.UiPrefab(MenuScreen.Crest));
            Prefab(root.Q("break"), art.UiPrefab("Component_DarkFantasyMenus_Section_Break_02"));
            var tip = root.Q<Label>("tip");

            var s0 = GameData.Stages[0];
            var demoMeta = Card(root.Q("demo"), art, Kit.Icon.Sword, Loc.T("menu.demo"), Loc.T("menu.demoDesc"), demo, selected == 0, () => hover?.Invoke(0), tip);
            Value(demoMeta, art, Kit.Icon.Pop, s0.Pop.ToString(), Ui.Candle, Ui.Candle);
            if (s0.Silver > 0) Value(demoMeta, art, Kit.Icon.Silver, s0.Silver.ToString(), Ui.Candle, Ui.Candle);
            var campMeta = Card(root.Q("camp"), art, Kit.Icon.Home, Loc.T("menu.campaign"), Loc.T("menu.campaignDesc"), camp, selected == 1, () => hover?.Invoke(1), tip);
            Value(campMeta, art, Kit.Icon.Flag, Campaign.Stages.Count(campaign.IsCleared) + "/" + Campaign.Stages.Length, Ui.Steel, Ui.Bone);
            Value(campMeta, art, Kit.Icon.Gold, campaign.Gold.ToString(), Ui.Candle, Ui.Candle);

            Languages(root.Q("languages"));
            return doc;
        }

        static VisualElement Card(VisualElement card, ArtCatalog art, string icon, string title, string desc, Action click, bool on, Action enter, Label tip)
        {
            card.EnableInClassList("on", on);
            card.style.backgroundImage = new StyleBackground(art.Ui("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"));
            card.Q(className: "card-frame").style.backgroundImage = new StyleBackground(art.Ui("SPR_DarkFantasyMenus_Frame_Box_Medium_02"));
            card.Q(className: "card-icon").style.backgroundImage = new StyleBackground(art.Ui(icon));
            card.Q<Label>(className: "card-name").text = title;
            card.RegisterCallback<ClickEvent>(_ => click?.Invoke());
            Tip(card, desc, tip);
            card.RegisterCallback<PointerEnterEvent>(_ => enter?.Invoke());
            return card.Q(className: "card-meta");
        }

        static void Value(VisualElement row, ArtCatalog art, string icon, string value, Color iconColor, Color textColor)
        {
            var v = new VisualElement();
            v.AddToClassList("value");
            var i = new VisualElement();
            i.AddToClassList("value-icon");
            i.style.backgroundImage = new StyleBackground(art.Ui(icon));
            i.style.unityBackgroundImageTintColor = iconColor;
            var t = new Label(value);
            t.AddToClassList("value-text");
            t.style.color = textColor;
            v.Add(i);
            v.Add(t);
            row.Add(v);
        }

        // HoverTip's frame.tip: shown above the owner after the pointer rests 0.6 s.
        static void Tip(VisualElement owner, string text, Label tip)
        {
            IVisualElementScheduledItem pending = null;
            owner.RegisterCallback<PointerEnterEvent>(_ =>
            {
                pending = owner.schedule.Execute(() =>
                {
                    tip.text = text;
                    tip.style.display = DisplayStyle.Flex;
                    var b = owner.worldBound;
                    var local = tip.parent.WorldToLocal(new Vector2(b.xMin, b.yMin));
                    tip.style.left = local.x;
                    tip.style.top = local.y - 44f;
                }).StartingIn(600);
            });
            owner.RegisterCallback<PointerLeaveEvent>(_ => { pending?.Pause(); tip.style.display = DisplayStyle.None; });
        }

        static void Languages(VisualElement row)
        {
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales.OrderBy(l => l.SortOrder))
            {
                var label = new Label(locale.LocaleName);
                label.AddToClassList("lang");
                label.EnableInClassList("current", locale == Loc.Current);
                var l = locale;
                label.RegisterCallback<ClickEvent>(_ => { if (l != Loc.Current) LocalizationSettings.SelectedLocale = l; });
                row.Add(label);
            }
        }

        // A Synty INTERFACE uGUI prefab redrawn as stacked VisualElements: same sprites, tints, rects, mirroring, scaled into the host's box.
        public static void Prefab(VisualElement host, GameObject prefab)
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
                    e.style.unityBackgroundImageTintColor = img.color;
                    var mirrored = x.x * y.y - x.y * y.x < 0f;
                    if (mirrored) e.style.scale = new Scale(new Vector2(-1f, 1f));
                    e.style.rotate = new Rotate(-Vector2.SignedAngle(Vector2.right, mirrored ? -x : x));
                    host.Add(e);
                }
            });
        }

        static readonly System.Collections.Generic.Dictionary<string, FontAsset> fonts = new System.Collections.Generic.Dictionary<string, FontAsset>();

        // UI Toolkit cannot draw uGUI's OS dynamic Font, so the same pick (first installed name in the chain) is loaded from its file.
        static FontAsset OsFont(string key)
        {
            var chain = Loc.T(key);
            if (fonts.TryGetValue(chain, out var cached)) return cached;
            var installed = new System.Collections.Generic.HashSet<string>(Font.GetOSInstalledFontNames());
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
