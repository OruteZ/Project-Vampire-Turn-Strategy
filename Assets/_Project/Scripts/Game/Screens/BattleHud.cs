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
        readonly List<Graphic> pulse = new List<Graphic>(), blink = new List<Graphic>();
        readonly Dictionary<int, HeadMark> heads = new Dictionary<int, HeadMark>();
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

        static Dictionary<UnitKind, string> Weapon => Kit.Weapon;


        public void Build(RectTransform root, BattleSession s, HudState st, BattleActions a)
        {
            pulse.Clear();
            blink.Clear();
            heads.Clear();
            (lastValues, values) = (values, lastValues);
            values.Clear();
            (lastPanels, panels) = (panels, lastPanels);
            panels.Clear();
            var b = s.Battle;
            var hot = b.Phase == Phase.Player && b.MoonNext() && !st.FullMoon;
            if (st.FullMoon || st.MoonFading)
            {
                var vg = Vignette(root, 0.15f);
                if (st.MoonFading) Fx.FadeOut(vg, 0.4f);
            }
            else if (hot) Vignette(root, 0.08f);
            var headLayer = Ui.Node(root, "Heads");
            Ui.Stretch(headLayer);
            Moon(root, b, hot, st.FullMoon, st.MoonFading);
            if (b.Phase == Phase.Deploy) Deploy(root, s, st, a);
            else Fight(root, s, st, a, headLayer);
            EndTurn(root, b, st, a);
            Corner(root, st, a);
            Ui.Stretch(T(Ui.Place(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, Edge + 96f), new Vector2(1000f, 20f)),
                BattleScreen.Hint(s, st.EnemyPlaying), 14, Ui.Dim, false, false, TextAnchor.MiddleCenter).rectTransform);
            if (st.RulesOpen) RulesPopup(root, a);
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

        static Sprite edgeFade;

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
            Watch(T(label, Loc.T("battle.moon") + " " + moon + "/" + max, 20, Ui.Bone, true, false, TextAnchor.UpperCenter), "moon");
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
                var badge = Ui.Place(rt, "Badge", new Vector2(0f, 1f), new Vector2(-10f, 10f), new Vector2(28f, 28f));
                Ui.Stretch(Ui.Sprite(badge, "Disk", S("SPR_DarkFantasyMenus_Frame_Ring_01_Background"), Ui.Ink).rectTransform);
                Ui.Stretch(Ui.Sprite(badge, "Ring", S("SPR_DarkFantasyMenus_Frame_Ring_01"), Ui.Steel).rectTransform, -3, -3, -3, -3);
                Ui.Stretch(T(badge, left.ToString(), 14, Ui.Bone, true, false, TextAnchor.MiddleCenter).rectTransform);
            }
            var phase = st.EnemyPlaying || b.Phase == Phase.Enemy ? Loc.T("hud.enemyPhase") : b.Phase == Phase.Player ? Loc.T("battle.allyPhase") : "";
            Ui.Stretch(T(Ui.Place(root, "Phase", new Vector2(1f, 0f), new Vector2(-Edge, Edge + 60f), new Vector2(200f, 20f)), phase, 14, Ui.Muted, false, false, TextAnchor.LowerCenter).rectTransform);
        }

        void Corner(RectTransform root, HudState st, BattleActions a)
        {
            var row = Ui.Row(root, "Corner", 4, 0, null, null, TextAnchor.LowerLeft);
            row.anchorMin = row.anchorMax = row.pivot = Vector2.zero;
            row.anchoredPosition = new Vector2(Edge - 12f, Edge - 6f);
            row.sizeDelta = new Vector2(420f, 36f);
            row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
            Btn(row, "?  " + Loc.T("battle.rules"), a.ToggleRules, BtnKind.Ghost, new Vector2(140f, 36f));
            Btn(row, Loc.T(st.LeaveArmed ? "battle.leaveConfirm" : "battle.leave"), a.Leave, BtnKind.Ghost, new Vector2(st.LeaveArmed ? 200f : 100f, 36f), !st.EnemyPlaying);
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
            var title = Ui.Col(root, "Title", 2);
            title.anchorMin = title.anchorMax = title.pivot = new Vector2(0f, 1f);
            title.anchoredPosition = new Vector2(Edge, -Edge);
            title.sizeDelta = new Vector2(PanelW, 110f);
            T(title, BattleScreen.Subtitle(stg, st.Demo), 14, Ui.Muted);
            T(title, Loc.Text(stg.Label) + " · " + Loc.Text(stg.Title), 32, stg == Campaign.Stages.LastOrDefault() ? Ui.Blood : Ui.Bone, true, true);
            var res = Ui.Row(title, "Resources", 24, 0, null, null, TextAnchor.LowerLeft);
            res.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            res.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;
            Ui.Size(res, -1, -1, 0);
            Stat(res, b.Used(t => t.Pop) + "/" + stg.Pop, Loc.T("battle.pop"), Ui.Candle);
            if (stg.Silver > 0) Stat(res, b.Used(t => t.Silver) + "/" + stg.Silver, Loc.T("battle.silver"), Ui.Candle);
            Stat(res, b.Allies.Count().ToString(), Loc.T("battle.deployed"), Ui.Bone);

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
            Wrap(T(roster, Loc.T("hud.deployHelp"), 14, Ui.Dim));
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
            T(brief, Loc.T("battle.brief"), 22, Ui.Bone, true, true);
            Wrap(T(brief, Loc.Text(stg.Brief), 17, Ui.Muted));
            Def(brief, Loc.T("battle.objective"), Loc.T("battle.defeatBoss", stg.BossName));
            Def(brief, Loc.T("battle.bonus"), Loc.Text(stg.Bonus.Text));
            Def(brief, Loc.T("battle.enemy"), Loc.T("battle.enemyList", stg.Enemies.Length - 1, stg.BossName));

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

        void RosterRow(Transform parent, UnitType t, UnitKind k, bool on, bool can, IEnumerable<string> upgrades, Action pick)
        {
            var rt = Ui.Row(parent, "Roster " + t.Key, 10, 0, null, null, TextAnchor.MiddleLeft);
            Ui.Size(rt).minHeight = 64;
            rt.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 6, 6);
            rt.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Frame(rt, on ? Ui.Steel : Ui.Line, 14f);
            var icon = Ui.Sprite(rt, "Icon", S(Weapon[k]), on ? Ui.Steel : Ui.Muted);
            Ui.Size(icon, 40, 40);
            var col = Ui.Col(rt, "Info", 2);
            Ui.Size(col, -1, -1, 1);
            var top = Ui.Row(col, "Top", 8);
            Ui.Size(T(top, Loc.Text(t.Name), 17, Ui.Bone, true), -1, -1, 1);
            T(top, CampaignScreen.Resources(t.Pop, t.Silver), 14, can ? Ui.Muted : Ui.Blood);
            Wrap(T(col, CampaignScreen.StatLine(Battle.Preview(k, upgrades)), 14, Ui.Muted));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = rt.GetComponentInChildren<Image>();
            btn.onClick.AddListener(() => pick());
        }

        static void Def(Transform parent, string term, string value)
        {
            var row = Ui.Row(parent, "Def", 10, 0, null, null, TextAnchor.UpperLeft);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(T(row, term, 14, Ui.Muted), 80);
            Ui.Size(Wrap(T(row, value, 14, Ui.Bone)), 210, -1, 1);
        }

        static void Stat(Transform row, string value, string label, Color color)
        {
            var col = Ui.Col(row, "Stat " + label, 0);
            col.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(col, -1, -1, 0);
            T(col, value, 20, color, true);
            T(col, label, 14, Ui.Muted);
        }

        // ---------- player / enemy turn (battle-hud.md 3, 4) ----------

        void Fight(RectTransform root, BattleSession s, HudState st, BattleActions a, RectTransform headLayer)
        {
            var b = s.Battle;
            var stats = Ui.Row(root, "Stats", 28, 0, null, null, TextAnchor.UpperLeft);
            stats.anchorMin = stats.anchorMax = stats.pivot = new Vector2(0f, 1f);
            stats.anchoredPosition = new Vector2(Edge, -Edge);
            stats.sizeDelta = new Vector2(400f, 48f);
            stats.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            stats.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var spawns = b.Enemies.Count(e => e.Role == Role.Spawn);
            Stat(stats, b.Turn.ToString(), Loc.T("battle.turn"), Ui.Bone);
            Stat(stats, b.Allies.Count().ToString(), Loc.T("battle.allies"), Ui.Steel);
            Stat(stats, b.Enemies.Count(e => e.Role != Role.Spawn).ToString(), Loc.T("battle.enemies"), Ui.Blood);
            Stat(stats, spawns + (b.Corpses.Count > 0 ? " +" + b.Corpses.Count : ""), Loc.T("unit.card.spawn"), Ui.Spawn);
            if (b.Vampire is Unit v && !v.Asleep) Boss(root, v);
            Log(root, b, st, a);

            var sel = s.SelectedUnit;
            if (!st.EnemyPlaying)
            {
                var ally = sel ?? (st.Hover != null && st.Hover.Side == Side.Ally ? st.Hover : null);
                if (ally != null) Panel(root, b, ally, Vector2.zero, new Vector2(Edge, Edge + 48f), sel == ally);
                if (sel != null && b.Phase == Phase.Player && !sel.Acted) Actions(root, s, a);
            }
            if (st.HoverCorpse != null) CorpsePanel(root, st.HoverCorpse);
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
            foreach (var l in b.Log.Take(3)) LogLine(col, l, W, false);
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

        void StatCell(Transform row, string icon, string value, string label, float px, Color iconColor, Color valueColor, string watch = null)
        {
            var cell = Ui.Row(row, "Stat " + label, 6, 0, null, null, TextAnchor.LowerLeft);
            cell.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Size(Ui.Sprite(cell, "Icon", S(icon), iconColor), 24, 24);
            var v = T(cell, value, px, valueColor, true);
            if (watch != null) Watch(v, watch);
            T(cell, label, Mathf.Max(14f, px / 2f), Ui.Muted);
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

        RectTransform Panel(RectTransform root, Battle b, Unit o, Vector2 anchor, Vector2 pos, bool lit)
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
            T(head, spawn ? Loc.T("hud.spawnOf", o.Origin) : Loc.Text(o.Name), 22, Ui.Bone, true, true);
            T(head, Loc.T(o.Side == Side.Ally ? "unit.card.ally" : o.Role == Role.Vampire ? "unit.card.boss" : spawn ? "unit.card.spawn" : "unit.card.minion"), 14, side);
            var big = Ui.Row(col, "Big", 22);
            big.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            StatCell(big, "ICON_DarkFantasy_Stat_Health_01_Clean", o.Hp + "/" + o.MaxHp, Loc.T("unit.card.hp"), 34, side, o.Side == Side.Ally && o.Hp * 3 <= o.MaxHp ? Ui.Blood : Ui.Bone, "hp" + o.Id);
            StatCell(big, "ICON_DarkFantasy_Stat_Speed_01_Clean", o.Mov.ToString(), Loc.T("unit.card.mov"), 34, side, Ui.Bone, "mov" + o.Id);
            var small = Ui.Row(col, "Small", 18);
            small.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            StatCell(small, "ICON_DarkFantasy_Stat_Strength_01_Clean", o.Atk + (o.Bane > 0 ? "+" + o.Bane : ""), Loc.T("unit.card.atk"), 20, side, Ui.Muted);
            StatCell(small, "ICON_DarkFantasy_Status_Defense_01_Clean", o.Def.ToString(), Loc.T("unit.card.def"), 20, side, Ui.Muted);
            StatCell(small, "ICON_DarkFantasy_Stat_Accuracy_01_Clean", o.Rng.ToString(), Loc.T("unit.card.rng"), 20, side, Ui.Muted);
            var (warn, wc) = Warning(b, o);
            if (warn != null) T(col, warn, 14, wc, true);
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
            T(col, Loc.T("hud.warn.corpse"), 14, Ui.Spawn, true);
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
            var row = Ui.Row(root, "ActionBar", 8, 0, null, null, TextAnchor.LowerCenter);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, Edge);
            row.sizeDelta = new Vector2(400f, 84f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var key = 1;
            foreach (var (icon, label, act, on, danger) in new[]
            {
                ("ICON_DarkFantasy_Status_Dead_01_Clean", Loc.T(exec ? "unit.card.cancelExec" : "unit.card.exec"), a.Exec, exec, true),
                ("ICON_DarkFantasy_Status_Defense_01_Clean", Loc.T("unit.card.wait"), a.Wait, false, false),
            })
            {
                var col = Ui.Col(row, "Slot " + label, 4);
                col.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
                col.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
                Ui.Size(col, 90);
                var slot = Ui.Node(col, "Slot");
                Ui.Size(slot, 64, 64);
                Frame(slot, on ? Ui.Blood : danger ? Ui.BloodDim : Ui.SteelDim, 14f);
                var ic = Ui.Sprite(slot, "Icon", S(icon), danger ? Ui.Blood : Ui.Steel);
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ic.rectTransform.sizeDelta = new Vector2(40f, 40f);
                Hotkey(slot, (key++).ToString());
                var btn = slot.gameObject.AddComponent<Button>();
                btn.targetGraphic = slot.GetComponentInChildren<Image>();
                var go = act;
                btn.onClick.AddListener(() => go?.Invoke());
                T(col, label, 14, Ui.Muted, false, false, TextAnchor.UpperCenter);
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
            var y = 12f;
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
                m.sizeDelta = new Vector2(60f, 24f);
                m.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                Ui.Size(Ui.Sprite(m, "Icon", S("ICON_DarkFantasy_Stat_Speed_01_Stroke"), Ui.Blood), 20, 20);
                T(m, u.Mov.ToString(), 20, Ui.Blood, true).gameObject.AddComponent<Outline>().effectColor = Ui.Ink;
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
            var box = Ui.Col(backdrop, "OverBox", 10, 32);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(520f, 0f);
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            box.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            Ui.Frame(box, S("SPR_DarkFantasyMenus_Frame_Box_Medium_02"), S("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"), o.Won ? Ui.Steel : Ui.Blood, FramePpu);
            var st = o.Stats;
            var stg = o.Stage;
            T(box, Loc.T(o.Won ? "over.won" : "over.lost"), 56, o.Won ? Ui.Sun : Ui.Blood, true, true);
            Wrap(T(box, o.Won ? Loc.T("over.wonLine", o.Turn, stg.BossName) : Loc.T("over.lostLine"), 17, Ui.Muted));
            var tally = Ui.Col(box, "Tally", 6);
            Pair(tally, Loc.T("over.fallen"), st.Lost.ToString());
            Pair(tally, Loc.T("over.execs"), st.Executed.ToString());
            Pair(tally, Loc.T("over.risen"), st.Risen.ToString());
            Pair(tally, Loc.T("over.drained"), st.Drained.ToString());
            var ok = o.Won && o.BonusOk;
            if (o.Won && o.Camp)
            {
                var r = o.Result;
                var g1 = r.ClearGold;
                var g2 = r.BonusGold;
                var rew = Ui.Col(box, "Reward", 6);
                Pair(rew, r.FirstClear ? Loc.T("over.firstClear") : Loc.T("over.replayClear"), g1 > 0 ? "+" + g1 : Loc.T("over.claimed"));
                Pair(rew, Loc.T("over.bonus", stg.Bonus.Text), !ok ? Loc.T("over.failed") : g2 > 0 ? "+" + g2 : Loc.T("over.claimed"), !ok);
                Pair(rew, Loc.T("over.earned"), Loc.T("camp.cost", g1 + g2), false, true);
                var nxt = Array.IndexOf(Campaign.Stages, stg) + 1;
                var next = nxt < Campaign.Stages.Length ? Campaign.Stages[nxt] : null;
                if (r.FirstClear && next != null)
                {
                    var news = next.Units.Where(u => GameData.Type(u).UnlockAt == nxt + 1).Select(u => Loc.Text(GameData.Type(u).Name)).ToList();
                    Wrap(T(box, Loc.T("over.nextOpen", next.Label, next.Title) + (news.Count > 0 ? " " + Loc.T("over.unitsUnlocked", string.Join(", ", news)) : ""), 14, Ui.Muted));
                }
                if (r.FirstClear && next == null) Wrap(T(box, Loc.T("over.campaignDone"), 14, Ui.Muted));
                var armory = GameData.Types.Where(t => t.UnlockAt == nxt).Select(t => Loc.Text(t.Name)).ToList();
                if (r.FirstClear && armory.Count > 0) Wrap(T(box, Loc.T("over.armoryOpen", string.Join(", ", armory)), 14, Ui.Muted));
            }
            else if (o.Won)
            {
                var rew = Ui.Col(box, "Reward", 6);
                Pair(rew, Loc.T("over.bonus", stg.Bonus.Text), ok ? Loc.T("over.done") : Loc.T("over.failed"), !ok);
            }
            var acts = Ui.Row(box, "Acts", 8, 0, null, null, TextAnchor.MiddleRight);
            acts.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
            if (o.Camp) Btn(acts, Loc.T("over.toCamp"), a.ToCamp, BtnKind.Primary, new Vector2(150f, 48f));
            Btn(acts, Loc.T("over.retry"), a.Retry, o.Camp ? BtnKind.Normal : BtnKind.Primary, new Vector2(200f, 48f));
            Btn(acts, Loc.T("over.redeploy"), a.Redeploy, BtnKind.Normal, new Vector2(150f, 48f));
        }

        static void Pair(Transform parent, string left, string right, bool dim = false, bool strong = false)
        {
            var row = Ui.Row(parent, "Pair", 12);
            var color = dim ? Ui.Dim : Ui.Muted;
            Ui.Size(Wrap(T(row, left, 17, color)), -1, -1, 1);
            T(row, right, 20, strong ? Ui.Candle : dim ? Ui.Dim : Ui.Bone, true, false, TextAnchor.UpperRight);
        }
    }
}
