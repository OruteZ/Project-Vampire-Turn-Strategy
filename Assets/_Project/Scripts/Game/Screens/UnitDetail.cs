using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static VampireSpawn.Game.Screens.ToolkitKit;

namespace VampireSpawn.Game.Screens
{
    // docs/ui/unit-detail.md: the one window that shows a unit in full, drawn above whichever screen opened it. Read-only.
    // The opening screen owns the keys (I, Esc, arrows) and checks IsOpen to hold its own input back.
    public sealed class UnitDetail
    {
        const string Cancel = "ICON_DarkFantasyMenus_Menu_Cancel_01_Clean", Break = "Component_DarkFantasyMenus_Section_Break_02";

        public static bool IsOpen { get; private set; }

        readonly ArtCatalog art;
        readonly VisualElement root, window;
        readonly Label tip;
        IReadOnlyList<string> kinds;
        Func<string, DetailModel> page;
        Action<int> onPage;
        int index;

        public UnitDetail(Transform host, ArtCatalog art)
        {
            this.art = art;
            // Its own panel one order above, so it covers both the shared camp panel and the battle's zoomed copy.
            var panel = UnityEngine.Object.Instantiate(art.panel);
            panel.sortingOrder = art.panel.sortingOrder + 10f;
            var doc = Document(host, "Unit Detail (UI Toolkit)", panel, art.detailDocument);
            root = doc.rootVisualElement.Q("detail");
            window = root.Q("window");
            tip = root.Q<Label>("tip");
            var veil = root.Q("veil");
            veil.RegisterCallback<ClickEvent>(e => { if (e.target == veil) Close(); });
            root.RegisterCallback<DetachFromPanelEvent>(_ => IsOpen = false);
            IsOpen = false;
        }

        public void Show(DetailModel m)
        {
            kinds = null;
            Open(m);
        }

        // A unit type opened from a list: the arrows walk that list (unit-detail.md 12).
        public void Browse(IReadOnlyList<string> list, int at, Func<string, DetailModel> page, Action<int> onPage = null)
        {
            kinds = list;
            index = Mathf.Clamp(at, 0, list.Count - 1);
            this.page = page;
            this.onPage = onPage;
            Open(page(list[index]));
        }

        public void Step(int by)
        {
            if (!IsOpen || kinds == null) return;
            var next = Mathf.Clamp(index + by, 0, kinds.Count - 1);
            if (next == index) return;
            index = next;
            Fill(page(kinds[index]));
            onPage?.Invoke(index);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            tip.style.display = DisplayStyle.None;
            root.RemoveFromClassList("open");
        }

        void Open(DetailModel m)
        {
            Fill(m);
            if (IsOpen) return;
            IsOpen = true;
            root.AddToClassList("open");
            window.AddToClassList("enter");
            Fx.Later(window, () => window.RemoveFromClassList("enter"));
        }

