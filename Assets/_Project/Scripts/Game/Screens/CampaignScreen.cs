using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    public sealed class CampaignActions
    {
        public Action<int> Play;
        public Action<string> Buy;
        public Action Reset, Menu;
    }

    public static class CampaignScreen
    {
        public static string StatLine(Unit s) =>
            Loc.T("camp.statLine", s.Hp, s.Atk + (s.Bane > 0 ? "(+" + s.Bane + ")" : ""), s.Def, s.Mov, s.Rng);

        public static string Resources(Stage s) => Resources(s.Pop, s.Silver);

        public static string Resources(int pop, int silver) => Loc.T("camp.pop", pop) + (silver > 0 ? " · " + Loc.T("camp.silver", silver) : "");

        public static void Build(RectTransform root, Campaign c, bool resetArm, string buyArm, CampaignActions a)
        {
            var page = Ui.Col(root, "Campaign", 14, 16);
            Ui.Stretch(page);

            var head = Ui.Row(page, "Head", 12, 0, null, null, TextAnchor.LowerLeft);
            var title = Ui.Col(head, "Title", 2);
            Ui.Size(title, -1, -1, 1);
            Ui.Text(title, Loc.T("camp.title"), 0.75f, Ui.Muted);
            Ui.Text(title, Loc.T("camp.heading"), 1.55f, Ui.Bone, true, true);
            Ui.Text(head, c.Gold + "\n<size=" + Mathf.RoundToInt(Ui.Base * 0.68f) + "><color=#" + Ui.Hex(Ui.Muted) + ">" + Loc.T("camp.gold") + "</color></size>", 1.3f, Ui.Candle, true);
            Ui.Button(head, resetArm ? Loc.T("camp.resetConfirm") : Loc.T("camp.reset"), a.Reset, Look.Ghost, true);
            Ui.Button(head, Loc.T("camp.menu"), a.Menu, Look.Normal, true);

            var grid = Ui.Row(page, "Grid", 16, 0, null, null, TextAnchor.UpperLeft);
            Ui.Size(grid, -1, -1, 1, 1);
            var hg = grid.GetComponent<HorizontalLayoutGroup>();
            hg.childForceExpandHeight = true;

            var left = Ui.Scroll(grid, "Stages", out var ls);
            Ui.Size(ls, -1, -1, 1f, 1);
            var intro = Ui.Card(left, "Intro");
            Ui.Text(intro, Loc.T("camp.chapters"), 1.05f, Ui.Bone, true, true);
            Ui.Text(intro, Loc.T("camp.intro"), 0.9f, Ui.Muted);
            for (var i = 0; i < Campaign.Stages.Length; i++) StageCard(left, c, i, a);

            var right = Ui.Scroll(grid, "Armory", out var rs);
            Ui.Size(rs, -1, -1, 1.25f, 1);
            var arm = Ui.Card(right, "Barracks");
            Ui.Text(arm, Loc.T("camp.barracks"), 1.05f, Ui.Bone, true, true);
            Ui.Text(arm, Loc.T("camp.barracksHelp"), 0.9f, Ui.Muted);
            var open = GameData.Types.Where(t => c.UpgradesOpen(t.Kind)).ToList();
            if (open.Count == 0)
            {
                var none = Ui.Card(right, "Locked");
                Ui.Text(none, Loc.T("camp.noUpgrades"), 0.9f, Ui.Muted);
            }
            foreach (var t in open) ArmCard(right, c, t, buyArm, a);
        }

        static void StageCard(Transform parent, Campaign c, int i, CampaignActions a)
        {
            var s = Campaign.Stages[i];
            var open = c.StageOpen(i);
            var clear = c.IsCleared(s);
            var card = Ui.Card(parent, "Stage " + s.Id);
            if (!open) card.gameObject.AddComponent<CanvasGroup>().alpha = 0.5f;
            var row = Ui.Row(card, "Row", 10, 0, null, null, TextAnchor.UpperLeft);
            var name = Ui.Col(row, "Name", 0);
            Ui.Size(name, -1, -1, 1);
            Ui.Text(name, Loc.Text(s.Label), 0.72f, Ui.Muted);
            Ui.Text(name, Loc.Text(s.Title), 1.12f, Ui.Bone, true, true);
            Ui.Text(row, clear ? Loc.T("camp.cleared") : open ? Loc.T("camp.open") : Loc.T("camp.locked"), 0.75f, clear ? Ui.Candle : open ? Ui.Bone : Ui.Muted);
            Ui.Text(card, Loc.Text(s.Brief), 0.86f, Ui.Muted);
            var chips = Ui.Row(card, "Chips", 6);
            foreach (var k in s.Units)
            {
                var t = GameData.Type(k);
                var isNew = t.UnlockAt == i + 1;
                var chip = Ui.Row(chips, "Chip", 0, 3, Ui.Panel2, isNew ? Ui.Candle : Ui.Line);
                chip.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(8, 8, 2, 2);
                Ui.Text(chip, isNew ? Loc.T("camp.new", t.Name) : Loc.Text(t.Name), 0.75f, isNew ? Ui.Candle : Ui.Bone).horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            var bon = Ui.Row(card, "Bonus", 14);
            var got = c.BonusDone.Contains(s.Id);
            Ui.Text(bon, Loc.T("camp.clearReward", s.Reward), 0.78f, Ui.Muted).horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Text(bon, (got ? "✓" : "○") + " " + Loc.Text(s.Bonus.Text) + " +" + s.Bonus.Gold, 0.78f, got ? Ui.Candle : Ui.Muted).horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Text(bon, Resources(s), 0.78f, Ui.Muted).horizontalOverflow = HorizontalWrapMode.Overflow;
            if (open)
            {
                var acts = Ui.Row(card, "Acts", 8);
                var index = i;
                Ui.Button(acts, Loc.T("camp.march"), () => a.Play?.Invoke(index), clear ? Look.Normal : Look.Primary, true);
            }
        }

        static void ArmCard(Transform parent, Campaign c, UnitType t, string buyArm, CampaignActions a)
        {
            var card = Ui.Card(parent, "Arm " + t.Key);
            var head = Ui.Row(card, "Head", 10);
            Ui.Glyph(head, t);
            Ui.Text(head, Loc.Text(t.Name), 1f, Ui.Bone, true, true).horizontalOverflow = HorizontalWrapMode.Overflow;
            var basis = Ui.Text(head, StatLine(Battle.Preview(t.Kind, c.Upgrades)), 0.75f, Ui.Muted, false, false, TextAnchor.MiddleRight);
            Ui.Size(basis, -1, -1, 1);
            var ups = Ui.Row(card, "Ups", 8, 0, null, null, TextAnchor.UpperLeft);
            ups.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            foreach (var u in GameData.Upgrades.Where(x => x.Unit == t.Kind))
            {
                var own = c.Upgrades.Contains(u.Id);
                var box = Ui.Col(ups, "Up " + u.Id, 4, 8, Ui.Panel2, own ? Ui.Candle : Ui.Line);
                Ui.Size(box, -1, -1, 1);
                var line = Ui.Row(box, "T", 8);
                Ui.Size(Ui.Text(line, Loc.Text(u.Name), 0.95f, Ui.Bone, true), -1, -1, 1);
                Ui.Text(line, own ? Loc.T("camp.owned") : Loc.T("camp.cost", u.Cost), 0.85f, Ui.Muted).horizontalOverflow = HorizontalWrapMode.Overflow;
                Ui.Text(box, Loc.Text(u.Desc), 0.8f, Ui.Muted);
                Ui.Text(box, u.Inherit ? Loc.T("camp.inherit") : Loc.T("camp.noInherit"), 0.68f, u.Inherit ? Ui.Spawn : Ui.Muted);
                if (!own)
                {
                    var id = u.Id;
                    var acts = Ui.Row(box, "Acts", 0);
                    var armed = buyArm == u.Id;
                    Ui.Button(acts, armed ? Loc.T("camp.buyConfirm", u.Cost) : Loc.T("camp.upgrade"), () => a.Buy?.Invoke(id), armed ? Look.DangerOn : Look.Normal, true, c.Gold >= u.Cost);
                }
            }
        }
    }
}
