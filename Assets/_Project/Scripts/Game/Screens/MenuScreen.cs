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
            Loc.T("menu.campMeta", Campaign.Stages.Count(c.IsCleared), Campaign.Stages.Length, c.Gold);

        public static void Build(RectTransform root, Campaign campaign, Action demo, Action camp)
        {
            var page = Ui.Col(root, "Menu", 28);
            page.anchorMin = page.anchorMax = new Vector2(0.5f, 0.5f);
            page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = new Vector2(760f, 520f);
            page.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

            var head = Ui.Col(page, "Head", 8);
            Ui.Text(head, "PROJECT VAMPIRE SPAWN", 3.2f, Ui.Bone, true, true);
            Ui.Text(head, Loc.T("menu.tagline"), 1f, Ui.Muted);

            var choices = Ui.Row(page, "Choices", 12, 0, null, null, TextAnchor.UpperLeft);
            choices.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            Choice(choices, Loc.T("menu.demo"), Loc.T("menu.demoDesc"), CampaignScreen.Resources(GameData.Stages[0]), demo);
            Choice(choices, Loc.T("menu.campaign"), Loc.T("menu.campaignDesc"), CampMeta(campaign), camp);
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
