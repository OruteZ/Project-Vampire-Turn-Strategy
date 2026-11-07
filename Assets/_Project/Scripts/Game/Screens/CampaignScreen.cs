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

    // docs/ui/stage-select.md (route, stage info) and docs/ui/upgrade.md (barracks). Sizes are 1920x1080 px.
    public static class CampaignScreen
    {
        const float Edge = 24f, Top = 144f, Gap = 16f, RouteW = 420f, InfoW = 640f;

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
            var title = Ui.Col(root, "Title", 2);
            title.anchorMin = title.anchorMax = title.pivot = new Vector2(0f, 1f);
            title.anchoredPosition = new Vector2(Edge, -Edge);
            title.sizeDelta = new Vector2(600f, 96f);
            T(title, Loc.T("camp.title"), 14, Ui.Muted);
            T(title, Loc.T("camp.expedition"), 32, Ui.Bone, true, true);

            var right = Ui.Row(root, "Gold", 16, 0, null, null, TextAnchor.MiddleRight);
            right.anchorMin = right.anchorMax = right.pivot = new Vector2(1f, 1f);
            right.anchoredPosition = new Vector2(-Edge, -Edge);
            right.sizeDelta = new Vector2(700f, 72f);
            right.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            right.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
            var gold = Ui.Row(right, "Coins", 6, 0, null, null, TextAnchor.LowerRight);
            gold.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(gold, 160, 48);
            var num = T(gold, c.Gold.ToString(), 34, Ui.Candle, true);
            T(gold, Loc.T("camp.gold"), 14, Ui.Muted);
            if (v.Bought != null) Fx.Flash(num);
            kit.Btn(right, Loc.T(v.ResetArm ? "camp.resetConfirm" : "camp.reset"), a.Reset, v.ResetArm ? BtnKind.DangerOn : BtnKind.Ghost, new Vector2(v.ResetArm ? 200f : 140f, 44f));
            var menu = kit.Btn(right, Loc.T("camp.menu"), a.Menu, BtnKind.Normal, new Vector2(150f, 44f));
            Kit.Hotkey((RectTransform)menu.transform, "Esc");
        }

        static void Route(RectTransform root, Campaign c, CampView v, CampaignActions a, Kit kit)
        {
            var col = Column(root, "Route", Edge, RouteW, 0, 20);
            kit.Frame(col, Ui.SteelDim);
            T(col, Loc.T("camp.route"), 22, Ui.Bone, true, true);
            var intro = Kit.Wrap(T(col, Loc.T("camp.intro"), 14, Ui.Muted));
            Ui.Size(intro).minHeight = 60;
            var stages = Campaign.Stages;
            for (var i = 0; i < stages.Length; i++) RouteRow(col, c, i, v.Selected == i, i < stages.Length - 1, a, kit);
        }

        static void RouteRow(Transform parent, Campaign c, int i, bool selected, bool hasNext, CampaignActions a, Kit kit)
        {
            var s = Campaign.Stages[i];
            var open = c.StageOpen(i);
            var clear = c.IsCleared(s);
            var last = Last(s);
            var row = Ui.Node(parent, "Stage " + s.Id);
            Ui.Size(row, -1, 72);
            if (selected)
            {
                Ui.Bg(row, Ui.Alpha(Ui.Deep, 0.92f));
                var bar = Ui.Place(row, "Bar", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, 72f));
                bar.pivot = new Vector2(0f, 0.5f);
                Ui.Bg(bar, last ? Ui.Blood : Ui.Steel, null, false);
            }
            else Ui.Bg(row, Ui.Alpha(Ui.Ink, 0f));
            var nodeColor = last ? (open ? Ui.Blood : Ui.BloodDim) : clear ? Ui.Steel : open ? Ui.Sun : Ui.Line;
            if (hasNext)
            {
                var line = Ui.Place(row, "Line", new Vector2(0f, 0.5f), new Vector2(26f, -14f), new Vector2(2f, 44f));
                line.pivot = new Vector2(0.5f, 1f);
                Ui.Bg(line, clear ? Ui.Steel : Ui.Line, null, false);
            }
            var node = Ui.Place(row, "Node", new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(28f, 28f));
            node.pivot = new Vector2(0f, 0.5f);
            var disk = kit.S("SPR_DarkFantasyMenus_Frame_Ring_01_Background");
            Ui.Stretch(Ui.Sprite(node, "Fill", disk, clear ? nodeColor : Ui.Deep).rectTransform);
            var ring = Ui.Sprite(node, "Ring", kit.S("SPR_DarkFantasyMenus_Frame_Ring_01"), nodeColor);
            Ui.Stretch(ring.rectTransform, -3, -3, -3, -3);
            if (clear) Ui.Stretch(T(node, "✓", 17, Ui.Ink, true, false, TextAnchor.MiddleCenter).rectTransform);
            if (open && !clear) Fx.Pulse(ring);
            var text = Ui.Col(row, "Text", 0);
            text.anchorMin = new Vector2(0f, 0f);
            text.anchorMax = new Vector2(1f, 1f);
            text.offsetMin = new Vector2(56f, 10f);
            text.offsetMax = new Vector2(-110f, -10f);
            T(text, Loc.Text(s.Label), 14, Ui.Muted);
            var name = Ui.Row(text, "Name", 8, 0, null, null, TextAnchor.MiddleLeft);
            name.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            T(name, Loc.Text(s.Title), 17, last ? nodeColor : open ? Ui.Bone : Ui.Dim, open && !clear);
            var got = c.BonusDone.Contains(s.Id);
            T(name, got ? "★" : "☆", 14, got ? Ui.Candle : Ui.Line);
            var state = T(Ui.Place(row, "State", new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(100f, 20f)),
                Loc.T(clear ? "camp.cleared" : open ? "camp.open" : "camp.locked"), 14, clear ? Ui.Steel : open ? Ui.Sun : Ui.Dim, false, false, TextAnchor.MiddleRight);
            ((RectTransform)state.transform.parent).pivot = new Vector2(1f, 0.5f);
            Ui.Stretch(state.rectTransform);
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
            var panel = Ui.Node(root, "Info");
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            var x = Edge + RouteW + Gap;
            panel.offsetMin = new Vector2(x, Edge);
            panel.offsetMax = new Vector2(x + InfoW, -Top);
            kit.Frame(panel, Last(s) ? Ui.Blood : Ui.Steel);
            // The kit crest is a demon skull, so only the count's castle wears it (ui-style.md 5-3).
            if (Last(s)) kit.Prefab(panel, "Crest_DarkFantasyMenus_Large_01", new Vector2(0.5f, 1f), new Vector2(0f, 40f), 96f, Ui.Blood);
            var col = Ui.Col(panel, "Body", 10, 32);
            Ui.Stretch(col, 0, 40, 0, 96);
            col.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            T(col, Loc.Text(s.Label), 14, Ui.Muted);
            T(col, Loc.Text(s.Title), 32, Last(s) ? Ui.Blood : Ui.Bone, true, true);
            Kit.Wrap(T(col, Loc.Text(s.Brief), 17, Ui.Muted));
            var brk = Ui.Node(col, "Break");
            Ui.Size(brk, -1, 24);
            if (kit.Prefab(brk, "Component_DarkFantasyMenus_Section_Break_02", new Vector2(0.5f, 0.5f), Vector2.zero, 420f, Ui.Line) == null)
                Ui.Bg(Ui.Place(brk, "Line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 1f)), Ui.Line, null, false);
            Def(col, Loc.T("camp.objective"), Loc.T("camp.objectiveBoss", s.BossName), Ui.Bone);
            Def(col, Loc.T("camp.bonusGoal"), Loc.Text(s.Bonus.Text) + (bonusGot ? "  · " + Loc.T("camp.got") : ""), bonusGot ? Ui.Candle : Ui.Bone);
            var rew = Ui.Row(col, "Def Reward", 10, 0, null, null, TextAnchor.UpperLeft);
            rew.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(T(rew, Loc.T("camp.reward"), 17, Ui.Muted), 120);
            T(rew, Loc.T("camp.rewardClear", s.Reward) + (clear ? " (" + Loc.T("camp.got") + ")" : ""), 17, clear ? Ui.Dim : Ui.Bone);
            T(rew, "·  " + Loc.T("camp.rewardBonus", s.Bonus.Gold) + (bonusGot ? " (" + Loc.T("camp.got") + ")" : ""), 17, bonusGot ? Ui.Dim : Ui.Bone);
            Def(col, Loc.T("camp.resources"), Resources(s), Ui.Candle);
            Def(col, Loc.T("camp.enemies"), Loc.T("battle.enemyList", s.Enemies.Length - 1, s.BossName), Ui.Bone);
            T(col, Loc.T("camp.roster"), 14, Ui.Muted);
            var chips = Ui.Row(col, "Chips", 6, 0, null, null, TextAnchor.MiddleLeft);
            chips.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            foreach (var k in s.Units)
            {
                var t = GameData.Type(k);
                var isNew = t.UnlockAt == i + 1;
                var chip = Ui.Row(chips, "Chip", 4, 0, Ui.Alpha(Ui.Deep, 0.92f), isNew ? Ui.Candle : Ui.Line, TextAnchor.MiddleLeft);
                chip.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(6, 8, 4, 4);
                chip.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                Ui.Size(Ui.Sprite(chip, "Icon", kit.S(Kit.Weapon[k]), isNew ? Ui.Candle : Ui.Steel), 24, 24);
                T(chip, isNew ? Loc.T("camp.new", t.Name) : Loc.Text(t.Name), 14, isNew ? Ui.Candle : Ui.Bone);
            }

            var foot = Ui.Node(panel, "Foot");
            foot.anchorMin = new Vector2(0f, 0f);
            foot.anchorMax = new Vector2(1f, 0f);
            foot.pivot = new Vector2(0.5f, 0f);
            foot.offsetMin = new Vector2(32f, 28f);
            foot.offsetMax = new Vector2(-32f, 84f);
            if (!open)
            {
                var prev = Campaign.Stages[i - 1];
                Ui.Stretch(T(foot, Loc.T("camp.lockedBy", Loc.Text(prev.Title)), 14, Ui.Dim, false, false, TextAnchor.MiddleRight).rectTransform);
                return;
            }
            var btn = (RectTransform)kit.Btn(foot, Loc.T(clear ? "camp.marchAgain" : "camp.march"), () => a.Play?.Invoke(i), clear ? BtnKind.Normal : BtnKind.Primary, new Vector2(240f, 56f)).transform;
            btn.anchorMin = btn.anchorMax = btn.pivot = new Vector2(1f, 0.5f);
            btn.anchoredPosition = Vector2.zero;
            Kit.Hotkey(btn, "Enter");
            if (clear)
            {
                var note = T(Ui.Place(foot, "Note", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(320f, 20f)),
                    Loc.T(bonusGot ? "camp.allClaimed" : "camp.replayNote"), 14, Ui.Dim, false, false, TextAnchor.MiddleLeft);
                ((RectTransform)note.transform.parent).pivot = new Vector2(0f, 0.5f);
                Ui.Stretch(note.rectTransform);
            }
        }

        static void Def(Transform parent, string term, string value, Color color)
        {
            var row = Ui.Row(parent, "Def", 10, 0, null, null, TextAnchor.UpperLeft);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(T(row, term, 17, Ui.Muted), 120);
            Ui.Size(Kit.Wrap(T(row, value, 17, color)), 440, -1, 1);
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
            T(view, Loc.T("camp.barracks"), 22, Ui.Bone, true, true);
            Kit.Wrap(T(view, Loc.T("camp.barracksHelp"), 14, Ui.Muted));
            var open = GameData.Types.Where(t => c.UpgradesOpen(t.Kind)).ToList();
            if (open.Count == 0) Kit.Wrap(T(view, Loc.T("camp.noUpgrades"), 14, Ui.Dim));
            foreach (var t in open) ArmCard(view, c, t, v, a, kit);
        }

        static void ArmCard(Transform parent, Campaign c, UnitType t, CampView v, CampaignActions a, Kit kit)
        {
            var card = Ui.Col(parent, "Arm " + t.Key, 10, 16);
            kit.Frame(card, Ui.SteelDim);
            var head = Ui.Row(card, "Head", 10, 0, null, null, TextAnchor.MiddleLeft);
            head.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(Ui.Sprite(head, "Icon", kit.S(Kit.Weapon[t.Kind]), Ui.Steel), 40, 40);
            T(head, Loc.Text(t.Name), 22, Ui.Bone, true, true);
            Ui.Size(T(head, StatLine(Battle.Preview(t.Kind, c.Upgrades)), 14, Ui.Muted, false, false, TextAnchor.MiddleRight), -1, -1, 1);
            var ups = Ui.Row(card, "Ups", 8, 0, null, null, TextAnchor.UpperLeft);
            ups.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            foreach (var u in GameData.Upgrades.Where(x => x.Unit == t.Kind))
            {
                var own = c.Upgrades.Contains(u.Id);
                var box = Ui.Col(ups, "Up " + u.Id, 4, 10, Ui.Alpha(Ui.Deep, 0.92f), own ? Ui.Candle : Ui.Line);
                Ui.Size(box, -1, -1, 1);
                if (v.Bought == u.Id) Fx.Flash(box.GetComponent<Image>());
                var line = Ui.Row(box, "T", 8);
                Ui.Size(T(line, Loc.Text(u.Name), 17, Ui.Bone, true), -1, -1, 1);
                T(line, own ? Loc.T("camp.owned") : Loc.T("camp.cost", u.Cost), own ? 14 : 20, Ui.Candle, !own, false, TextAnchor.UpperRight);
                Kit.Wrap(T(box, Loc.Text(u.Desc), 14, Ui.Muted));
                var inh = Ui.Row(box, "Inherit", 4, 0, null, null, TextAnchor.MiddleLeft);
                inh.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                var ic = u.Inherit ? Ui.Spawn : Ui.Dim;
                Ui.Size(Ui.Sprite(inh, "Icon", kit.S("ICON_DarkFantasy_Status_Vampiric_01_Clean"), ic), 16, 16);
                T(inh, Loc.T(u.Inherit ? "camp.inherit" : "camp.noInherit"), 14, ic);
                if (own) continue;
                var acts = Ui.Row(box, "Acts", 0, 0, null, null, TextAnchor.MiddleRight);
                acts.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                var armed = v.BuyArm == u.Id;
                var id = u.Id;
                var afford = c.Gold >= u.Cost;
                kit.Btn(acts, armed ? Loc.T("camp.buyConfirm", u.Cost) : Loc.T("camp.upgrade"), () => a.Buy?.Invoke(id), armed ? BtnKind.DangerOn : BtnKind.Normal,
                    new Vector2(armed ? 220f : 120f, 40f), afford, afford ? null : Loc.T("camp.needGold", c.Gold, u.Cost));
            }
        }
    }
}
