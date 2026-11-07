using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    public sealed class CampaignActions
    {
        public Action<int> Play, Select;
        public Action<string> Buy;
        public Action Reset, Menu;
    }

    public sealed class CampView
    {
        public int Selected;
        public bool ResetArm;
        public string BuyArm, Bought;
    }

    // docs/ui/stage-select.md and docs/ui/upgrade.md, cut down to names, numbers and icons by docs/art/ui-style.md 14. Sizes are 1920x1080 px.
    public static class CampaignScreen
    {
        const float Edge = 24f, Top = 112f, Gap = 16f, RouteW = 360f, InfoW = 620f;

        public static string StatLine(Unit s) =>
            Loc.T("camp.statLine", s.Hp, s.Atk + (s.Bane > 0 ? "(+" + s.Bane + ")" : ""), s.Def, s.Mov, s.Rng);

        public static string Resources(Stage s) => Resources(s.Pop, s.Silver);

        public static string Resources(int pop, int silver) => Loc.T("camp.pop", pop) + (silver > 0 ? " · " + Loc.T("camp.silver", silver) : "");

        // stage-select.md 2-8: the first open stage not yet cleared, else the last one.
        public static int DefaultStage(Campaign c)
        {
            for (var i = 0; i < Campaign.Stages.Length; i++) if (c.StageOpen(i) && !c.IsCleared(Campaign.Stages[i])) return i;
            return Campaign.Stages.Length - 1;
        }

        static Text T(Transform p, string s, float px, Color c, bool bold = false, bool display = false, TextAnchor a = TextAnchor.UpperLeft) => Kit.T(p, s, px, c, bold, display, a);

        static bool Last(Stage s) => s == Campaign.Stages.Last();

        public static void Build(RectTransform root, Campaign c, CampView v, CampaignActions a, ArtCatalog art)
        {
            var kit = new Kit(art);
            Head(root, c, v, a, kit);
            Route(root, c, v, a, kit);
            Info(root, c, v, a, kit);
            Barracks(root, c, v, a, kit);
        }

        static RectTransform Column(RectTransform root, string name, float x, float width, float spacing, int pad)
        {
            var col = Ui.Col(root, name, spacing, pad);
            col.anchorMin = new Vector2(0f, 0f);
            col.anchorMax = new Vector2(0f, 1f);
            col.pivot = new Vector2(0f, 1f);
            col.offsetMin = new Vector2(x, Edge);
            col.offsetMax = new Vector2(x + width, -Top);
            col.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            return col;
        }

        static void Head(RectTransform root, Campaign c, CampView v, CampaignActions a, Kit kit)
        {
            var title = T(Ui.Place(root, "Title", new Vector2(0f, 1f), new Vector2(Edge, -Edge), new Vector2(600f, 64f)), Loc.T("camp.expedition"), 40, Ui.Bone, true, true, TextAnchor.MiddleLeft);
            ((RectTransform)title.transform.parent).pivot = new Vector2(0f, 1f);
            Ui.Stretch(title.rectTransform);

            var right = Ui.Row(root, "Gold", 20, 0, null, null, TextAnchor.MiddleRight);
            right.anchorMin = right.anchorMax = right.pivot = new Vector2(1f, 1f);
            right.anchoredPosition = new Vector2(-Edge, -Edge);
            right.sizeDelta = new Vector2(700f, 64f);
            right.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            right.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
            var gold = kit.Value(right, Kit.Icon.Gold, c.Gold.ToString(), 34, Ui.Candle, Ui.Candle, Loc.T("camp.gold"));
            Ui.Size(gold, 150, 56);
            if (v.Bought != null) Fx.Flash(gold.GetComponentInChildren<Text>());
            kit.Btn(right, Loc.T(v.ResetArm ? "camp.resetConfirm" : "camp.reset"), a.Reset, v.ResetArm ? BtnKind.DangerOn : BtnKind.Ghost, new Vector2(v.ResetArm ? 220f : 140f, 44f));
            var menu = kit.Btn(right, Loc.T("camp.menu"), a.Menu, BtnKind.Normal, new Vector2(190f, 48f));
            Kit.Hotkey((RectTransform)menu.transform, "Esc");
        }

        static void Route(RectTransform root, Campaign c, CampView v, CampaignActions a, Kit kit)
        {
            var col = Column(root, "Route", Edge, RouteW, 0, 18);
            kit.Frame(col, Ui.SteelDim);
            var stages = Campaign.Stages;
            for (var i = 0; i < stages.Length; i++) RouteRow(col, c, i, v.Selected == i, i < stages.Length - 1, a, kit);
        }

        static Color NodeColor(Campaign c, int i)
        {
            var s = Campaign.Stages[i];
            var open = c.StageOpen(i);
            return Last(s) ? (open ? Ui.Blood : Ui.BloodDim) : c.IsCleared(s) ? Ui.Steel : open ? Ui.Sun : Ui.Line;
        }

        // ui-style.md 14-1-5: the node shows the state (filled = cleared, pulsing = open, lock = locked); no state words.
        static void RouteRow(Transform parent, Campaign c, int i, bool selected, bool hasNext, CampaignActions a, Kit kit)
        {
            var s = Campaign.Stages[i];
            var open = c.StageOpen(i);
            var clear = c.IsCleared(s);
            var last = Last(s);
            var row = Ui.Node(parent, "Stage " + s.Id);
            Ui.Size(row, -1, 66);
            if (selected)
            {
                Ui.Bg(row, Ui.Alpha(Ui.Deep, 0.92f));
                var bar = Ui.Place(row, "Bar", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, 66f));
                bar.pivot = new Vector2(0f, 0.5f);
                Ui.Bg(bar, last ? Ui.Blood : Ui.Steel, null, false);
            }
            else Ui.Bg(row, Ui.Alpha(Ui.Ink, 0f));
            var nodeColor = NodeColor(c, i);
            if (hasNext)
            {
                var line = Ui.Place(row, "Line", new Vector2(0f, 0.5f), new Vector2(30f, -18f), new Vector2(2f, 30f));
                line.pivot = new Vector2(0.5f, 1f);
                Ui.Bg(line, clear ? Ui.Steel : Ui.Line, null, false);
            }
            var node = Ui.Place(row, "Node", new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(36f, 36f));
            node.pivot = new Vector2(0f, 0.5f);
            if (!open)
            {
                Ui.Stretch(Ui.Sprite(node, "Ring", kit.S("SPR_DarkFantasyMenus_Frame_Ring_01"), nodeColor).rectTransform, -2, -2, -2, -2);
                Ui.Stretch(Ui.Sprite(node, "Lock", kit.S(Kit.Icon.Lock), Ui.Dim).rectTransform, 9, 9, 9, 9);
            }
            else
            {
                var badge = kit.Badge(node, (i + 1).ToString(), 36, nodeColor, clear ? nodeColor : Ui.Deep, clear ? Ui.Ink : Ui.Bone);
                Ui.Stretch(badge);
                if (!clear) Fx.Pulse(badge.Find("Ring").GetComponent<Image>());
            }
            var name = Ui.Row(row, "Name", 8, 0, null, null, TextAnchor.MiddleLeft);
            name.anchorMin = new Vector2(0f, 0f);
            name.anchorMax = new Vector2(1f, 1f);
            name.offsetMin = new Vector2(64f, 0f);
            name.offsetMax = new Vector2(-12f, 0f);
            name.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var title = T(name, Loc.Text(s.Title), 20, last ? nodeColor : open ? Ui.Bone : Ui.Dim, open && !clear, false, TextAnchor.MiddleLeft);
            Ui.Ellipsize(title, RouteW - 2 * 18 - 64 - 12 - 32);
            var got = c.BonusDone.Contains(s.Id);
            Ui.Size(Ui.Sprite(name, "Star", kit.S(Kit.Icon.Star), got ? Ui.Candle : Ui.Line), 20, 20);
            HoverTip.Add(row, Loc.Text(s.Label) + " · " + Loc.T(clear ? "camp.cleared" : open ? "camp.open" : "camp.locked"), 0.6f);
            var btn = row.gameObject.AddComponent<Button>();
            btn.targetGraphic = row.GetComponent<Image>();
            var index = i;
            btn.onClick.AddListener(() => a.Select?.Invoke(index));
        }

        static void Info(RectTransform root, Campaign c, CampView v, CampaignActions a, Kit kit)
        {
            var i = Mathf.Clamp(v.Selected, 0, Campaign.Stages.Length - 1);
            var s = Campaign.Stages[i];
            var open = c.StageOpen(i);
            var clear = c.IsCleared(s);
            var bonusGot = c.BonusDone.Contains(s.Id);
            var last = Last(s);
            var panel = Ui.Node(root, "Info");
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            var x = Edge + RouteW + Gap;
            panel.offsetMin = new Vector2(x, Edge);
            panel.offsetMax = new Vector2(x + InfoW, -Top);
            kit.Frame(panel, last ? Ui.Blood : Ui.Steel);
            // The kit crest is a demon skull, so only the count's castle wears it (ui-style.md 5-3).
            if (last) kit.Prefab(panel, "Crest_DarkFantasyMenus_Large_01", new Vector2(0.5f, 1f), new Vector2(0f, 40f), 96f, Ui.Blood);
            var col = Ui.Col(panel, "Body", 16, 36);
            Ui.Stretch(col, 0, 44, 0, 100);
            col.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;

            var head = Ui.Row(col, "Head", 16, 0, null, null, TextAnchor.MiddleLeft);
            head.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            kit.Badge(head, (i + 1).ToString(), 48, NodeColor(c, i), Ui.Deep, Ui.Bone);
            var title = T(head, Loc.Text(s.Title), 40, last ? Ui.Blood : Ui.Bone, true, true, TextAnchor.MiddleLeft);
            Ui.Ellipsize(title, InfoW - 72 - 64 - 16);
            // ui-style.md 14-1-2: the stage brief moves into the title's tooltip.
            HoverTip.Add(head, Loc.Text(s.Brief), 0.4f);

            var brk = Ui.Node(col, "Break");
            Ui.Size(brk, -1, 24);
            if (kit.Prefab(brk, "Component_DarkFantasyMenus_Section_Break_02", new Vector2(0.5f, 0.5f), Vector2.zero, 420f, Ui.Line) == null)
                Ui.Bg(Ui.Place(brk, "Line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 1f)), Ui.Line, null, false);

            Line(col, kit, Kit.Icon.Boss, Loc.T("camp.objectiveBoss", s.BossName), Ui.Blood, Ui.Bone, Loc.T("camp.objective"));
            Line(col, kit, Kit.Icon.Star, Loc.Text(s.Bonus.Text), bonusGot ? Ui.Candle : Ui.Muted, bonusGot ? Ui.Candle : Ui.Bone, Loc.T("camp.bonusGoal") + (bonusGot ? " · " + Loc.T("camp.got") : ""));

            var nums = Ui.Row(col, "Numbers", 28, 0, null, null, TextAnchor.MiddleLeft);
            nums.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            kit.Value(nums, Kit.Icon.Gold, "+" + s.Reward, 22, clear ? Ui.Dim : Ui.Candle, clear ? Ui.Dim : Ui.Bone, Loc.T("camp.rewardClear", s.Reward) + (clear ? " · " + Loc.T("camp.got") : ""));
            kit.Value(nums, Kit.Icon.Star, "+" + s.Bonus.Gold, 22, bonusGot ? Ui.Dim : Ui.Candle, bonusGot ? Ui.Dim : Ui.Bone, Loc.T("camp.rewardBonus", s.Bonus.Gold) + (bonusGot ? " · " + Loc.T("camp.got") : ""));
            kit.Value(nums, Kit.Icon.Pop, s.Pop.ToString(), 22, Ui.Candle, Ui.Candle, Loc.T("battle.pop"));
            if (s.Silver > 0) kit.Value(nums, Kit.Icon.Silver, s.Silver.ToString(), 22, Ui.Candle, Ui.Candle, Loc.T("battle.silver"));
            kit.Value(nums, Kit.Icon.Skulls, (s.Enemies.Length - 1).ToString(), 22, Ui.Blood, Ui.Bone, Loc.T("battle.enemyList", s.Enemies.Length - 1, s.BossName));

            var chips = Ui.Row(col, "Roster", 10, 0, null, null, TextAnchor.MiddleLeft);
            chips.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            foreach (var k in s.Units)
            {
                var t = GameData.Type(k);
                var isNew = t.UnlockAt == i + 1;
                var chip = Ui.Node(chips, "Chip " + t.Key);
                Ui.Size(chip, 64, 64);
                kit.Frame(chip, isNew ? Ui.Candle : Ui.SteelDim, 14f);
                Ui.Stretch(Ui.Sprite(chip, "Icon", kit.S(Kit.Weapon[k]), isNew ? Ui.Candle : Ui.Steel).rectTransform, 12, 12, 12, 12);
                HoverTip.Add(chip, isNew ? Loc.T("camp.new", t.Name) : Loc.Text(t.Name), 0.2f);
            }

            var foot = Ui.Node(panel, "Foot");
            foot.anchorMin = new Vector2(0f, 0f);
            foot.anchorMax = new Vector2(1f, 0f);
            foot.pivot = new Vector2(0.5f, 0f);
            foot.offsetMin = new Vector2(32f, 28f);
            foot.offsetMax = new Vector2(-32f, 88f);
            if (!open)
            {
                var lk = Ui.Sprite(foot, "Lock", kit.S(Kit.Icon.Lock), Ui.Dim).rectTransform;
                lk.anchorMin = lk.anchorMax = lk.pivot = new Vector2(1f, 0.5f);
                lk.sizeDelta = new Vector2(48f, 48f);
                HoverTip.Add(foot, Loc.T("camp.lockedBy", Loc.Text(Campaign.Stages[i - 1].Title)), 0f);
                return;
            }
            var btn = (RectTransform)kit.Btn(foot, Loc.T(clear ? "camp.marchAgain" : "camp.march"), () => a.Play?.Invoke(i), clear ? BtnKind.Normal : BtnKind.Primary, new Vector2(260f, 60f),
                true, clear ? Loc.T(bonusGot ? "camp.allClaimed" : "camp.replayNote") : null).transform;
            btn.anchorMin = btn.anchorMax = btn.pivot = new Vector2(1f, 0.5f);
            btn.anchoredPosition = Vector2.zero;
            Kit.Hotkey(btn, "Enter");
        }

        static void Line(Transform parent, Kit kit, string icon, string text, Color iconColor, Color color, string tip)
        {
            var row = Ui.Row(parent, "Line", 12, 0, null, null, TextAnchor.MiddleLeft);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(Ui.Sprite(row, "Icon", kit.S(icon), iconColor), 30, 30);
            var t = T(row, text, 20, color, false, false, TextAnchor.MiddleLeft);
            Ui.Ellipsize(t, InfoW - 72 - 42);
            HoverTip.Add(row, tip, 0.3f);
        }

        static void Barracks(RectTransform root, Campaign c, CampView v, CampaignActions a, Kit kit)
        {
            var x = Edge + RouteW + Gap + InfoW + Gap;
            var view = Ui.Scroll(root, "Barracks", out var scroll);
            var sv = (RectTransform)scroll.transform;
            sv.anchorMin = new Vector2(0f, 0f);
            sv.anchorMax = new Vector2(1f, 1f);
            sv.pivot = new Vector2(0f, 1f);
            sv.offsetMin = new Vector2(x, Edge);
            sv.offsetMax = new Vector2(-Edge, -Top);
            kit.Frame(sv, Ui.SteelDim);
            view.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(20, 20, 20, 20);
            view.GetComponent<VerticalLayoutGroup>().spacing = 12;
            var head = Ui.Row(view, "Head", 10, 0, null, null, TextAnchor.MiddleLeft);
            head.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(Ui.Sprite(head, "Icon", kit.S(Kit.Icon.Up), Ui.Steel), 28, 28);
            T(head, Loc.T("camp.barracks"), 24, Ui.Bone, true, true, TextAnchor.MiddleLeft);
            HoverTip.Add(head, Loc.T("camp.barracksHelp"), 0.4f);
            var open = GameData.Types.Where(t => c.UpgradesOpen(t.Kind)).ToList();
            if (open.Count == 0)
            {
                var none = Ui.Row(view, "None", 8, 0, null, null, TextAnchor.MiddleLeft);
                none.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                Ui.Size(Ui.Sprite(none, "Lock", kit.S(Kit.Icon.Lock), Ui.Dim), 28, 28);
                HoverTip.Add(none, Loc.T("camp.noUpgrades"), 0f);
            }
            foreach (var t in open) ArmCard(view, c, t, v, a, kit);
        }

        static void ArmCard(Transform parent, Campaign c, UnitType t, CampView v, CampaignActions a, Kit kit)
        {
            var card = Ui.Col(parent, "Arm " + t.Key, 10, 16);
            kit.Frame(card, Ui.SteelDim);
            var head = Ui.Row(card, "Head", 12, 0, null, null, TextAnchor.MiddleLeft);
            head.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(Ui.Sprite(head, "Icon", kit.S(Kit.Weapon[t.Kind]), Ui.Steel), 44, 44);
            var name = Ui.Col(head, "Name", 4);
            name.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            T(name, Loc.Text(t.Name), 22, Ui.Bone, true, true);
            kit.Stats(name, Battle.Preview(t.Kind, c.Upgrades), 17, Ui.Steel, Ui.Muted);
            var ups = Ui.Row(card, "Ups", 8, 0, null, null, TextAnchor.UpperLeft);
            ups.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            foreach (var u in GameData.Upgrades.Where(x => x.Unit == t.Kind))
            {
                var own = c.Upgrades.Contains(u.Id);
                var box = Ui.Col(ups, "Up " + u.Id, 8, 12, Ui.Alpha(Ui.Deep, 0.92f), own ? Ui.Candle : Ui.Line);
                // Equal shares: an owned upgrade (just a check) must not shrink and squeeze its name out.
                Ui.Size(box, 0, -1, 1);
                if (v.Bought == u.Id) Fx.Flash(box.GetComponent<Image>());
                var line = Ui.Row(box, "T", 6, 0, null, null, TextAnchor.MiddleLeft);
                line.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                // ui-style.md 14-4: a fang before the name marks an upgrade the spawn inherits.
                if (u.Inherit) Ui.Size(Ui.Sprite(line, "Inherit", kit.S(Kit.Icon.Fang), Ui.Spawn), 18, 18);
                Ui.Size(Kit.Wrap(T(line, Loc.Text(u.Name), 17, Ui.Bone, true, false, TextAnchor.MiddleLeft)), 0, -1, 1);
                // The effect decides the purchase, so it stays on the card (direction review, docs/dev/ui-overhaul-decisions.tsv).
                Kit.Wrap(T(box, Loc.Text(u.Desc), 15, Ui.Muted));
                HoverTip.Add(box, Loc.T(u.Inherit ? "camp.inherit" : "camp.noInherit"), 0.3f);
                var acts = Ui.Row(box, "Acts", 8, 0, null, null, TextAnchor.MiddleLeft);
                acts.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                acts.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                if (own)
                {
                    Ui.Size(T(acts, "✓", 20, Ui.Candle, true, false, TextAnchor.MiddleLeft), 24, 40);
                    continue;
                }
                var coin = kit.Value(acts, Kit.Icon.Gold, u.Cost.ToString(), 20, Ui.Candle, Ui.Candle);
                Ui.Size(coin, 76, 40);
                var armed = v.BuyArm == u.Id;
                var id = u.Id;
                var afford = c.Gold >= u.Cost;
                kit.Btn(acts, armed ? Loc.T("camp.buyConfirm", u.Cost) : Loc.T("camp.upgrade"), () => a.Buy?.Invoke(id), armed ? BtnKind.DangerOn : BtnKind.Normal,
                    new Vector2(armed ? 180f : 96f, 40f), afford, afford ? null : Loc.T("camp.needGold", c.Gold, u.Cost));
            }
        }
    }
}
