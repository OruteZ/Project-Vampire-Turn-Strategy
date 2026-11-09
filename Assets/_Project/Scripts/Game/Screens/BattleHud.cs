using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    public sealed class HudState
    {
        public Unit Hover;
        public Corpse HoverCorpse;
        public bool Alt, EnemyPlaying, LeaveArmed, RulesOpen, LogOpen, Demo, FullMoon, MoonFading;
        public IEnumerable<string> Upgrades = Enumerable.Empty<string>();
    }

    // docs/ui/battle-hud.md in the docs/art/ui-style.md tokens. Sizes are 1920x1080 px.
    public sealed class BattleHud
    {
        const float Edge = 24f, FramePpu = Kit.FramePpu, PanelW = 340f, PanelH = 190f;

        sealed class HeadMark
        {
            public RectTransform Rt;
            public Image[] Cells;
            public int Per, MaxHp;
            public Color Color;
        }

        readonly ArtCatalog art;
        readonly List<Graphic> pulse = new List<Graphic>();
        readonly HashSet<Graphic> blink = new HashSet<Graphic>();
        readonly Dictionary<int, HeadMark> heads = new Dictionary<int, HeadMark>();
        RectTransform headLayer, hoverLayer;
        // Values and panels of the previous build, so only real changes flash and only new panels slide in (ui-style.md 12).
        Dictionary<string, string> lastValues = new Dictionary<string, string>(), values = new Dictionary<string, string>();
        HashSet<string> lastPanels = new HashSet<string>(), panels = new HashSet<string>();

        public BattleHud(ArtCatalog art) { this.art = art; kit = new Kit(art); }

        public bool HasHeads => heads.Count > 0;

        readonly Kit kit;

        Sprite S(string name) => kit.S(name);

        static Text T(Transform p, string s, float px, Color c, bool bold = false, bool display = false, TextAnchor a = TextAnchor.UpperLeft) => Kit.T(p, s, px, c, bold, display, a);

        static Text Wrap(Text t) => Kit.Wrap(t);

        void Frame(RectTransform rt, Color edge, float ppu = Kit.FramePpu) => kit.Frame(rt, edge, ppu);

        Button Btn(Transform parent, string label, Action click, BtnKind kind, Vector2 size, bool enabled = true, string tip = null) => kit.Btn(parent, label, click, kind, size, enabled, tip);

        static void Hotkey(RectTransform button, string key) => Kit.Hotkey(button, key);

        string Weapon(string key) => kit.Weapon(key);


        public void Build(RectTransform root, BattleSession s, HudState st, BattleActions a)
        {
            pulse.Clear();
            (lastValues, values) = (values, lastValues);
            values.Clear();
            var b = s.Battle;
            Shade(root);
            var hot = b.Phase == Phase.Player && b.MoonNext() && !st.FullMoon;
            if (st.FullMoon || st.MoonFading)
            {
                var vg = Vignette(root, 0.15f);
                if (st.MoonFading) Fx.FadeOut(vg, 0.4f);
            }
            else if (hot) Vignette(root, 0.08f);
            headLayer = Ui.Node(root, "Heads");
            Ui.Stretch(headLayer);
            Moon(root, b, hot, st.FullMoon, st.MoonFading);
            if (b.Phase == Phase.Deploy) Deploy(root, s, st, a);
            else Fight(root, s, st, a);
            hoverLayer = Ui.Node(root, "Hover");
            Ui.Stretch(hoverLayer);
            Hover(s, st, a);
            EndTurn(root, b, st, a);
            Corner(root, st, a);
            if (BattleScreen.ShowsHint(s, st.EnemyPlaying))
                Ui.Stretch(T(Ui.Place(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, Edge + 96f), new Vector2(1000f, 24f)),
                    BattleScreen.Hint(s, st.EnemyPlaying), 17, Ui.Muted, false, false, TextAnchor.MiddleCenter).rectTransform);
            if (st.RulesOpen) RulesPopup(root, a);
        }

        // Hovering only redraws the unit panels and heads; rebuilding the whole HUD per hovered cell stalled the frame.
        public void RebuildHover(BattleSession s, HudState st, BattleActions a)
        {
            if (hoverLayer == null || headLayer == null) return;
            lastValues.Clear();
            foreach (var kv in values) lastValues[kv.Key] = kv.Value;
            Ui.Clear(headLayer);
            Ui.Clear(hoverLayer);
            Hover(s, st, a);
        }

        void Hover(BattleSession s, HudState st, BattleActions a)
        {
            blink.Clear();
            heads.Clear();
            (lastPanels, panels) = (panels, lastPanels);
            panels.Clear();
            if (s.Battle.Phase == Phase.Deploy) DeployHover(hoverLayer, s, st, a);
            else FightHover(hoverLayer, s, st, a);
        }

        // ---------- per frame ----------

        public void Tick(Camera cam, BattleView view)
        {
            var k = 1f + 0.1f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 1.2f);
            foreach (var g in pulse) { var c = Ui.MoonBlood * k; c.a = 1f; g.color = c; }
            var on = Time.unscaledTime % 0.5f < 0.25f;
            foreach (var g in blink) g.color = Ui.Alpha(g.color, on ? 1f : 0.15f);
            if (cam == null || art == null) return;
            foreach (var kv in heads)
            {
                var v = view.ViewOf(kv.Key);
                var h = kv.Value;
                var sp = v != null && !v.Lying ? cam.WorldToScreenPoint(v.transform.position + Vector3.up * (art.unitHeight * art.hexSize + 0.15f)) : Vector3.back;
                h.Rt.gameObject.SetActive(sp.z > 0f);
                if (sp.z <= 0f) continue;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)h.Rt.parent, sp, null, out var lp)) h.Rt.localPosition = lp;
                // The view's hp follows the hit animations; the model already holds the result.
                var full = Mathf.CeilToInt(v.HpFraction * h.MaxHp / h.Per - 0.001f);
                for (var i = 0; i < h.Cells.Length; i++)
                    if (!blink.Contains(h.Cells[i])) h.Cells[i].color = i < full ? h.Color : Ui.Deep;
            }
        }

        // ---------- shared pieces ----------

        static Sprite edgeFade, wideFade;

        // stage-dressing.md 1-6: the world runs behind the HUD, so the outer quarter darkens to keep HUD text readable.
        public static Image Shade(Transform root)
        {
            var shade = Ui.Sprite(root, "Shade", WideFade(), Ui.Alpha(Ui.Ink, 0.55f));
            shade.preserveAspect = false;
            Ui.Stretch(shade.rectTransform);
            return shade;
        }

        static Sprite WideFade()
        {
            if (wideFade != null) return wideFade;
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < N; y++)
                for (var x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                    var a = Mathf.Clamp01(1f - Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) / 0.25f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return wideFade = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        // ui-style.md 7-2: an 80 px fade on all four edges, as one full-screen texture.
        static Sprite EdgeFade()
        {
            if (edgeFade != null) return edgeFade;
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < N; y++)
                for (var x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                    var a = Mathf.Max(Mathf.Clamp01(1f - Mathf.Min(u, 1f - u) / (80f / 1920f)), Mathf.Clamp01(1f - Mathf.Min(v, 1f - v) / (80f / 1080f)));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return edgeFade = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        Image Vignette(RectTransform root, float alpha)
        {
            var img = Ui.Sprite(root, "Vignette", EdgeFade(), Ui.Alpha(Ui.MoonBlood, alpha));
            img.preserveAspect = false;
            Ui.Stretch(img.rectTransform);
            return img;
        }

        Text Watch(Text t, string key)
        {
            values[key] = t.text;
            if (lastValues.TryGetValue(key, out var was) && was != t.text) Fx.Flash(t);
            return t;
        }

        // full: the full-moon enemy turn is playing (7-2); fading: it just ended, so the blood drains away over 0.4 s.
        void Moon(RectTransform root, Battle b, bool hot, bool full, bool fading)
        {
            const float D = 112f;
            var box = Ui.Place(root, "Moon", new Vector2(0.5f, 1f), new Vector2(0f, -Edge), new Vector2(D, D));
            var disk = S("SPR_DarkFantasyMenus_Frame_Ring_01_Background");
            Ui.Stretch(Ui.Sprite(box, "Back", disk, Ui.Deep).rectTransform);
            var max = Rules.MoonMax;
            var moon = full ? max : b.Moon;
            for (var i = 0; i < max; i++)
            {
                var img = Ui.Sprite(box, "Phase " + i, disk, i < moon ? (hot || full ? Ui.MoonBlood : Ui.Sun) : Ui.Panel2);
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = (int)Image.Origin360.Top;
                img.fillClockwise = true;
                img.fillAmount = 1f / max - 0.015f;
                img.preserveAspect = true;
                Ui.Stretch(img.rectTransform, 12, 12, 12, 12);
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, -360f * i / max);
                if (hot && i < moon) pulse.Add(img);
                if (fading)
                {
                    var drain = Ui.Sprite(box, "Drain " + i, disk, Ui.MoonBlood);
                    drain.type = Image.Type.Filled;
                    drain.fillMethod = Image.FillMethod.Radial360;
                    drain.fillOrigin = (int)Image.Origin360.Top;
                    drain.fillAmount = img.fillAmount;
                    drain.preserveAspect = true;
                    Ui.Stretch(drain.rectTransform, 12, 12, 12, 12);
                    drain.rectTransform.localEulerAngles = img.rectTransform.localEulerAngles;
                    Fx.FadeOut(drain, 0.4f);
                }
            }
            var v = b.Vampire;
            if (v != null && v.Asleep)
            {
                var pivot = Ui.Place(box, "WakeAt", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                pivot.localEulerAngles = new Vector3(0f, 0f, -360f * Rules.MoonWake / max);
                Ui.Bg(Ui.Place(pivot, "Coffin", new Vector2(0.5f, 0.5f), new Vector2(0f, D / 2f - 14f), new Vector2(5f, 26f)), Ui.Line, null, false);
            }
            Ui.Stretch(Ui.Sprite(box, "Ring", S("SPR_DarkFantasyMenus_Frame_Ring_01"), hot || full ? Ui.MoonBlood : Ui.Steel).rectTransform, -6, -6, -6, -6);
            if (full)
                for (var k = 0; k < 4; k++)
                {
                    var pivot = Ui.Place(box, "SkullAt", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    pivot.localEulerAngles = new Vector3(0f, 0f, 45f + 90f * k);
                    Ui.Sprite(pivot, "Skull", S("SPR_DarkFantasyMenus_Greeble_DemonSkull_01"), Ui.MoonBlood).rectTransform.anchoredPosition = new Vector2(0f, D / 2f + 6f);
                    var sk = (RectTransform)pivot.GetChild(0);
                    sk.sizeDelta = new Vector2(28f, 28f);
                    sk.localEulerAngles = Vector3.zero;
                }
            var label = Ui.Col(root, "MoonLabel", 0);
            label.anchorMin = label.anchorMax = label.pivot = new Vector2(0.5f, 1f);
            label.anchoredPosition = new Vector2(0f, -Edge - D - 6f);
            label.sizeDelta = new Vector2(300f, 44f);
            Watch(T(label, moon + "/" + max, 20, Ui.Bone, true, false, TextAnchor.UpperCenter), "moon");
            HoverTip.Add(box, Loc.T("battle.moon") + " · " + Loc.T("battle.rule.moon", Rules.MoonMax), 0.4f);
            var state = hot ? Loc.T("moon.nextTurn") : v != null && v.Asleep ? Loc.T("battle.coffinAt", Rules.MoonWake) : "";
            if (state.Length > 0) T(label, state, 14, hot ? Ui.MoonBlood : Ui.Muted, hot, false, TextAnchor.UpperCenter);
        }

        void EndTurn(RectTransform root, Battle b, HudState st, BattleActions a)
        {
            var label = BattleScreen.PrimaryLabel(b, st.EnemyPlaying, out var enabled);
            var tip = b.Phase == Phase.Deploy && !enabled ? Loc.T("hud.needAlly") : null;
            var btn = Btn(root, label, a.Primary, BtnKind.Primary, new Vector2(200f, 56f), enabled, tip);
            var rt = (RectTransform)btn.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-Edge, Edge);
            Hotkey(rt, "Space");
            var left = b.Phase == Phase.Player && !st.EnemyPlaying ? b.Allies.Count(u => !u.Acted) : 0;
            if (left > 0)
            {
                // ui-style.md 14-3-3: inside the button, left of the label.
                var badge = kit.Badge(rt, left.ToString(), 28, Ui.Steel, Ui.Ink, Ui.Bone);
                badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(0f, 0.5f);
                badge.anchoredPosition = new Vector2(12f, 0f);
                badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                HoverTip.Add(badge, Loc.T("battle.allyPhase"), 0.3f);
            }
        }

        void Corner(RectTransform root, HudState st, BattleActions a)
        {
            var row = Ui.Row(root, "Corner", 4, 0, null, null, TextAnchor.LowerLeft);
            row.anchorMin = row.anchorMax = row.pivot = Vector2.zero;
            row.anchoredPosition = new Vector2(Edge - 12f, Edge - 6f);
            row.sizeDelta = new Vector2(420f, 40f);
            row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
            kit.IconBtn(row, Kit.Icon.Rules, Loc.T("battle.rules"), a.ToggleRules);
            if (st.LeaveArmed) Btn(row, Loc.T("battle.leaveConfirm"), a.Leave, BtnKind.Danger, new Vector2(220f, 40f), !st.EnemyPlaying);
            else kit.IconBtn(row, Kit.Icon.Leave, Loc.T("battle.leave"), a.Leave, 40f, null, !st.EnemyPlaying);
        }

        void RulesPopup(RectTransform root, BattleActions a)
        {
            var back = Ui.Node(root, "RulesBack");
            Ui.Stretch(back);
            Ui.Bg(back, Ui.Alpha(Ui.Ink, 0.85f));
            back.gameObject.AddComponent<Button>().onClick.AddListener(() => a.ToggleRules?.Invoke());
            var p = Ui.Col(back, "RulesPopup", 8, 32);
            p.anchorMin = p.anchorMax = p.pivot = new Vector2(0.5f, 0.5f);
            p.sizeDelta = new Vector2(720f, 0f);
            p.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            p.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            Ui.Frame(p, S("SPR_DarkFantasyMenus_Frame_Box_Medium_02"), S("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"), Ui.Steel, FramePpu);
            T(p, Loc.T("battle.rules"), 32, Ui.Bone, true, true);
            foreach (var line in BattleScreen.RuleLines) Wrap(T(p, line, 17, Ui.Muted));
            var row = Ui.Row(p, "Close", 0, 0, null, null, TextAnchor.MiddleRight);
            Btn(row, Loc.T("hud.close"), a.ToggleRules, BtnKind.Ghost, new Vector2(100f, 36f));
        }

        // ---------- deploy (battle-hud.md 2) ----------

        void Deploy(RectTransform root, BattleSession s, HudState st, BattleActions a)
        {
            var b = s.Battle;
            var stg = b.Stage;
            var title = Ui.Col(root, "Title", 8);
            title.anchorMin = title.anchorMax = title.pivot = new Vector2(0f, 1f);
            title.anchoredPosition = new Vector2(Edge, -Edge);
            title.sizeDelta = new Vector2(PanelW, 110f);
            var name = Ui.Row(title, "Name", 12, 0, null, null, TextAnchor.MiddleLeft);
            name.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var last = stg == Campaign.Stages.LastOrDefault();
            var num = Array.IndexOf(Campaign.Stages, stg) + 1;
            if (num > 0 && !st.Demo) kit.Badge(name, num.ToString(), 40, last ? Ui.Blood : Ui.Steel, Ui.Deep, Ui.Bone);
            var tt = T(name, Loc.Text(stg.Title), 32, last ? Ui.Blood : Ui.Bone, true, true, TextAnchor.MiddleLeft);
            Ui.Ellipsize(tt, PanelW - 56);
            HoverTip.Add(name, BattleScreen.Subtitle(stg, st.Demo), 0.4f);
            var res = Ui.Row(title, "Resources", 24, 0, null, null, TextAnchor.MiddleLeft);
            res.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            kit.Value(res, Kit.Icon.Pop, b.Used(t => t.Pop) + "/" + stg.Pop, 22, Ui.Candle, Ui.Candle, Loc.T("battle.pop"));
            if (stg.Silver > 0) kit.Value(res, Kit.Icon.Silver, b.Used(t => t.Silver) + "/" + stg.Silver, 22, Ui.Candle, Ui.Candle, Loc.T("battle.silver"));
            kit.Value(res, Kit.Icon.Flag, b.Allies.Count().ToString(), 22, Ui.Steel, Ui.Bone, Loc.T("battle.deployed"));

            var roster = Ui.Col(root, "Roster", 8);
            roster.anchorMin = roster.anchorMax = roster.pivot = new Vector2(0f, 1f);
            roster.anchoredPosition = new Vector2(Edge, -Edge - 130f);
            roster.sizeDelta = new Vector2(PanelW, 0f);
            roster.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            roster.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 16, 16);
            Frame(roster, Ui.Steel);
            T(roster, Loc.T("battle.formation"), 22, Ui.Bone, true, true);
            var pop = b.Used(t => t.Pop);
            var sil = b.Used(t => t.Silver);
            foreach (var k in stg.Units)
            {
                var t = GameData.Type(k);
                var can = pop + t.Pop <= stg.Pop && sil + t.Silver <= stg.Silver;
                var kind = k;
                RosterRow(roster, t, k, s.DeployType == k, can, st.Upgrades, () => a.Pick?.Invoke(kind));
            }
            var acts = Ui.Row(roster, "Acts", 8);
            acts.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
            Btn(acts, Loc.T("battle.recallAll"), a.Clear, BtnKind.Normal, new Vector2(150f, 40f));
            Btn(acts, Loc.T("battle.sample"), a.Sample, BtnKind.Normal, new Vector2(150f, 40f));

            var brief = Ui.Col(root, "Brief", 6);
            brief.anchorMin = brief.anchorMax = brief.pivot = new Vector2(1f, 1f);
            brief.anchoredPosition = new Vector2(-Edge, -Edge);
            brief.sizeDelta = new Vector2(PanelW, 0f);
            brief.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            brief.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 16, 16);
            Frame(brief, Ui.SteelDim);
            brief.GetComponent<VerticalLayoutGroup>().spacing = 10;
            HoverTip.Add(brief, Loc.Text(stg.Brief), 0.6f);
            Def(brief, Kit.Icon.Boss, Ui.Blood, Loc.T("battle.defeatBoss", stg.BossName), Loc.T("battle.objective"));
            Def(brief, Kit.Icon.Star, Ui.Candle, Loc.Text(stg.Bonus.Text), Loc.T("battle.bonus"));
            Def(brief, Kit.Icon.Skulls, Ui.Blood, Kit.EnemyList(stg), Loc.T("battle.enemy"));
        }

        void DeployHover(RectTransform root, BattleSession s, HudState st, BattleActions a)
        {
            var b = s.Battle;
            var ins = s.InspectUnit is int id ? b.Find(id) : null;
            var show = st.Hover ?? ins;
            if (show != null)
            {
                var p = Panel(root, b, show, new Vector2(1f, 0f), new Vector2(-Edge, Edge + 100f), true);
                if (show.Side == Side.Ally)
                {
                    var uid = show.Id;
                    var r = (RectTransform)Btn(p, Loc.T("unit.card.recall"), () => a.Recall?.Invoke(uid), BtnKind.Normal, new Vector2(90f, 32f)).transform;
                    r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
                    r.anchoredPosition = new Vector2(-16f, 12f);
                }
            }
        }

        void RosterRow(Transform parent, UnitType t, string k, bool on, bool can, IEnumerable<string> upgrades, Action pick)
        {
            var rt = Ui.Row(parent, "Roster " + t.Key, 10, 0, null, null, TextAnchor.MiddleLeft);
            Ui.Size(rt).minHeight = 64;
            rt.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 6, 6);
            rt.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Frame(rt, on ? Ui.Steel : Ui.Line, 14f);
            var icon = Ui.Sprite(rt, "Icon", S(Weapon(k)), on ? Ui.Steel : Ui.Muted);
            Ui.Size(icon, 40, 40);
            var col = Ui.Col(rt, "Info", 4);
            Ui.Size(col, -1, -1, 1);
            var top = Ui.Row(col, "Top", 10);
            top.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(T(top, Loc.Text(t.Name), 17, Ui.Bone, true), -1, -1, 1);
            kit.Value(top, Kit.Icon.Pop, t.Pop.ToString(), 15, can ? Ui.Candle : Ui.Blood, can ? Ui.Muted : Ui.Blood, Loc.T("battle.pop"));
            if (t.Silver > 0) kit.Value(top, Kit.Icon.Silver, t.Silver.ToString(), 15, can ? Ui.Candle : Ui.Blood, can ? Ui.Muted : Ui.Blood, Loc.T("battle.silver"));
            kit.Stats(col, Battle.Preview(k, upgrades), 14, Ui.SteelDim, Ui.Muted).GetComponent<HorizontalLayoutGroup>().spacing = 8;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = rt.GetComponentInChildren<Image>();
            btn.onClick.AddListener(() => pick());
        }

        void Def(Transform parent, string icon, Color iconColor, string value, string tip)
        {
            var row = Ui.Row(parent, "Def", 10, 0, null, null, TextAnchor.UpperLeft);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(Ui.Sprite(row, "Icon", S(icon), iconColor), 24, 24);
            Ui.Size(Wrap(T(row, value, 15, Ui.Bone)), PanelW - 32 - 34, -1, 1);
            HoverTip.Add(row, tip, 0.3f);
        }

        // ---------- player / enemy turn (battle-hud.md 3, 4) ----------

        void Fight(RectTransform root, BattleSession s, HudState st, BattleActions a)
        {
            var b = s.Battle;
            var stats = Ui.Row(root, "Stats", 28, 0, null, null, TextAnchor.UpperLeft);
            stats.anchorMin = stats.anchorMax = stats.pivot = new Vector2(0f, 1f);
            stats.anchoredPosition = new Vector2(Edge, -Edge);
            stats.sizeDelta = new Vector2(400f, 48f);
            stats.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            stats.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var spawns = b.Enemies.Count(e => e.Role == Role.Spawn);
            kit.Value(stats, Kit.Icon.Turn, b.Turn.ToString(), 22, Ui.Bone, Ui.Bone, Loc.T("battle.turn"));
            kit.Value(stats, Kit.Icon.Pop, b.Allies.Count().ToString(), 22, Ui.Steel, Ui.Steel, Loc.T("battle.allies"));
            kit.Value(stats, Kit.Icon.Skulls, b.Enemies.Count(e => e.Role != Role.Spawn).ToString(), 22, Ui.Blood, Ui.Blood, Loc.T("battle.enemies"));
            kit.Value(stats, Kit.Icon.Fang, spawns + (b.Corpses.Count > 0 ? " +" + b.Corpses.Count : ""), 22, Ui.Spawn, Ui.Spawn, Loc.T("unit.card.spawn"));
            // Direction review: Alt is the only way to read enemy movement, so its key stays on screen.
            var alt = Ui.Row(stats, "Alt", 4, 0, null, null, TextAnchor.MiddleLeft);
            alt.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var key = Ui.Node(alt, "Key");
            Ui.Size(key, 34, 22);
            Ui.Bg(key, Ui.Alpha(Ui.Deep, 0.85f), st.Alt ? Ui.Blood : Ui.Line);
            Ui.Stretch(T(key, "Alt", 14, st.Alt ? Ui.Bone : Ui.Muted, false, false, TextAnchor.MiddleCenter).rectTransform);
            Ui.Size(Ui.Sprite(alt, "Icon", S(Kit.Icon.Mov), st.Alt ? Ui.Blood : Ui.Muted), 22, 22);
            HoverTip.Add(alt, Loc.T("hud.altTip"), 0.2f);
            if (b.Vampire is Unit v && !v.Asleep) Boss(root, v);
            Log(root, b, st, a);
        }

        void FightHover(RectTransform root, BattleSession s, HudState st, BattleActions a)
        {
            var b = s.Battle;
            var sel = s.SelectedUnit;
            if (!st.EnemyPlaying)
            {
                var ally = sel ?? (st.Hover != null && st.Hover.Side == Side.Ally ? st.Hover : null);
                if (ally != null) Panel(root, b, ally, Vector2.zero, new Vector2(Edge, Edge + 48f), sel == ally);
                if (sel != null && b.Phase == Phase.Player && !sel.Acted) Actions(root, s, a);
            }
            var mendTarget = sel != null && s.Mode == SelectMode.Mend && b.CanMend(sel, st.Hover) ? st.Hover : null;
            if (st.HoverCorpse != null) CorpsePanel(root, st.HoverCorpse);
            else if (mendTarget != null) Panel(root, b, mendTarget, new Vector2(1f, 0f), new Vector2(-Edge - 216f, Edge + 48f), true, b.MendAmount(sel, mendTarget));
            else if (st.Hover != null && st.Hover.Side == Side.Enemy) Panel(root, b, st.Hover, new Vector2(1f, 0f), new Vector2(-Edge - 216f, Edge + 48f), true);

            var atk = new HashSet<Hex>(s.Overlay().Where(m => m.Mark == Mark.Atk).Select(m => m.Hex));
            foreach (var u in b.Units)
            {
                var dmg = sel != null && st.Hover == u && atk.Contains(u.Pos) ? b.Damage(sel, u) : 0;
                Head(headLayer, b, u, u == st.Hover || u == sel, st.Alt && u.Side == Side.Enemy, dmg);
            }
            if (st.Hover != null && heads.TryGetValue(st.Hover.Id, out var top)) top.Rt.SetAsLastSibling();
        }

        void Boss(RectTransform root, Unit v)
        {
            var bar = Ui.Place(root, "Boss", new Vector2(0.5f, 1f), new Vector2(0f, -Edge - 112f - 60f), new Vector2(560f, 40f));
            Ui.Stretch(Ui.Sprite(bar, "Back", S("SPR_DarkFantasyMenus_Frame_Bar_Medium_01_Background"), Ui.Deep, 12f).rectTransform);
            var f = Mathf.Clamp01((float)v.Hp / v.MaxHp);
            var fill = Ui.Node(bar, "Fill");
            Ui.Bg(fill, Ui.Blood, null, false);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(f, 1f);
            fill.offsetMin = new Vector2(24f, 10f);
            fill.offsetMax = new Vector2(-24f * f, -10f);
            Ui.Stretch(Ui.Sprite(bar, "Frame", S("SPR_DarkFantasyMenus_Frame_Bar_Medium_01"), Ui.Blood, 12f).rectTransform);
            var name = Watch(T(bar, Loc.Text(v.Name) + "   " + v.Hp + "/" + v.MaxHp, 17, Ui.Bone, true, false, TextAnchor.MiddleCenter), "boss");
            Ui.Stretch(name.rectTransform);
            name.gameObject.AddComponent<Outline>().effectColor = Ui.Deep;
            foreach (var x in new[] { 0f, 1f })
            {
                var rt = Ui.Sprite(bar, "Skull", S("SPR_DarkFantasyMenus_Greeble_DemonSkull_01"), Ui.Blood).rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(x, 0.5f);
                rt.anchoredPosition = new Vector2(x == 0f ? -30f : 30f, 0f);
                rt.sizeDelta = new Vector2(48f, 48f);
            }
        }

        static readonly Dictionary<string, (string icon, Color color)> LogIcon = new Dictionary<string, (string, Color)>
        {
            ["k"] = ("ICON_DarkFantasy_Status_Dead_01_Clean", Ui.Blood), ["s"] = ("ICON_DarkFantasy_Status_Vampiric_01_Clean", Ui.Spawn),
            ["h"] = ("ICON_DarkFantasy_Status_Targeted_01_Stroke", Ui.MoonBlood),
        };

        void Log(RectTransform root, Battle b, HudState st, BattleActions a)
        {
            const float W = 320f;
            var col = Ui.Col(root, "Log", 4);
            col.anchorMin = col.anchorMax = col.pivot = new Vector2(1f, 1f);
            col.anchoredPosition = new Vector2(-Edge, -Edge);
            col.sizeDelta = new Vector2(W, 0f);
            col.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var head = T(col, Loc.T("battle.log") + (st.LogOpen ? " ▴" : " ▾"), 14, Ui.Dim, false, false, TextAnchor.UpperRight);
            head.raycastTarget = true;
            head.gameObject.AddComponent<Button>().onClick.AddListener(() => a.ToggleLog?.Invoke());
            if (st.LogOpen)
            {
                var view = Ui.Scroll(col, "Lines", out var scroll);
                Ui.Size(scroll, W, 400f);
                Ui.Bg((RectTransform)scroll.transform, Ui.Alpha(Ui.Deep, 0.92f), Ui.Line);
                view.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(10, 10, 10, 10);
                view.GetComponent<VerticalLayoutGroup>().spacing = 4;
                foreach (var l in b.Log) LogLine(view, l, W - 20f, true);
                return;
            }
            foreach (var l in b.Log.Take(1)) LogLine(col, l, W, false);
        }

        void LogLine(Transform parent, LogEntry l, float width, bool wrap)
        {
            var row = Ui.Row(parent, "Log", 6, 0, null, null, wrap ? TextAnchor.UpperLeft : TextAnchor.MiddleRight);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var text = Loc.Text(l.Text);
            var iconW = 0f;
            if (LogIcon.TryGetValue(l.Style, out var ic))
            {
                Ui.Size(Ui.Sprite(row, "Icon", S(ic.icon), ic.color), 16, 16);
                iconW = 22f;
            }
            var t = T(row, text, 17, l.Style == "t" ? Ui.Dim : Ui.Muted, false, false, wrap ? TextAnchor.UpperLeft : TextAnchor.MiddleRight);
            t.name = "Log";
            if (wrap) Ui.Size(Wrap(t), width - iconW, -1, 1);
            else Ui.Ellipsize(t, width - iconW);
        }

        RectTransform StatCell(Transform row, string icon, string value, string label, float px, Color iconColor, Color valueColor, string watch = null)
        {
            var cell = Ui.Row(row, "Stat " + label, 6, 0, null, null, TextAnchor.LowerLeft);
            cell.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(Ui.Sprite(cell, "Icon", S(icon), iconColor), 24, 24);
            var v = T(cell, value, px, valueColor, true);
            Ui.Size(v, -1, px * 1.3f);
            if (watch != null) Watch(v, watch);
            HoverTip.Add(cell, label, 0.3f);
            return cell;
        }

        RectTransform PanelFrame(RectTransform root, string name, Vector2 anchor, Vector2 pos, Color edge)
        {
            var p = Ui.Place(root, name, anchor, pos, new Vector2(PanelW, PanelH));
            Frame(p, edge);
            if (!lastPanels.Contains(name)) Fx.Appear(p);
            panels.Add(name);
            return p;
        }

        RectTransform PanelBody(RectTransform p)
        {
            var col = Ui.Col(p, "Body", 6, 16);
            Ui.Stretch(col);
            col.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(20, 20, 16, 14);
            return col;
        }

        RectTransform Panel(RectTransform root, Battle b, Unit o, Vector2 anchor, Vector2 pos, bool lit, int heal = 0)
        {
            var spawn = o.Role == Role.Spawn;
            var side = o.Side == Side.Ally ? Ui.Steel : spawn ? Ui.Spawn : Ui.Blood;
            var dim = o.Side == Side.Ally ? Ui.SteelDim : spawn ? Ui.Alpha(Ui.Spawn, 0.6f) : Ui.BloodDim;
            var p = PanelFrame(root, "Panel " + o.Id, anchor, pos, lit ? side : dim);
            if (o.Side == Side.Enemy && !spawn) Spikes(p, side);
            if (spawn) Corner(p, "ICON_DarkFantasy_Status_Vampiric_01_Clean", Ui.Spawn);
            var col = PanelBody(p);
            var head = Ui.Row(col, "Head", 10, 0, null, null, TextAnchor.LowerLeft);
            head.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            if (o.Type != null && Weapon(o.Type) is string wi) Ui.Size(Ui.Sprite(head, "Weapon", S(wi), side), 26, 26);
            Ui.Ellipsize(T(head, spawn ? Loc.T("hud.spawnOf", o.Origin) : Loc.Text(o.Name), 22, Ui.Bone, true, true), PanelW - 80);
            var big = Ui.Row(col, "Big", 22);
            big.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var hp = StatCell(big, "ICON_DarkFantasy_Stat_Health_01_Clean", o.Hp + "/" + o.MaxHp, Loc.T("unit.card.hp"), 34, side, o.Side == Side.Ally && o.Hp * 3 <= o.MaxHp ? Ui.Blood : Ui.Bone, "hp" + o.Id);
            if (heal > 0) Ui.Size(T(hp, "+" + heal, 34, Ui.Steel, true), -1, 34 * 1.3f);
            StatCell(big, "ICON_DarkFantasy_Stat_Speed_01_Clean", o.Mov.ToString(), Loc.T("unit.card.mov"), 34, side, Ui.Bone, "mov" + o.Id);
            var small = Ui.Row(col, "Small", 18);
            small.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            StatCell(small, "ICON_DarkFantasy_Stat_Strength_01_Clean", o.Atk + (o.Bane > 0 ? "+" + o.Bane : ""), Loc.T("unit.card.atk"), 20, side, Ui.Muted);
            StatCell(small, "ICON_DarkFantasy_Status_Defense_01_Clean", o.Def.ToString(), Loc.T("unit.card.def"), 20, side, Ui.Muted);
            StatCell(small, "ICON_DarkFantasy_Stat_Accuracy_01_Clean", o.Rng.ToString(), Loc.T("unit.card.rng"), 20, side, Ui.Muted);
            var (warn, wc) = Warning(b, o);
            if (warn != null) T(col, warn, 15, wc, true).name = "Warn";
            if (o.Side == Side.Ally && BattleScreen.Skills(o) is string sk) T(col, sk, 14, Ui.Muted);
            HoverTip.Add(p, BattleScreen.Note(o), 0.5f);
            return p;
        }

        static (string, Color) Warning(Battle b, Unit o)
        {
            if (o.Side == Side.Ally && b.EatTarget == o.Id)
            {
                var v = b.Vampire;
                var safe = v != null && (Hex.Distance(v.Pos, o.Pos) > Rules.EatRange || b.Board.Lit(o.Pos));
                return (Loc.T("hud.warn.target") + (safe ? " · " + Loc.T("hud.warn.safeNow") : ""), Ui.Blood);
            }
            if (o.Side == Side.Ally && b.Board.Lit(o.Pos)) return (Loc.T("hud.warn.lit"), Ui.Candle);
            if (o.JustRisen) return (Loc.T("hud.warn.risen"), Ui.Spawn);
            if (o.Role == Role.Vampire && o.Asleep) return (Loc.T("hud.warn.coffin", Rules.MoonWake), Ui.Candle);
            return (null, default);
        }

        void CorpsePanel(RectTransform root, Corpse c)
        {
            var p = PanelFrame(root, "Corpse " + c.Id, new Vector2(1f, 0f), new Vector2(-Edge - 216f, Edge + 48f), Ui.Spawn);
            Corner(p, "ICON_DarkFantasy_Status_Vampiric_01_Clean", Ui.Spawn);
            var col = PanelBody(p);
            var head = Ui.Row(col, "Head", 10, 0, null, null, TextAnchor.LowerLeft);
            head.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            T(head, Loc.T("unit.card.corpseOf", c.Name), 22, Ui.Bone, true, true);
            T(head, Loc.T("unit.card.corpse"), 14, Ui.Spawn);
            var big = Ui.Row(col, "Big", 22);
            big.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            StatCell(big, "ICON_DarkFantasy_Stat_Health_01_Clean", c.MaxHp.ToString(), Loc.T("unit.card.hp"), 34, Ui.Spawn, Ui.Bone);
            StatCell(big, "ICON_DarkFantasy_Stat_Speed_01_Clean", c.Mov.ToString(), Loc.T("unit.card.mov"), 34, Ui.Spawn, Ui.Bone);
            var small = Ui.Row(col, "Small", 18);
            small.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            StatCell(small, "ICON_DarkFantasy_Stat_Strength_01_Clean", c.Atk.ToString(), Loc.T("unit.card.atk"), 20, Ui.Spawn, Ui.Muted);
            StatCell(small, "ICON_DarkFantasy_Status_Defense_01_Clean", "0", Loc.T("unit.card.def"), 20, Ui.Spawn, Ui.Muted);
            StatCell(small, "ICON_DarkFantasy_Stat_Accuracy_01_Clean", "1", Loc.T("unit.card.rng"), 20, Ui.Spawn, Ui.Muted);
            T(col, Loc.T("hud.warn.corpse"), 15, Ui.Spawn, true).name = "Warn";
        }

        void Spikes(RectTransform p, Color c)
        {
            foreach (var (x, n) in new[] { (0f, "SPR_DarkFantasyMenus_Greeble_Spikes_02_TopLeft"), (1f, "SPR_DarkFantasyMenus_Greeble_Spikes_02_TopRight") })
            {
                var g = Ui.Sprite(p, "Spikes", S(n), c).rectTransform;
                g.anchorMin = g.anchorMax = g.pivot = new Vector2(x, 1f);
                g.anchoredPosition = new Vector2(x == 0f ? -10f : 10f, 10f);
                g.sizeDelta = new Vector2(32f, 32f);
            }
        }

        void Corner(RectTransform p, string icon, Color c)
        {
            var g = Ui.Sprite(p, "Badge", S(icon), c).rectTransform;
            g.anchorMin = g.anchorMax = g.pivot = new Vector2(0f, 1f);
            g.anchoredPosition = new Vector2(-6f, 6f);
            g.sizeDelta = new Vector2(24f, 24f);
        }

        void Actions(RectTransform root, BattleSession s, BattleActions a)
        {
            var exec = s.Mode == SelectMode.Execute;
            var mend = s.Mode == SelectMode.Mend;
            var sel = s.SelectedUnit;
            var row = Ui.Row(root, "ActionBar", 8, 0, null, null, TextAnchor.LowerCenter);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, Edge);
            row.sizeDelta = new Vector2(400f, 84f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var key = 1;
            foreach (var (icon, label, act, on, danger) in new[]
            {
                ("ICON_DarkFantasy_Status_Dead_01_Clean", Loc.T(exec ? "unit.card.cancelExec" : "unit.card.exec"), a.Exec, exec, true),
                (Kit.Icon.Wait, Loc.T("unit.card.wait"), a.Wait, false, false),
                (Kit.Icon.Heal, Loc.T(mend ? "hud.cancelMend" : "hud.mend"), a.Mend, mend, false),
            })
            {
                if (act == a.Mend && !sel.Has(Ability.Mend)) continue;
                var col = Ui.Col(row, "Slot " + label, 4);
                col.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
                col.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
                Ui.Size(col, 90);
                var slot = Ui.Node(col, "Slot");
                Ui.Size(slot, 64, 64);
                Frame(slot, on ? (danger ? Ui.Blood : Ui.Steel) : danger ? Ui.BloodDim : Ui.SteelDim, 14f);
                var ic = Ui.Sprite(slot, "Icon", S(icon), danger ? Ui.Blood : Ui.Steel);
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ic.rectTransform.sizeDelta = new Vector2(40f, 40f);
                Hotkey(slot, (key++).ToString());
                var btn = slot.gameObject.AddComponent<Button>();
                btn.targetGraphic = slot.GetComponentInChildren<Image>();
                var go = act;
                btn.onClick.AddListener(() => go?.Invoke());
                HoverTip.Add(slot, label, 0.2f);
            }
        }

        // ---------- heads (ui-style.md 8-1, 8-3, 8-4) ----------

        void Head(RectTransform layer, Battle b, Unit u, bool named, bool showMov, int damage)
        {
            var per = u.MaxHp > 12 ? 2 : 1;
            var cells = (u.MaxHp + per - 1) / per;
            var full = (u.Hp + per - 1) / per;
            var lost = damage > 0 ? full - Mathf.Max(0, (u.Hp - damage + per - 1) / per) : 0;
            var color = u.Side == Side.Ally ? Ui.Steel : u.Role == Role.Spawn ? Ui.Spawn : Ui.Blood;
            var w = cells * 7f - 1f;
            var rt = Ui.Place(layer, "Head " + u.Id, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 8f));
            rt.pivot = new Vector2(0.5f, 0f);
            Ui.Bg(Ui.Place(rt, "Back", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w + 4f, 12f)), Ui.Alpha(Ui.Ink, 0.85f), null, false);
            var imgs = new Image[cells];
            for (var i = 0; i < cells; i++)
            {
                var c = Ui.Place(rt, "Cell", new Vector2(0f, 0.5f), new Vector2(i * 7f, 0f), new Vector2(6f, 8f));
                c.pivot = new Vector2(0f, 0.5f);
                imgs[i] = Ui.Bg(c, i < full ? color : Ui.Deep, null, false);
                if (i >= full - lost && i < full) blink.Add(imgs[i]);
            }
            if (u.Def > 0)
            {
                var shields = Ui.Row(rt, "Def", -3, 0, null, null, TextAnchor.MiddleCenter);
                shields.anchorMin = shields.anchorMax = new Vector2(0.5f, 0.5f);
                shields.pivot = new Vector2(0.5f, 0.3f);
                shields.sizeDelta = new Vector2(w + 40f, 16f);
                shields.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                for (var i = 0; i < u.Def; i++) Ui.Size(Ui.Sprite(shields, "Shield", S(Kit.Icon.BoardDef), Ui.Bone), 16, 16);
            }
            var x = 4f;
            if (per > 1)
            {
                var n = T(Ui.Place(rt, "Num", new Vector2(1f, 0.5f), new Vector2(x, 0f), new Vector2(30f, 16f)), u.Hp.ToString(), 14, Ui.Bone, true, false, TextAnchor.MiddleLeft);
                n.rectTransform.parent.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
                Ui.Stretch(n.rectTransform);
                n.gameObject.AddComponent<Outline>().effectColor = Ui.Ink;
                x += 26f;
            }
            if (damage > 0)
            {
                var at = Ui.Place(rt, "Damage", new Vector2(1f, 0.5f), new Vector2(x, 0f), new Vector2(60f, 28f));
                at.pivot = new Vector2(0f, 0.5f);
                if (damage >= u.Hp) Ui.Place(at, "Dead", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(24f, 24f)).gameObject.AddComponent<Image>().sprite = S("ICON_DarkFantasy_Status_Dead_01_Stroke");
                else
                {
                    var d = T(at, "−" + damage, 20, u.Side == Side.Ally ? Ui.Blood : Ui.Bone, true, false, TextAnchor.MiddleLeft);
                    Ui.Stretch(d.rectTransform);
                    d.gameObject.AddComponent<Outline>().effectColor = Ui.Ink;
                }
            }
            var y = u.Def > 0 ? 20f : 12f;
            if (named)
            {
                var n = T(Ui.Place(rt, "Name", new Vector2(0.5f, 1f), new Vector2(0f, y - 4f), new Vector2(240f, 18f)), Loc.Text(u.Name) + "  " + u.Hp + "/" + u.MaxHp, 14, Ui.Bone, true, false, TextAnchor.LowerCenter);
                n.rectTransform.parent.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
                Ui.Stretch(n.rectTransform);
                n.gameObject.AddComponent<Outline>().effectColor = Ui.Ink;
                y += 18f;
            }
            if (showMov)
            {
                var m = Ui.Row(rt, "Mov", 2, 0, null, null, TextAnchor.LowerCenter);
                m.anchorMin = m.anchorMax = new Vector2(0.5f, 1f);
                m.pivot = new Vector2(0.5f, 0f);
                m.anchoredPosition = new Vector2(0f, y - 4f);
                m.sizeDelta = new Vector2(u.Rng >= 2 ? 110f : 60f, 24f);
                m.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                Ui.Size(Ui.Sprite(m, "Icon", S("ICON_DarkFantasy_Stat_Speed_01_Stroke"), Ui.Blood), 20, 20);
                T(m, u.Mov.ToString(), 20, Ui.Blood, true).gameObject.AddComponent<Outline>().effectColor = Ui.Ink;
                if (u.Rng >= 2)
                {
                    Ui.Size(Ui.Sprite(m, "Rng", S(Kit.Icon.Rng), Ui.Blood), 20, 20);
                    T(m, u.Rng.ToString(), 20, Ui.Blood, true).gameObject.AddComponent<Outline>().effectColor = Ui.Ink;
                }
                y += 24f;
            }
            if (b.EatTarget == u.Id)
            {
                var t = Ui.Sprite(rt, "Targeted", S("ICON_DarkFantasy_Status_Targeted_01_Stroke"), Ui.Blood).rectTransform;
                t.anchorMin = t.anchorMax = new Vector2(0.5f, 1f);
                t.pivot = new Vector2(0.5f, 0f);
                t.anchoredPosition = new Vector2(0f, y);
                t.sizeDelta = new Vector2(32f, 32f);
            }
            heads[u.Id] = new HeadMark { Rt = rt, Cells = imgs, Per = per, MaxHp = u.MaxHp, Color = color };
        }

        // ---------- result (battle-hud.md 5) ----------

        public void Over(RectTransform root, OverInfo o, BattleActions a)
        {
            var backdrop = Ui.Node(root, "Over");
            Ui.Stretch(backdrop);
            Ui.Bg(backdrop, Ui.Alpha(Ui.Ink, 0.7f));
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(() => a.CloseOver?.Invoke());
            var box = Ui.Col(backdrop, "OverBox", 16, 36);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(640f, 0f);
            box.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            box.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            var edge = o.Won ? Ui.Steel : Ui.Blood;
            Ui.Frame(box, S("SPR_DarkFantasyMenus_Frame_Box_Medium_02"), S("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"), edge, FramePpu);
            // ui-style.md 14-4: a big emblem carries the result, like the reference's Mission Failed screen. Large_01 is the demon skull, so only defeat wears it.
            var emblem = Ui.Node(box, "Emblem");
            Ui.Size(emblem, 200, 170);
            if (kit.Prefab(emblem, o.Won ? MenuScreen.Crest : "Crest_DarkFantasyMenus_Large_01", new Vector2(0.5f, 0.5f), Vector2.zero, 150f, edge) == null)
                Ui.Stretch(Ui.Sprite(emblem, "Icon", S(o.Won ? Kit.Icon.Trophy : Kit.Icon.Dead), edge).rectTransform, 37, 22, 37, 22);
            var st = o.Stats;
            var stg = o.Stage;
            var title = T(box, Loc.T(o.Won ? "over.won" : "over.lost"), 64, o.Won ? Ui.Sun : Ui.Blood, true, true, TextAnchor.UpperCenter);
            HoverTip.Add(title.rectTransform, o.Won ? Loc.T("over.wonLine", o.Turn, stg.BossName) : Loc.T("over.lostLine"), 0.3f);
            title.raycastTarget = true;

            var tally = Ui.Row(box, "Tally", 36, 0, null, null, TextAnchor.MiddleCenter);
            tally.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            kit.Value(tally, Kit.Icon.Dead, st.Lost.ToString(), 24, Ui.Muted, Ui.Bone, Loc.T("over.fallen"));
            kit.Value(tally, Kit.Icon.Skulls, st.Executed.ToString(), 24, Ui.Blood, Ui.Bone, Loc.T("over.execs"));
            kit.Value(tally, Kit.Icon.Fang, st.Risen.ToString(), 24, Ui.Spawn, Ui.Bone, Loc.T("over.risen"));
            kit.Value(tally, Kit.Icon.Target, st.Drained.ToString(), 24, Ui.MoonBlood, Ui.Bone, Loc.T("over.drained"));

            var ok = o.Won && o.BonusOk;
            if (o.Won)
            {
                var rew = Ui.Row(box, "Reward", 36, 0, null, null, TextAnchor.MiddleCenter);
                rew.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                var bonusTip = Loc.T("over.bonus", Loc.Text(stg.Bonus.Text)) + " · " + (ok ? Loc.T("over.done") : Loc.T("over.failed"));
                if (o.Camp)
                {
                    var r = o.Result;
                    kit.Value(rew, Kit.Icon.Gold, r.ClearGold > 0 ? "+" + r.ClearGold : "0", 28, Ui.Candle, r.ClearGold > 0 ? Ui.Candle : Ui.Dim,
                        r.FirstClear ? Loc.T("over.firstClear") : Loc.T("over.replayClear"));
                    kit.Value(rew, Kit.Icon.Star, !ok ? "✕" : r.BonusGold > 0 ? "+" + r.BonusGold : "✓", 28, ok ? Ui.Candle : Ui.Dim, ok ? Ui.Candle : Ui.Dim, bonusTip);
                    var nxt = Array.IndexOf(Campaign.Stages, stg) + 1;
                    var next = nxt < Campaign.Stages.Length ? Campaign.Stages[nxt] : null;
                    if (r.FirstClear && next != null)
                    {
                        var news = Ui.Row(box, "Unlocked", 10, 0, null, null, TextAnchor.MiddleCenter);
                        news.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                        var lockOpen = Ui.Sprite(news, "Next", S(Kit.Icon.Flag), Ui.Sun);
                        Ui.Size(lockOpen, 28, 28);
                        T(news, Loc.Text(next.Title), 20, Ui.Bone, true, false, TextAnchor.MiddleLeft);
                        foreach (var u in next.Units.Where(u => GameData.Type(u).UnlockAt == nxt + 1))
                        {
                            var chip = Ui.Sprite(news, "New", S(Weapon(u)), Ui.Candle);
                            Ui.Size(chip, 32, 32);
                            HoverTip.Add(chip.rectTransform, Loc.T("camp.new", GameData.Type(u).Name), 0.2f);
                            chip.raycastTarget = true;
                        }
                        HoverTip.Add(news, Loc.T("over.nextOpen", next.Label, next.Title), 0.4f);
                    }
                    if (r.FirstClear && next == null) HoverTip.Add(title.rectTransform, Loc.T("over.campaignDone"), 0.3f);
                }
                else kit.Value(rew, Kit.Icon.Star, ok ? "✓" : "✕", 28, ok ? Ui.Candle : Ui.Dim, ok ? Ui.Candle : Ui.Dim, bonusTip);
            }

            var acts = Ui.Row(box, "Acts", 10, 0, null, null, TextAnchor.MiddleCenter);
            acts.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
            acts.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var w = o.Camp ? 178f : 270f;
            if (o.Camp) Btn(acts, Loc.T("over.toCamp"), a.ToCamp, BtnKind.Primary, new Vector2(w, 52f));
            Btn(acts, Loc.T("over.retry"), a.Retry, o.Camp ? BtnKind.Normal : BtnKind.Primary, new Vector2(w, 52f));
            Btn(acts, Loc.T("over.redeploy"), a.Redeploy, BtnKind.Normal, new Vector2(w, 52f));
        }
    }
}
