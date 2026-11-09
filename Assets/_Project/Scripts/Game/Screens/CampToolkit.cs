using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    // docs/ui/stage-select.md 2, 3 and docs/ui/upgrade.md 3-6 on UI Toolkit (stage-select.md 10). Layout is Assets/_Project/UI/Camp.uxml/.uss;
    // Build wires the fixed controls once, Fill redraws what the CampNav state shows. Every input goes out as a CampKey.
    public sealed class CampToolkit
    {
        const string Ring = ToolkitKit.Ring, RingBack = ToolkitKit.RingBack, Cancel = "ICON_DarkFantasyMenus_Menu_Cancel_01_Clean",
            Break = "Component_DarkFantasyMenus_Section_Break_02", Crest = "Crest_DarkFantasyMenus_Large_01";

        public readonly UIDocument Doc;
        readonly ArtCatalog art;
        readonly VisualElement root;
        readonly Label tip;
        readonly Action<CampInput> send;

        public VisualElement Root => root;

        CampToolkit(UIDocument doc, ArtCatalog art, Action<CampInput> send)
        {
            Doc = doc;
            this.art = art;
            this.send = send;
            root = doc.rootVisualElement.Q("camp");
            tip = root.Q<Label>("tip");
        }

        public static CampToolkit Build(Transform host, ArtCatalog art, Func<CampKey, CampInput> input, Action<CampInput> send)
        {
            var doc = ToolkitKit.Document(host, "Camp (UI Toolkit)", art.panel, art.campDocument);
            var ui = new CampToolkit(doc, art, send);
            var r = ui.root;
            ui.Wire(r.Q("menu"), BtnKind.Ghost, () => send(input(CampKey.Esc)));
            ToolkitKit.Picture(r.Q("menu").Q(className: "icon"), art.Ui(Kit.Icon.Leave));
            ToolkitKit.Tip(r.Q("menu"), Loc.T("camp.menu"), ui.tip, 300);
            ui.Wire(r.Q("reset"), BtnKind.Ghost, () => send(input(CampKey.Reset)));
            ui.Wire(r.Q("barracks"), BtnKind.Normal, () => send(input(CampKey.Barracks)));
            ToolkitKit.Picture(r.Q("barracks").Q(className: "icon"), art.Ui(Kit.Icon.Up));
            foreach (var close in new[] { r.Q("info-close"), r.Q("bk-close") })
            {
                ui.Wire(close, BtnKind.Ghost, () => send(input(CampKey.Close)));
                ToolkitKit.Picture(close.Q(className: "icon"), art.Ui(Cancel));
                ToolkitKit.Tip(close, Loc.T("camp.toMap"), ui.tip, 300);
            }
            foreach (var p in new[] { r.Q("info"), r.Q("barracks-panel") }) ToolkitKit.Frame(p, art, Ui.Steel);
            ToolkitKit.Prefab(r.Q("info-break"), art.UiPrefab(Break), Ui.Line);
            ToolkitKit.Prefab(r.Q("bk-break"), art.UiPrefab(Break), Ui.Line);
            ToolkitKit.Picture(r.Q(className: "bk-icon"), art.Ui(Kit.Icon.Up), Ui.Steel);
            ToolkitKit.Frame(r.Q("buy-box"), art, Ui.Steel);
            ui.Wire(r.Q("buy-cancel"), BtnKind.Normal, () => send(input(CampKey.Close)));
            ui.Wire(r.Q("buy-ok"), BtnKind.Primary, () => send(input(CampKey.Enter)));
            var veil = r.Q("buy-dialog");
            veil.RegisterCallback<ClickEvent>(e => { if (e.target == veil) send(input(CampKey.Close)); });
            return ui;
        }

        CampLayer? shown;

        public void Fill(Campaign c, CampNav nav, Func<CampKey, CampInput> input)
        {
            if (shown.HasValue && shown != nav.Layer) { Leave(PanelOf(shown.Value)); Enter(PanelOf(nav.Layer)); }
            shown = nav.Layer;
            root.EnableInClassList("l-map", nav.Layer == CampLayer.Map);
            root.EnableInClassList("l-info", nav.Layer == CampLayer.Info);
            root.EnableInClassList("l-barracks", nav.Layer == CampLayer.Barracks);
            tip.style.display = DisplayStyle.None;
            root.Q<Label>("title").text = Loc.T("camp.expedition");

            var reset = root.Q("reset");
            reset.Q<Label>(className: "btn-text").text = Loc.T(nav.ResetArmed ? "camp.resetConfirm" : "camp.reset");
            ToolkitKit.Style(reset, art, nav.ResetArmed ? BtnKind.DangerOn : BtnKind.Ghost);

            var barracks = root.Q("barracks");
            barracks.Q<Label>(className: "btn-text").text = Loc.T("camp.barracksName");
            barracks.EnableInClassList("on", nav.Barracks);
            ToolkitKit.Style(barracks, art, nav.Barracks ? BtnKind.Primary : BtnKind.Normal);
            var buyable = CampNav.Affordable(c);
            var badge = barracks.Q("badge");
            badge.EnableInClassList("shown", buyable > 0);
            badge.Q<Label>().text = buyable.ToString();
            SetTip(barracks, buyable > 0 ? Loc.T("camp.affordable", buyable) : null);

            if (nav.Layer == CampLayer.Info) Info(c, nav.Stage, input);
            if (nav.Layer == CampLayer.Barracks) Barracks(c, nav, input);
            BuyDialog(nav.BuyArmed);
        }

        string asking;

        // upgrade.md 6-1: the upgrade's name, the question and its price; buttons and veil are wired once in Build.
        void BuyDialog(string id)
        {
            var up = id != null ? GameData.Upgrades.FirstOrDefault(u => u.Id == id) : null;
            root.EnableInClassList("asking", up != null);
            if (up != null && asking != id) Enter(root.Q("buy-box"));
            asking = up?.Id;
            if (up == null) return;
            root.Q<Label>("buy-name").text = Loc.Text(up.Name);
            root.Q<Label>("buy-ask").text = Loc.T("camp.buyAsk");
            root.Q<Label>("buy-warn").text = Loc.T("camp.buyWarn", Loc.Text(GameData.Type(up.Unit).Name));
            var price = root.Q("buy-price");
            price.Clear();
            ToolkitKit.Value(price, art, Kit.Icon.Gold, up.Cost.ToString(), Ui.Candle, Ui.Candle);
            root.Q("buy-cancel").Q<Label>(className: "btn-text").text = Loc.T("camp.cancel");
            root.Q("buy-ok").Q<Label>(className: "btn-text").text = Loc.T("camp.buy");
        }

        VisualElement PanelOf(CampLayer layer) => layer == CampLayer.Info ? root.Q("info") : layer == CampLayer.Barracks ? root.Q("barracks-panel") : null;

        // ui-style.md 12 / stage-select.md 5-1: panels fade in rising 8 px and fade out over 0.15 s (USS transition on .panel).
        static void Enter(VisualElement p)
        {
            if (p == null) return;
            p.RemoveFromClassList("leave");
            p.SetEnabled(true);
            p.AddToClassList("enter");
            // Two scheduler ticks: the first frame must resolve the hidden style, or there is nothing to transition from.
            p.schedule.Execute(() => p.schedule.Execute(() => p.RemoveFromClassList("enter")));
        }

        static void Leave(VisualElement p)
        {
            if (p == null) return;
            p.SetEnabled(false);
            p.AddToClassList("leave");
            p.schedule.Execute(() => { p.RemoveFromClassList("leave"); p.SetEnabled(true); }).StartingIn(150);
        }

        void Info(Campaign c, int i, Func<CampKey, CampInput> input)
        {
            var s = Campaign.Stages[i];
            var last = i == Campaign.Stages.Length - 1;
            var clear = c.IsCleared(s);
            var bonusGot = c.BonusDone.Contains(s.Id);
            var panel = root.Q("info");
            ToolkitKit.Frame(panel, art, last ? Ui.Blood : Ui.Steel);
            var crest = panel.Q("info-crest");
            crest.Clear();
            crest.style.display = last ? DisplayStyle.Flex : DisplayStyle.None;
            if (last) ToolkitKit.Prefab(crest, art.UiPrefab(Crest), Ui.Blood);

            var badge = panel.Q("info-badge");
            var (ring, fill, ink) = BadgeColors(c, i);
            ToolkitKit.Picture(badge.Q(className: "ring-fill"), art.Ui(RingBack), fill);
            ToolkitKit.Picture(badge.Q(className: "ring"), art.Ui(Ring), ring);
            badge.Q<Label>().text = (i + 1).ToString();
            badge.Q<Label>().style.color = ink;

            var name = panel.Q<Label>("info-name");
            name.text = Loc.Text(s.Title);
            name.style.color = last ? Ui.Blood : Ui.Bone;
            SetTip(name, Loc.Text(s.Brief));

            Line(panel.Q("info-goal"), Kit.Icon.Boss, Ui.Blood, Loc.T("camp.objectiveBoss", s.BossName), Ui.Bone, Loc.T("camp.objective"));
            Line(panel.Q("info-bonus"), Kit.Icon.Star, bonusGot ? Ui.Candle : Ui.Muted, Loc.Text(s.Bonus.Text), bonusGot ? Ui.Candle : Ui.Bone,
                Loc.T("camp.bonusGoal") + (bonusGot ? " · " + Loc.T("camp.got") : ""));

            var stats = panel.Q("info-stats");
            stats.Clear();
            ToolkitKit.Value(stats, art, Kit.Icon.Gold, "+" + s.Reward, clear ? Ui.Dim : Ui.Candle, clear ? Ui.Dim : Ui.Bone, Loc.T("camp.rewardClear", s.Reward) + (clear ? " · " + Loc.T("camp.got") : ""), tip);
            ToolkitKit.Value(stats, art, Kit.Icon.Star, "+" + s.Bonus.Gold, bonusGot ? Ui.Dim : Ui.Candle, bonusGot ? Ui.Dim : Ui.Bone, Loc.T("camp.rewardBonus", s.Bonus.Gold) + (bonusGot ? " · " + Loc.T("camp.got") : ""), tip);
            ToolkitKit.Value(stats, art, Kit.Icon.Pop, s.Pop.ToString(), Ui.Candle, Ui.Candle, Loc.T("battle.pop"), tip);
            if (s.Silver > 0) ToolkitKit.Value(stats, art, Kit.Icon.Silver, s.Silver.ToString(), Ui.Candle, Ui.Candle, Loc.T("battle.silver"), tip);
            ToolkitKit.Value(stats, art, Kit.Icon.Skulls, s.Minions.Length.ToString(), Ui.Blood, Ui.Bone, Kit.EnemyList(s), tip);

            var roster = panel.Q("info-roster");
            roster.Clear();
            foreach (var k in s.Units)
            {
                var t = GameData.Type(k);
                var isNew = t.UnlockAt == i + 1;
                var chip = ToolkitKit.Chip(roster, art, isNew ? Ui.Candle : Ui.SteelDim, art.Ui(art.Icon(k)), isNew ? Ui.Candle : Ui.Steel);
                SetTip(chip, isNew ? Loc.T("camp.new", t.Name) : Loc.Text(t.Name));
            }

            var march = panel.Q("info-march");
            march.Clear();
            var btn = ToolkitKit.Btn(march, art, Loc.T(clear ? "camp.marchAgain" : "camp.march"), clear ? BtnKind.Normal : BtnKind.Primary, "Enter", () => send(input(CampKey.Enter)));
            if (clear) SetTip(btn, Loc.T(bonusGot ? "camp.allClaimed" : "camp.replayNote"));
        }

        void Barracks(Campaign c, CampNav nav, Func<CampKey, CampInput> input)
        {
            var panel = root.Q("barracks-panel");
            var name = panel.Q<Label>("bk-name");
            name.text = Loc.T("camp.barracksName");
            var anyOpen = GameData.Types.Any(t => c.UpgradesOpen(t.Key));
            SetTip(name, Loc.T(anyOpen ? "camp.barracksHelp" : "camp.noUpgrades"));
            var gold = panel.Q("bk-gold");
            gold.Clear();
            ToolkitKit.Value(gold, art, Kit.Icon.Gold, c.Gold.ToString(), Ui.Candle, Ui.Candle, Loc.T("camp.gold"), tip);

            var tabs = panel.Q("bk-tabs");
            tabs.Clear();
            var types = GameData.Types;
            for (var i = 0; i < types.Length; i++)
            {
                var t = types[i];
                var state = CampNav.TabOf(c, t.Key);
                var chip = ToolkitKit.Chip(tabs, art, nav.Tab == i ? Ui.Steel : state == TabState.Open ? Ui.SteelDim : Ui.Line,
                    art.Ui(state == TabState.Hidden ? Kit.Icon.Lock : art.Icon(t.Key)), state == TabState.Open ? Ui.Steel : Ui.Dim, state == TabState.Hidden ? 28f : 36f);
                chip.EnableInClassList("on", nav.Tab == i);
                var unlockAt = Mathf.Clamp(t.UnlockAt - 1, 0, Campaign.Stages.Length - 1);
                if (state == TabState.Locked) chip.Add(Small("tab-lock", Kit.Icon.Lock, Ui.Dim));
                if (CampNav.Affordable(c, t.Key) > 0) { var dot = new VisualElement { pickingMode = PickingMode.Ignore }; dot.AddToClassList("tab-dot"); chip.Add(dot); }
                SetTip(chip, state == TabState.Open ? Loc.Text(t.Name) : Loc.T("camp.lockedBy", Loc.Text(Campaign.Stages[unlockAt].Title)));
                var index = i;
                UiSound.Add(chip);
                chip.RegisterCallback<ClickEvent>(_ => send(CampInput.Tab(index, Time.unscaledTime)));
            }

            var sel = Mathf.Clamp(nav.Tab, 0, types.Length - 1);
            var kind = types[sel].Key;
            var tabState = CampNav.TabOf(c, kind);
            var unit = panel.Q("bk-unit");
            ToolkitKit.Picture(unit.Q(className: "bk-unit-icon"), art.Ui(art.Icon(kind)), Ui.Steel);
            unit.Q<Label>().text = Loc.Text(types[sel].Name);
            var stats = panel.Q("bk-stats");
            var now = Battle.Preview(kind, c.Upgrades);
            Stats(stats, now, null);

            var chosen = c.Chosen(kind);
            var rule = panel.Q<Label>("bk-rule");
            rule.text = chosen != null ? Loc.T("camp.branchChosen", Loc.Text(chosen.Name)) : Loc.T("camp.branchRule");
            rule.EnableInClassList("chosen", chosen != null);
            var lockedBy = Loc.T("camp.lockedBy", Loc.Text(Campaign.Stages[Mathf.Clamp(types[sel].UnlockAt - 1, 0, Campaign.Stages.Length - 1)].Title));
            var branchLocked = chosen != null ? Loc.T("camp.branchLocked", Loc.Text(chosen.Name)) : null;

            var ups = panel.Q<ScrollView>("bk-ups");
            ups.Clear();
            foreach (var up in GameData.Branches(kind))
            {
                var own = up.Id == chosen?.Id;
                var shut = chosen != null && !own;
                var row = new VisualElement();
                row.AddToClassList("up");
                row.EnableInClassList("owned", own);
                row.EnableInClassList("shut", shut);
                SetTip(row, Loc.T(up.Inherit ? "camp.inherit" : "camp.noInherit") + (shut ? " · " + branchLocked : ""));
                var head = new VisualElement { pickingMode = PickingMode.Ignore };
                head.AddToClassList("up-name");
                if (up.Inherit) head.Add(Small("up-fang", Kit.Icon.Fang, shut ? Ui.Dim : Ui.Spawn));
                head.Add(new Label(Loc.Text(up.Name)) { pickingMode = PickingMode.Ignore });
                row.Add(head);
                if (own) { var check = new Label("✓") { pickingMode = PickingMode.Ignore }; check.AddToClassList("up-owned"); row.Add(check); }
                var desc = new Label(Loc.Text(up.Desc)) { pickingMode = PickingMode.Ignore };
                desc.AddToClassList("up-desc");
                row.Add(desc);
                ups.Add(row);
                if (own) continue;
                var act = new VisualElement { pickingMode = PickingMode.Ignore };
                act.AddToClassList("up-act");
                row.Add(act);
                if (tabState != TabState.Open || shut)
                {
                    var lk = Small("up-lock", Kit.Icon.Lock, Ui.Dim);
                    lk.pickingMode = PickingMode.Position;
                    act.Add(lk);
                    SetTip(lk, shut ? branchLocked : lockedBy);
                    continue;
                }
                var price = new VisualElement { pickingMode = PickingMode.Ignore };
                price.AddToClassList("up-price");
                ToolkitKit.Value(price, art, Kit.Icon.Gold, up.Cost.ToString(), Ui.Candle, Ui.Candle);
                row.Add(price);
                var id = up.Id;
                var b = ToolkitKit.Btn(act, art, Loc.T("camp.upgrade"), BtnKind.Normal, null, () => send(CampInput.Buy(id, Time.unscaledTime)), c.CanBuy(up));
                if (!c.CanBuy(up)) SetTip(b, Loc.T("camp.needGold", c.Gold, up.Cost));
                var next = Battle.Preview(kind, c.Upgrades.Append(up.Id));
                row.RegisterCallback<PointerEnterEvent>(_ => Stats(stats, now, next));
                row.RegisterCallback<PointerLeaveEvent>(_ => Stats(stats, now, null));
            }
            // USS has no :last-child; the gap belongs only between rows, or three rows overflow the 480 viewport by 12.
            if (ups.contentContainer.childCount > 0) ups.contentContainer[ups.contentContainer.childCount - 1].style.marginBottom = 0;
        }

        // upgrade.md 3 stat row; with a hovered branch (5-5) the numbers it would change turn light.
        void Stats(VisualElement stats, Unit u, Unit next)
        {
            stats.Clear();
            void Cell(string icon, string value, string nextValue, string label)
            {
                var changed = nextValue != null && nextValue != value;
                ToolkitKit.Value(stats, art, icon, changed ? nextValue : value, Ui.Steel, changed ? Ui.Candle : Ui.Bone, label, tip);
            }
            string Atk(Unit x) => x.Atk + (x.Bane > 0 ? "+" + x.Bane : "");
            Cell(Kit.Icon.Hp, u.Hp.ToString(), next?.Hp.ToString(), Loc.T("unit.card.hp"));
            Cell(Kit.Icon.Atk, Atk(u), next != null ? Atk(next) : null, Loc.T("unit.card.atk"));
            Cell(Kit.Icon.Def, u.Def.ToString(), next?.Def.ToString(), Loc.T("unit.card.def"));
            Cell(Kit.Icon.Mov, u.Mov.ToString(), next?.Mov.ToString(), Loc.T("unit.card.mov"));
            Cell(Kit.Icon.Rng, u.Rng.ToString(), next?.Rng.ToString(), Loc.T("unit.card.rng"));
            if (u.Heal > 0 || next?.Heal > 0) Cell(Kit.Icon.Heal, u.Heal.ToString(), next?.Heal.ToString(), Loc.T("skill.mend"));
        }

        static (Color ring, Color fill, Color ink) BadgeColors(Campaign c, int i)
        {
            var last = i == Campaign.Stages.Length - 1;
            var accent = last ? Ui.Blood : Ui.Steel;
            if (c.IsCleared(Campaign.Stages[i])) return (accent, accent, Ui.Ink);
            if (c.StageOpen(i)) return (last ? Ui.Blood : Ui.Sun, Ui.Deep, Ui.Bone);
            return (last ? Ui.BloodDim : Ui.Line, Ui.Deep, Ui.Dim);
        }

        void Line(VisualElement line, string icon, Color iconColor, string text, Color color, string tipText)
        {
            ToolkitKit.Picture(line.Q(className: "line-icon"), art.Ui(icon), iconColor);
            var t = line.Q<Label>(className: "line-text");
            t.text = text;
            t.style.color = color;
            SetTip(line, tipText);
        }

        VisualElement Small(string cls, string icon, Color color)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(cls);
            ToolkitKit.Picture(e, art.Ui(icon), color);
            return e;
        }

        void Wire(VisualElement b, BtnKind kind, Action click)
        {
            ToolkitKit.Style(b, art, kind);
            ToolkitKit.Click(b, click);
        }

        // Fill runs again on every state change, so a fixed element binds its tooltip once and reads the text from userData.
        void SetTip(VisualElement e, string text)
        {
            if (!(e.userData is TipText t))
            {
                t = new TipText();
                e.userData = t;
                ToolkitKit.Tip(e, () => t.Text, tip, 400);
            }
            t.Text = text;
        }

        sealed class TipText { public string Text; }
    }
}
