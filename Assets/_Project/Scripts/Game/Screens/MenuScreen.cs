using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    public static class MenuScreen
    {
        public static string CampMeta(Campaign c) =>
            Campaign.Stages.Count(c.IsCleared) + "/" + Campaign.Stages.Length + "장 클리어 · 금화 " + c.Gold;

        public static void Build(RectTransform root, Campaign campaign, Action demo, Action camp)
        {
            var page = Ui.Col(root, "Menu", 28);
            page.anchorMin = page.anchorMax = new Vector2(0.5f, 0.5f);
            page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = new Vector2(760f, 520f);
            page.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

            var head = Ui.Col(page, "Head", 8);
            Ui.Text(head, "PROJECT VAMPIRE SPAWN", 3.2f, Ui.Bone, true, true);
            Ui.Text(head, "육각 타일 턴제 전술. 뱀파이어에게 쓰러진 병사는 스폰이 되어 아군을 공격합니다. 빛을 지키고, 살릴 수 없는 아군은 직접 보내세요.", 1f, Ui.Muted);

            var choices = Ui.Row(page, "Choices", 12, 0, null, null, TextAnchor.UpperLeft);
            choices.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            Choice(choices, "데모 전투", "모든 유닛을 쓸 수 있는 단일 전투. 강화 없음.", CampaignScreen.Resources(GameData.Stages[0]), demo);
            Choice(choices, "캠페인", "농민군에서 사냥꾼까지. 장을 넘길 때마다 유닛이 해금되고, 금화로 유닛을 강화합니다.", CampMeta(campaign), camp);
        }

        static void Choice(Transform parent, string title, string desc, string meta, Action click)
        {
            var b = Ui.Button(parent, null, click, Look.Choice);
            var row = b.GetComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(18, 18, 18, 18);
            row.childAlignment = TextAnchor.UpperLeft;
            Ui.Size(b, -1, -1, 1);
            var col = Ui.Col(b.transform, "Choice", 6);
            Ui.Size(col, -1, -1, 1);
            Ui.Text(col, title, 1.25f, Ui.Bone, true, true);
            Ui.Text(col, desc, 0.88f, Ui.Muted);
            Ui.Text(col, meta, 0.8f, Ui.Candle);
        }
    }
}
