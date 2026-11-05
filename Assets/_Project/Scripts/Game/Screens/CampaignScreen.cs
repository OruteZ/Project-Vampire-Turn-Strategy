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
            "체력 " + s.Hp + " · 공격 " + s.Atk + (s.Bane > 0 ? "(+" + s.Bane + ")" : "") + " · 방어 " + s.Def + " · 이동 " + s.Mov + " · 사거리 " + s.Rng;

        public static string Resources(Stage s) => "인구 " + s.Pop + (s.Silver > 0 ? " · 은 " + s.Silver : "");

        public static void Build(RectTransform root, Campaign c, bool resetArm, string buyArm, CampaignActions a)
        {
            var page = Ui.Col(root, "Campaign", 14, 16);
            Ui.Stretch(page);

            var head = Ui.Row(page, "Head", 12, 0, null, null, TextAnchor.LowerLeft);
            var title = Ui.Col(head, "Title", 2);
            Ui.Size(title, -1, -1, 1);
            Ui.Text(title, "캠페인", 0.75f, Ui.Muted);
            Ui.Text(title, "뱀파이어 토벌 원정", 1.55f, Ui.Bone, true, true);
            Ui.Text(head, c.Gold + "\n<size=" + Mathf.RoundToInt(Ui.Base * 0.68f) + "><color=#" + Ui.Hex(Ui.Muted) + ">금화</color></size>", 1.3f, Ui.Candle, true);
            Ui.Button(head, resetArm ? "정말 초기화할까요?" : "진행 초기화", a.Reset, Look.Ghost, true);
            Ui.Button(head, "메뉴로", a.Menu, Look.Normal, true);

            var grid = Ui.Row(page, "Grid", 16, 0, null, null, TextAnchor.UpperLeft);
            Ui.Size(grid, -1, -1, 1, 1);
            var hg = grid.GetComponent<HorizontalLayoutGroup>();
            hg.childForceExpandHeight = true;

            var left = Ui.Scroll(grid, "Stages", out var ls);
            Ui.Size(ls, -1, -1, 1f, 1);
            var intro = Ui.Card(left, "Intro");
            Ui.Text(intro, "장 선택", 1.05f, Ui.Bone, true, true);
            Ui.Text(intro, "각 장을 클리어하면 다음 장과 새 유닛이 열립니다. 추가 목표 보상은 처음 달성할 때 한 번만 받습니다. 금화는 모든 강화를 사기에 부족하니 주력을 골라 투자하세요.", 0.9f, Ui.Muted);
            for (var i = 0; i < Campaign.Stages.Length; i++) StageCard(left, c, i, a);

            var right = Ui.Scroll(grid, "Armory", out var rs);
            Ui.Size(rs, -1, -1, 1.25f, 1);
            var arm = Ui.Card(right, "Barracks");
            Ui.Text(arm, "병영 · 유닛 강화", 1.05f, Ui.Bone, true, true);
            Ui.Text(arm, "강화는 같은 병종 전체에 적용되고, 한 번 사면 되돌릴 수 없습니다. 유닛이 해금된 장을 클리어한 뒤부터 그 유닛을 강화할 수 있습니다. 스폰이 되면 체력·공격·이동 강화만 물려받습니다.", 0.9f, Ui.Muted);
            var open = GameData.Types.Where(t => c.UpgradesOpen(t.Kind)).ToList();
            if (open.Count == 0)
            {
                var none = Ui.Card(right, "Locked");
                Ui.Text(none, "아직 강화할 수 있는 병종이 없습니다. 1장을 클리어하면 농민군 강화가 열립니다.", 0.9f, Ui.Muted);
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
            Ui.Text(name, s.Label, 0.72f, Ui.Muted);
            Ui.Text(name, s.Title, 1.12f, Ui.Bone, true, true);
            Ui.Text(row, clear ? "클리어" : open ? "도전 가능" : "잠김", 0.75f, clear ? Ui.Candle : open ? Ui.Bone : Ui.Muted);
            Ui.Text(card, s.Brief, 0.86f, Ui.Muted);
            var chips = Ui.Row(card, "Chips", 6);
            foreach (var k in s.Units)
            {
                var t = GameData.Type(k);
                var isNew = t.UnlockAt == i + 1;
                var chip = Ui.Row(chips, "Chip", 0, 3, Ui.Panel2, isNew ? Ui.Candle : Ui.Line);
                chip.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(8, 8, 2, 2);
                Ui.Text(chip, (isNew ? "해금 · " : "") + t.Name, 0.75f, isNew ? Ui.Candle : Ui.Bone).horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            var bon = Ui.Row(card, "Bonus", 14);
            var got = c.BonusDone.Contains(s.Id);
            Ui.Text(bon, "클리어 +" + s.Reward, 0.78f, Ui.Muted).horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Text(bon, (got ? "✓" : "○") + " " + s.Bonus.Text + " +" + s.Bonus.Gold, 0.78f, got ? Ui.Candle : Ui.Muted).horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Text(bon, Resources(s), 0.78f, Ui.Muted).horizontalOverflow = HorizontalWrapMode.Overflow;
            if (open)
            {
                var acts = Ui.Row(card, "Acts", 8);
                var index = i;
                Ui.Button(acts, "출전", () => a.Play?.Invoke(index), clear ? Look.Normal : Look.Primary, true);
            }
        }

        static void ArmCard(Transform parent, Campaign c, UnitType t, string buyArm, CampaignActions a)
        {
            var card = Ui.Card(parent, "Arm " + t.Key);
            var head = Ui.Row(card, "Head", 10);
            Ui.Glyph(head, t);
            Ui.Text(head, t.Name, 1f, Ui.Bone, true, true).horizontalOverflow = HorizontalWrapMode.Overflow;
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
                Ui.Size(Ui.Text(line, u.Name, 0.95f, Ui.Bone, true), -1, -1, 1);
                Ui.Text(line, own ? "보유" : u.Cost + " 금화", 0.85f, Ui.Muted).horizontalOverflow = HorizontalWrapMode.Overflow;
                Ui.Text(box, u.Desc, 0.8f, Ui.Muted);
                Ui.Text(box, u.Inherit ? "스폰이 물려받음" : "스폰이 물려받지 않음", 0.68f, u.Inherit ? Ui.Spawn : Ui.Muted);
                if (!own)
                {
                    var id = u.Id;
                    var acts = Ui.Row(box, "Acts", 0);
                    var armed = buyArm == u.Id;
                    Ui.Button(acts, armed ? "정말 살까요? " + u.Cost + " 금화" : "강화", () => a.Buy?.Invoke(id), armed ? Look.DangerOn : Look.Normal, true, c.Gold >= u.Cost);
                }
            }
        }
    }
}