        void Fill(DetailModel m)
        {
            tip.style.display = DisplayStyle.None;
            window.Clear();
            Frame(window, art, m.Edge);
            if (m.Enemy)
            {
                var decor = E(window, "overhang decor");
                Pic(decor, art, "SPR_DarkFantasyMenus_Greeble_Spikes_02_TopLeft", m.Edge, "icon spike left");
                Pic(decor, art, "SPR_DarkFantasyMenus_Greeble_Spikes_02_TopRight", m.Edge, "icon spike right");
            }
            var portrait = E(window, "detail-portrait");
            portrait.style.borderTopColor = portrait.style.borderBottomColor = portrait.style.borderLeftColor = portrait.style.borderRightColor = m.Edge;
            portrait.name = "Detail " + m.Icon;
            var icon = art.Icon(m.Icon);
            Pic(portrait, art, string.IsNullOrEmpty(icon) || art.Ui(icon) == null ? m.Fallback : icon, m.Edge);
            L(window, m.Name, "detail-name display");
            var role = E(window, "detail-role");
            L(role, m.RoleText, "detail-role-text", m.Edge);
            if (m.Pop > 0) Value(role, art, Kit.Icon.Pop, m.Pop.ToString(), Ui.Candle, Ui.Candle, Loc.T("battle.pop"), tip);
            if (m.Silver > 0) Value(role, art, Kit.Icon.Silver, m.Silver.ToString(), Ui.Candle, Ui.Candle, Loc.T("battle.silver"), tip);
            var close = IconBtn(window, art, Cancel, Close);
            close.AddToClassList("detail-close");
            L(close, "Esc", "key");
            Prefab(E(window, "detail-break"), art.UiPrefab(Break), Ui.Line);

            var left = Column("detail-left");
            var stats = Section(left, "detail.sec.stats");
            foreach (var s in m.Stats)
            {
                var row = E(stats, "detail-stat row");
                Pic(row, art, s.Icon, m.Edge);
                L(row, s.Label, "detail-stat-label");
                L(row, s.Value, "detail-stat-value", s.Low ? Ui.Blood : s.Changed ? Ui.Candle : Ui.Bone);
                if (s.Tip != null) Tip(row, Titled(s.Label, Ui.Candle, s.Tip), tip, 500, true);
            }
            if (m.States.Count > 0)
            {
                var chips = E(Section(left, "detail.sec.state"), "detail-chips");
                foreach (var s in m.States)
                {
                    var chip = E(chips, "detail-chip", true);
                    chip.name = "State " + s.Id;
                    chip.style.borderTopColor = chip.style.borderBottomColor = chip.style.borderLeftColor = chip.style.borderRightColor = s.Color;
                    E(chip, "detail-chip-dot").style.backgroundColor = s.Color;
                    L(chip, s.Name, "detail-chip-text", s.Color);
                    Tip(chip, Titled(s.Name, s.Color, s.Desc), tip, 500, true);
                }
            }
            if (m.Spawn != null)
            {
                var box = E(Section(left, "detail.sec.spawn"), "detail-spawn", true);
                var icons = new[] { Kit.Icon.Hp, Kit.Icon.Atk, Kit.Icon.Def, Kit.Icon.Mov, Kit.Icon.Rng };
                for (var i = 0; i < icons.Length; i++) Value(box, art, icons[i], m.Spawn[i].ToString(), Ui.Spawn, Ui.Spawn).pickingMode = PickingMode.Ignore;
                Tip(box, Titled(Loc.T("detail.sec.spawn"), Ui.Spawn, Loc.T("detail.ifSpawn")), tip, 500, true);
            }

            var right = Column("detail-right");
            var skills = Section(right, null);
            skills.Q<Label>().text = m.SkillsTitle;
            if (m.Skills.Count == 0) L(skills, Loc.T("detail.noSkill"), "detail-none");
            foreach (var s in m.Skills)
            {
                var row = E(skills, "detail-skill");
                Pic(E(row, "detail-skill-icon"), art, s.Icon, s.Color);
                var body = E(row, "detail-skill-body");
                var head = E(body, "detail-skill-head");
                L(head, s.Name, "detail-skill-name");
                if (s.Key != null) L(head, s.Key, "detail-skill-key");
                if (s.Tag != null) L(head, s.Tag, "detail-skill-tag");
                L(body, Marked(s.Desc), "detail-skill-desc");
            }
            if (m.Upgrades != null)
            {
                var ups = Section(right, "detail.sec.upgrades");
                foreach (var u in m.Upgrades)
                {
                    var row = E(ups, u.Own ? "detail-up" : "detail-up off", true);
                    if (u.Own) L(row, "✓", "detail-up-mark");
                    else E(row, "detail-up-ring");
                    L(row, u.Name, "detail-up-name");
                    L(row, u.Effect, "detail-up-effect");
                    if (u.Inherit) Pic(row, art, Kit.Icon.Fang, Ui.Spawn, "icon detail-up-fang");
                    Tip(row, Loc.T(u.Inherit ? "camp.inherit" : "camp.noInherit"), tip, 500, true);
                }
            }

            var foot = E(window, "detail-foot");
            L(foot, kinds != null && kinds.Count > 1 ? Loc.T("detail.browse") : "");
            L(foot, Loc.T("detail.closeHint"));
            UseFonts(window);
        }

        VisualElement Column(string side)
        {
            var view = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            view.AddToClassList("detail-col");
            view.AddToClassList(side);
            window.Add(view);
            return view;
        }

        VisualElement Section(VisualElement column, string titleKey)
        {
            var sec = E(column, "detail-sec");
            L(sec, titleKey == null ? "" : Loc.T(titleKey), "detail-sec-title");
            return sec;
        }

        static string Titled(string name, Color c, string text) => "<b><color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + name + "</color></b>\n" + text;

        // The table marks numbers and conditions with <b>; on screen they also take the brighter ink (unit-detail.md 9-4).
        static string Marked(string text) => text.Replace("<b>", "<b><color=#" + ColorUtility.ToHtmlStringRGB(Ui.Bone) + ">").Replace("</b>", "</color></b>");
    }
}
