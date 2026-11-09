using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VampireSpawn.Core;
using static VampireSpawn.Game.Screens.ToolkitKit;

namespace VampireSpawn.Game.Screens
{
    public sealed class HudState
    {
        public Unit Hover;
        public Corpse HoverCorpse;
        public bool Alt, EnemyPlaying, LeaveArmed, RulesOpen, LogOpen, Demo, FullMoon, MoonFading;
        public IEnumerable<string> Upgrades = Enumerable.Empty<string>();
    }

    // docs/ui/battle-hud.md in the docs/art/ui-style.md tokens on UI Toolkit. Places and sizes are Assets/_Project/UI/Battle.uss;
    // this builds the elements and sets what the battle state decides.
    public sealed class BattleHud
    {
        sealed class HeadMark
        {
            public VisualElement Root;
            public VisualElement[] Cells;
            public int MaxHp;
            public Color Color;
        }

        readonly ArtCatalog art;
        readonly Label tip;
        readonly Dictionary<int, HeadMark> heads = new Dictionary<int, HeadMark>();
        readonly HashSet<VisualElement> blink = new HashSet<VisualElement>();
        VisualElement headLayer, hoverLayer, moonDisk;
        Color[] moonColors;
        bool moonPulse;
        // Values and panels of the previous build, so only real changes flash and only new panels slide in (ui-style.md 12).
        Dictionary<string, string> lastValues = new Dictionary<string, string>(), values = new Dictionary<string, string>();
        HashSet<string> lastPanels = new HashSet<string>(), panels = new HashSet<string>();

        public BattleHud(ArtCatalog art, Label tip)
        {
            this.art = art;
            this.tip = tip;
        }

        public bool HasHeads => heads.Count > 0;

        Sprite S(string name) => art.Ui(name);

        VisualElement Icon(VisualElement parent, string sprite, Color tint, string classes = "icon") => Pic(parent, art, sprite, tint, classes);

        void Tip(VisualElement owner, string text, long delayMs) => ToolkitKit.Tip(owner, text, tip, delayMs);

        VisualElement Value(VisualElement row, string icon, string value, Color iconColor, Color valueColor, string tipText = null) =>
            ToolkitKit.Value(row, art, icon, value, iconColor, valueColor, tipText, tip);

        VisualElement Btn(VisualElement parent, string label, Action click, BtnKind kind, bool enabled = true, string tipText = null, string classes = null)
        {
            var b = ToolkitKit.Btn(parent, art, label, kind, null, click, enabled);
            if (classes != null) foreach (var c in classes.Split(' ')) b.AddToClassList(c);
            if (tipText != null) Tip(b, tipText, 0);
            return b;
        }

        static void Hotkey(VisualElement b, string key)
        {
            L(b, key, "key");
            KeepClearOfKey(b, key);
        }

        public void Build(VisualElement root, BattleSession s, HudState st, BattleActions a)
        {
            root.Clear();
            (lastValues, values) = (values, lastValues);
            values.Clear();
            var b = s.Battle;
            Shade(E(root, "shade"));
            var hot = b.Phase == Phase.Player && b.MoonNext() && !st.FullMoon;
            if (st.FullMoon || st.MoonFading)
            {
                var vg = Vignette(root, 0.15f);
                if (st.MoonFading) Fx.FadeOut(vg, 0.4f);
            }
            else if (hot) Vignette(root, 0.08f);
            headLayer = E(root, "layer");
            Moon(root, b, hot, st.FullMoon, st.MoonFading);
            if (b.Phase == Phase.Deploy) Deploy(root, s, st, a);
            else Fight(root, s, st, a);
            hoverLayer = E(root, "layer");
            Hover(s, st, a);
            EndTurn(root, b, st, a);
            Corner(root, st, a);
            if (BattleScreen.ShowsHint(s, st.EnemyPlaying)) L(root, BattleScreen.Hint(s, st.EnemyPlaying), "hint");
            if (st.RulesOpen) RulesPopup(root, a);
            UseFonts(root);
        }

        // Hovering only redraws the unit panels and heads; rebuilding the whole HUD per hovered cell stalled the frame.
        public void RebuildHover(BattleSession s, HudState st, BattleActions a)
        {
            if (hoverLayer == null || headLayer == null) return;
            lastValues.Clear();
            foreach (var kv in values) lastValues[kv.Key] = kv.Value;
            headLayer.Clear();
            hoverLayer.Clear();
            Hover(s, st, a);
            UseFonts(hoverLayer);
            UseFonts(headLayer);
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
            if (moonPulse && moonDisk != null) moonDisk.MarkDirtyRepaint();
            var on = Time.unscaledTime % 0.5f < 0.25f;
            foreach (var c in blink) c.style.opacity = on ? 1f : 0.15f;
            if (cam == null || art == null || headLayer?.panel == null) return;
            foreach (var kv in heads)
            {
                var v = view.ViewOf(kv.Key);
                var h = kv.Value;
                var world = v != null && !v.Lying ? v.transform.position + Vector3.up * (art.unitHeight * art.hexSize + 0.15f) : (Vector3?)null;
                var shown = world.HasValue && cam.WorldToScreenPoint(world.Value).z > 0f;
                h.Root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                if (!shown) continue;
                var p = RuntimePanelUtils.CameraTransformWorldToPanel(headLayer.panel, world.Value, cam);
                h.Root.style.left = p.x;
                h.Root.style.top = p.y;
                // The view's hp follows the hit animations; the model already holds the result.
                var full = Mathf.CeilToInt(v.HpFraction * h.MaxHp - 0.001f);
                for (var i = 0; i < h.Cells.Length; i++)
                    if (!blink.Contains(h.Cells[i])) h.Cells[i].style.backgroundColor = i < full ? h.Color : Ui.Deep;
            }
        }

        // ---------- shared pieces ----------

        static Texture2D edgeFade;

        // ui-style.md 7-2: an 80 px fade on all four edges, as one full-screen texture.
        VisualElement Vignette(VisualElement root, float alpha)
        {
            var e = E(root, "vignette");
            e.style.backgroundImage = new StyleBackground(edgeFade ??= FadeTexture(128, (u, v) =>
                Mathf.Max(Mathf.Clamp01(1f - Mathf.Min(u, 1f - u) / (80f / 1920f)), Mathf.Clamp01(1f - Mathf.Min(v, 1f - v) / (80f / 1080f)))));
            e.style.unityBackgroundImageTintColor = Ui.Alpha(Ui.MoonBlood, alpha);
            return e;
        }

        Label Watch(Label t, string key)
        {
            values[key] = t.text;
            if (lastValues.TryGetValue(key, out var was) && was != t.text) Fx.Flash(t);
            return t;
        }

        // full: the full-moon enemy turn is playing (7-2); fading: it just ended, so the blood drains away over 0.4 s.
        void Moon(VisualElement root, Battle b, bool hot, bool full, bool fading)
        {
            var box = E(root, "moon");
            Tip(box, Loc.T("battle.moon") + " · " + Loc.T("battle.rule.moon", Rules.MoonMax), 400);
            Picture(E(box, "fill"), S(RingBack), Ui.Deep);
            var max = Rules.MoonMax;
            var moon = full ? max : b.Moon;
            moonColors = Enumerable.Range(0, max).Select(i => i < moon ? (hot || full ? Ui.MoonBlood : Ui.Sun) : Ui.Panel2).ToArray();
            moonPulse = hot;
            moonDisk = E(box, "moon-disk");
            var lit = moon;
            moonDisk.generateVisualContent += ctx => Phases(ctx, max, i => moonPulse && i < lit ? Fx.Bright(Ui.MoonBlood, Fx.PulseK()) : moonColors[i]);
            if (fading)
            {
                var drain = E(box, "moon-disk");
                drain.generateVisualContent += ctx => Phases(ctx, max, _ => Ui.MoonBlood);
                Fx.FadeOut(drain, 0.4f);
            }
            var v = b.Vampire;
            if (v != null && v.Asleep)
            {
                var pin = E(box, "moon-pin");
                pin.style.rotate = new Rotate(360f * Rules.MoonWake / max);
                E(pin, "coffin");
            }
            Picture(E(box, "moon-ring"), S(Ring), hot || full ? Ui.MoonBlood : Ui.Steel);
            if (full)
                for (var k = 0; k < 4; k++)
                {
                    var pin = E(box, "moon-pin");
                    pin.style.rotate = new Rotate(-(45f + 90f * k));
                    Icon(pin, "SPR_DarkFantasyMenus_Greeble_DemonSkull_01", Ui.MoonBlood, "icon moon-skull");
                }
            var label = E(root, "moon-label col");
            Watch(L(label, moon + "/" + max, "moon-count"), "moon");
            var state = hot ? Loc.T("moon.nextTurn") : v != null && v.Asleep ? Loc.T("battle.coffinAt", Rules.MoonWake) : "";
            if (state.Length > 0) L(label, state, hot ? "moon-state hot" : "moon-state", hot ? Ui.MoonBlood : Ui.Muted);
        }

        // One wedge per phase from the top, clockwise, with a 1.5% gap (the old radial fill).
        static void Phases(MeshGenerationContext ctx, int max, Func<int, Color> color)
        {
            var r = ctx.visualElement.contentRect;
            var c = r.center;
            var radius = Mathf.Min(r.width, r.height) * 0.5f;
            var p = ctx.painter2D;
            for (var i = 0; i < max; i++)
            {
                var start = -90f + 360f * i / max;
                p.fillColor = color(i);
                p.BeginPath();
                p.MoveTo(c);
                p.Arc(c, radius, start, start + 360f * (1f / max - 0.015f));
                p.ClosePath();
                p.Fill();
            }
        }

        void EndTurn(VisualElement root, Battle b, HudState st, BattleActions a)
        {
            var label = BattleScreen.PrimaryLabel(b, st.EnemyPlaying, out var enabled);
            var btn = Btn(root, label, a.Primary, BtnKind.Primary, enabled, b.Phase == Phase.Deploy && !enabled ? Loc.T("hud.needAlly") : null, "end-turn");
            Hotkey(btn, "Space");
            var left = b.Phase == Phase.Player && !st.EnemyPlaying ? b.Allies.Count(u => !u.Acted) : 0;
            // ui-style.md 14-3-3: inside the button, left of the label.
            if (left > 0) Tip(RingBadge(btn, art, left.ToString(), 28, Ui.Steel, Ui.Ink, Ui.Bone), Loc.T("battle.allyPhase"), 300);
        }

        void Corner(VisualElement root, HudState st, BattleActions a)
        {
            var row = E(root, "corner");
            Tip(IconBtn(row, art, Kit.Icon.Rules, a.ToggleRules), Loc.T("battle.rules"), 300);
            if (st.LeaveArmed) Btn(row, Loc.T("battle.leaveConfirm"), a.Leave, BtnKind.Danger, !st.EnemyPlaying);
            else Tip(IconBtn(row, art, Kit.Icon.Leave, a.Leave, !st.EnemyPlaying), Loc.T("battle.leave"), 300);
        }

        void RulesPopup(VisualElement root, BattleActions a)
        {
            var veil = E(root, "veil", true);
            Click(veil, null);
            veil.RegisterCallback<ClickEvent>(e => { if (e.target == veil) a.ToggleRules?.Invoke(); });
            var p = E(veil, "window rules", true);
            Frame(p, art, Ui.Steel);
            L(p, Loc.T("battle.rules"), "rules-title display");
            foreach (var line in BattleScreen.RuleLines) L(p, line, "rules-line");
            Btn(E(p, "rules-close"), Loc.T("hud.close"), a.ToggleRules, BtnKind.Ghost);
        }

        // ---------- deploy (battle-hud.md 2) ----------

        void Deploy(VisualElement root, BattleSession s, HudState st, BattleActions a)
        {
            var b = s.Battle;
            var stg = b.Stage;
            var title = E(root, "deploy-title col");
            var name = E(title, "stage-name row", true);
            var last = stg == Campaign.Stages.LastOrDefault();
            var num = Array.IndexOf(Campaign.Stages, stg) + 1;
            if (num > 0 && !st.Demo) RingBadge(name, art, num.ToString(), 40, last ? Ui.Blood : Ui.Steel, Ui.Deep, Ui.Bone);
            L(name, Loc.Text(stg.Title), "display", last ? Ui.Blood : Ui.Bone);
            Tip(name, BattleScreen.Subtitle(stg, st.Demo), 400);
            var res = E(title, "res row");
            Value(res, Kit.Icon.Pop, b.Used(t => t.Pop) + "/" + stg.Pop, Ui.Candle, Ui.Candle, Loc.T("battle.pop"));
            if (stg.Silver > 0) Value(res, Kit.Icon.Silver, b.Used(t => t.Silver) + "/" + stg.Silver, Ui.Candle, Ui.Candle, Loc.T("battle.silver"));
            Value(res, Kit.Icon.Flag, b.Allies.Count().ToString(), Ui.Steel, Ui.Bone, Loc.T("battle.deployed"));

            var roster = E(root, "box roster col", true);
            Frame(roster, art, Ui.Steel);
            L(roster, Loc.T("battle.formation"), "box-title display");
            var pop = b.Used(t => t.Pop);
            var sil = b.Used(t => t.Silver);
            foreach (var k in stg.Units)
            {
                var t = GameData.Type(k);
                var can = pop + t.Pop <= stg.Pop && sil + t.Silver <= stg.Silver;
                var kind = k;
                RosterRow(roster, t, k, s.DeployType == k, can, st.Upgrades, () => a.Pick?.Invoke(kind));
            }
            var acts = E(roster, "acts row");
            Btn(acts, Loc.T("battle.recallAll"), a.Clear, BtnKind.Normal);
            Btn(acts, Loc.T("battle.sample"), a.Sample, BtnKind.Normal);

            var brief = E(root, "box brief col", true);
            Frame(brief, art, Ui.SteelDim);
            Tip(brief, Loc.Text(stg.Brief), 600);
            Def(brief, Kit.Icon.Boss, Ui.Blood, Loc.T("battle.defeatBoss", stg.BossName), Loc.T("battle.objective"));
            Def(brief, Kit.Icon.Star, Ui.Candle, Loc.Text(stg.Bonus.Text), Loc.T("battle.bonus"));
            Def(brief, Kit.Icon.Skulls, Ui.Blood, Kit.EnemyList(stg), Loc.T("battle.enemy"));
        }

        void DeployHover(VisualElement root, BattleSession s, HudState st, BattleActions a)
        {
            var b = s.Battle;
            var ins = s.InspectUnit is int id ? b.Find(id) : null;
            var show = st.Hover ?? ins;
            if (show == null) return;
            var p = Panel(root, b, show, "deploy-side", true);
            if (show.Side != Side.Ally) return;
            var uid = show.Id;
            Btn(p, Loc.T("unit.card.recall"), () => a.Recall?.Invoke(uid), BtnKind.Normal, true, null, "recall");
        }

        void RosterRow(VisualElement parent, UnitType t, string k, bool on, bool can, IEnumerable<string> upgrades, Action pick)
        {
            var row = E(parent, "roster-row row", true);
            Frame(row, art, on ? Ui.Steel : Ui.Line);
            row.Q(className: "panel-frame").AddToClassList("thin");
            Icon(row, art.Icon(k), on ? Ui.Steel : Ui.Muted, "icon roster-icon");
            var col = E(row, "roster-info col");
            var top = E(col, "row");
            L(top, Loc.Text(t.Name), "roster-name");
            Value(top, Kit.Icon.Pop, t.Pop.ToString(), can ? Ui.Candle : Ui.Blood, can ? Ui.Muted : Ui.Blood, Loc.T("battle.pop"));
            if (t.Silver > 0) Value(top, Kit.Icon.Silver, t.Silver.ToString(), can ? Ui.Candle : Ui.Blood, can ? Ui.Muted : Ui.Blood, Loc.T("battle.silver"));
            Stats(col, Battle.Preview(k, upgrades));
            Click(row, pick);
        }

        void Stats(VisualElement parent, Unit u)
        {
            var row = E(parent, "unit-stats row");
            Value(row, Kit.Icon.Hp, u.Hp.ToString(), Ui.SteelDim, Ui.Muted, Loc.T("unit.card.hp"));
            Value(row, Kit.Icon.Atk, u.Atk + (u.Bane > 0 ? "+" + u.Bane : ""), Ui.SteelDim, Ui.Muted, Loc.T("unit.card.atk"));
            Value(row, Kit.Icon.Def, u.Def.ToString(), Ui.SteelDim, Ui.Muted, Loc.T("unit.card.def"));
            Value(row, Kit.Icon.Mov, u.Mov.ToString(), Ui.SteelDim, Ui.Muted, Loc.T("unit.card.mov"));
            Value(row, Kit.Icon.Rng, u.Rng.ToString(), Ui.SteelDim, Ui.Muted, Loc.T("unit.card.rng"));
        }

        void Def(VisualElement parent, string icon, Color iconColor, string value, string tipText)
        {
            var row = E(parent, "def row", true);
            Icon(row, icon, iconColor, "icon def-icon");
            L(row, value, "def-text");
            Tip(row, tipText, 300);
        }

        // ---------- player / enemy turn (battle-hud.md 3, 4) ----------

        void Fight(VisualElement root, BattleSession s, HudState st, BattleActions a)
        {
            var b = s.Battle;
            var stats = E(root, "fight-stats row");
            var spawns = b.Enemies.Count(e => e.Role == Role.Spawn);
            Value(stats, Kit.Icon.Turn, b.Turn.ToString(), Ui.Bone, Ui.Bone, Loc.T("battle.turn"));
            Value(stats, Kit.Icon.Pop, b.Allies.Count().ToString(), Ui.Steel, Ui.Steel, Loc.T("battle.allies"));
            Value(stats, Kit.Icon.Skulls, b.Enemies.Count(e => e.Role != Role.Spawn).ToString(), Ui.Blood, Ui.Blood, Loc.T("battle.enemies"));
            Value(stats, Kit.Icon.Fang, spawns + (b.Corpses.Count > 0 ? " +" + b.Corpses.Count : ""), Ui.Spawn, Ui.Spawn, Loc.T("unit.card.spawn"));
            // Direction review: Alt is the only way to read enemy movement, so its key stays on screen.
            var alt = E(stats, "row", true);
            L(alt, "Alt", st.Alt ? "alt-key on" : "alt-key");
            Icon(alt, Kit.Icon.Mov, st.Alt ? Ui.Blood : Ui.Muted, "icon alt-icon");
            Tip(alt, Loc.T("hud.altTip"), 200);
            if (b.Vampire is Unit v && !v.Asleep) Boss(root, v);
            Log(root, b, st, a);
        }

        void FightHover(VisualElement root, BattleSession s, HudState st, BattleActions a)
        {
            var b = s.Battle;
            var sel = s.SelectedUnit;
            if (!st.EnemyPlaying)
            {
                var ally = sel ?? (st.Hover != null && st.Hover.Side == Side.Ally ? st.Hover : null);
                if (ally != null) Panel(root, b, ally, "ally-side", sel == ally);
                if (sel != null && b.Phase == Phase.Player && !sel.Acted) Actions(root, s, a);
            }
            var mendTarget = sel != null && s.Mode == SelectMode.Mend && b.CanMend(sel, st.Hover) ? st.Hover : null;
            if (st.HoverCorpse != null) CorpsePanel(root, st.HoverCorpse);
            else if (mendTarget != null) Panel(root, b, mendTarget, "other-side", true, b.MendAmount(sel, mendTarget));
            else if (st.Hover != null && st.Hover.Side == Side.Enemy) Panel(root, b, st.Hover, "other-side", true);

            var atk = new HashSet<Hex>(s.Overlay().Where(m => m.Mark == Mark.Atk).Select(m => m.Hex));
            foreach (var u in b.Units)
            {
                var dmg = sel != null && st.Hover == u && atk.Contains(u.Pos) ? b.Damage(sel, u) : 0;
                Head(headLayer, b, u, u == st.Hover || u == sel, st.Alt && u.Side == Side.Enemy, dmg);
            }
            if (st.Hover != null && heads.TryGetValue(st.Hover.Id, out var top)) top.Root.BringToFront();
        }

        void Boss(VisualElement root, Unit v)
        {
            var bar = E(root, "boss");
            Picture(bar, S("SPR_DarkFantasyMenus_Frame_Bar_Medium_01_Background"), Ui.Deep);
            var f = Mathf.Clamp01((float)v.Hp / v.MaxHp);
            E(bar, "boss-fill").style.width = Mathf.Max(0f, 536f * f - 24f);
            Picture(E(bar, "boss-frame"), S("SPR_DarkFantasyMenus_Frame_Bar_Medium_01"), Ui.Blood);
            Watch(L(bar, Loc.Text(v.Name) + "   " + v.Hp + "/" + v.MaxHp, "boss-name"), "boss");
            Icon(bar, "SPR_DarkFantasyMenus_Greeble_DemonSkull_01", Ui.Blood, "icon boss-skull left");
            Icon(bar, "SPR_DarkFantasyMenus_Greeble_DemonSkull_01", Ui.Blood, "icon boss-skull right");
        }

        static readonly Dictionary<string, (string icon, Color color)> LogIcon = new Dictionary<string, (string, Color)>
        {
            ["k"] = ("ICON_DarkFantasy_Status_Dead_01_Clean", Ui.Blood), ["s"] = ("ICON_DarkFantasy_Status_Vampiric_01_Clean", Ui.Spawn),
            ["h"] = ("ICON_DarkFantasy_Status_Targeted_01_Stroke", Ui.MoonBlood),
        };

        void Log(VisualElement root, Battle b, HudState st, BattleActions a)
        {
            var col = E(root, "log col");
            var head = L(col, Loc.T("battle.log") + (st.LogOpen ? " ▴" : " ▾"), "log-head");
            head.pickingMode = PickingMode.Position;
            Click(head, a.ToggleLog);
            if (!st.LogOpen)
            {
                foreach (var l in b.Log.Take(1)) LogLine(col, l);
                return;
            }
            var view = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            view.AddToClassList("log-lines");
            col.Add(view);
            foreach (var l in b.Log) LogLine(view, l);
        }

        void LogLine(VisualElement parent, LogEntry l)
        {
            var row = E(parent, "log-line row");
            if (LogIcon.TryGetValue(l.Style, out var ic)) Icon(row, ic.icon, ic.color, "icon log-icon");
            L(row, Loc.Text(l.Text), "log-text", l.Style == "t" ? Ui.Dim : Ui.Muted);
        }

        VisualElement StatCell(VisualElement row, string icon, string value, string label, Color iconColor, Color valueColor, string watch = null)
        {
            var cell = E(row, "stat", true);
            Icon(cell, icon, iconColor, "icon stat-icon");
            var v = L(cell, value, "stat-value", valueColor);
            if (watch != null) Watch(v, watch);
            Tip(cell, label, 300);
            return cell;
        }

        VisualElement PanelFrame(VisualElement root, string name, string side, Color edge)
        {
            var p = E(root, "unit " + side, true);
            p.name = name;
            Frame(p, art, edge);
            if (!lastPanels.Contains(name)) Fx.Appear(p);
            panels.Add(name);
            return p;
        }

        VisualElement Panel(VisualElement root, Battle b, Unit o, string side, bool lit, int heal = 0)
        {
            var spawn = o.Role == Role.Spawn;
            var color = o.Side == Side.Ally ? Ui.Steel : spawn ? Ui.Spawn : Ui.Blood;
            var dim = o.Side == Side.Ally ? Ui.SteelDim : spawn ? Ui.Alpha(Ui.Spawn, 0.6f) : Ui.BloodDim;
            var p = PanelFrame(root, "Panel " + o.Id, side, lit ? color : dim);
            if (o.Side == Side.Enemy && !spawn) Spikes(p, color);
            if (spawn) CornerBadge(p, "ICON_DarkFantasy_Status_Vampiric_01_Clean", Ui.Spawn);
            var col = E(p, "unit-body col");
            var head = E(col, "unit-head row");
            if (o.Type != null && art.Icon(o.Type) is string wi) Icon(head, wi, color, "icon unit-weapon");
            L(head, spawn ? Loc.T("hud.spawnOf", o.Origin) : Loc.Text(o.Name), "unit-name display");
            var big = E(col, "big row");
            var hp = StatCell(big, Kit.Icon.Hp, o.Hp + "/" + o.MaxHp, Loc.T("unit.card.hp"), color, o.Side == Side.Ally && o.Hp * 3 <= o.MaxHp ? Ui.Blood : Ui.Bone, "hp" + o.Id);
            if (heal > 0) L(hp, "+" + heal, "stat-value stat-heal");
            StatCell(big, Kit.Icon.Mov, o.Mov.ToString(), Loc.T("unit.card.mov"), color, Ui.Bone, "mov" + o.Id);
            var small = E(col, "small row");
            StatCell(small, Kit.Icon.Atk, o.Atk + (o.Bane > 0 ? "+" + o.Bane : ""), Loc.T("unit.card.atk"), color, Ui.Muted);
            StatCell(small, Kit.Icon.Def, o.Def.ToString(), Loc.T("unit.card.def"), color, Ui.Muted);
            StatCell(small, Kit.Icon.Rng, o.Rng.ToString(), Loc.T("unit.card.rng"), color, Ui.Muted);
            var (warn, wc) = Warning(b, o);
            if (warn != null) L(col, warn, "warn", wc);
            if (o.Side == Side.Ally && BattleScreen.Skills(o) is string sk) L(col, sk, "skills");
            Tip(p, BattleScreen.Note(o), 500);
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

        void CorpsePanel(VisualElement root, Corpse c)
        {
            var p = PanelFrame(root, "Corpse " + c.Id, "other-side", Ui.Spawn);
            CornerBadge(p, "ICON_DarkFantasy_Status_Vampiric_01_Clean", Ui.Spawn);
            var col = E(p, "unit-body col");
            var head = E(col, "unit-head row");
            L(head, Loc.T("unit.card.corpseOf", c.Name), "unit-name display");
            L(head, Loc.T("unit.card.corpse"), "unit-tag");
            var big = E(col, "big row");
            StatCell(big, Kit.Icon.Hp, c.MaxHp.ToString(), Loc.T("unit.card.hp"), Ui.Spawn, Ui.Bone);
            StatCell(big, Kit.Icon.Mov, c.Mov.ToString(), Loc.T("unit.card.mov"), Ui.Spawn, Ui.Bone);
            var small = E(col, "small row");
            StatCell(small, Kit.Icon.Atk, c.Atk.ToString(), Loc.T("unit.card.atk"), Ui.Spawn, Ui.Muted);
            StatCell(small, Kit.Icon.Def, "0", Loc.T("unit.card.def"), Ui.Spawn, Ui.Muted);
            StatCell(small, Kit.Icon.Rng, "1", Loc.T("unit.card.rng"), Ui.Spawn, Ui.Muted);
            L(col, Loc.T("hud.warn.corpse"), "warn", Ui.Spawn);
        }

        // .decor stands out of its frame on purpose (UiAudit skips it); .overhang is its full-size holder.
        void Spikes(VisualElement p, Color c)
        {
            var decor = E(p, "overhang decor");
            Icon(decor, "SPR_DarkFantasyMenus_Greeble_Spikes_02_TopLeft", c, "icon spike left");
            Icon(decor, "SPR_DarkFantasyMenus_Greeble_Spikes_02_TopRight", c, "icon spike right");
        }

        void CornerBadge(VisualElement p, string icon, Color c) => Icon(E(p, "overhang decor"), icon, c, "icon corner-badge");

        void Actions(VisualElement root, BattleSession s, BattleActions a)
        {
            var exec = s.Mode == SelectMode.Execute;
            var mend = s.Mode == SelectMode.Mend;
            var sel = s.SelectedUnit;
            var row = E(root, "actions");
            var key = 1;
            foreach (var (icon, label, act, on, danger) in new[]
            {
                (Kit.Icon.Dead, Loc.T(exec ? "unit.card.cancelExec" : "unit.card.exec"), a.Exec, exec, true),
                (Kit.Icon.Wait, Loc.T("unit.card.wait"), a.Wait, false, false),
                (Kit.Icon.Heal, Loc.T(mend ? "hud.cancelMend" : "hud.mend"), a.Mend, mend, false),
            })
            {
                if (act == a.Mend && !sel.Has(Ability.Mend)) continue;
                var slot = E(row, "slot", true);
                Frame(slot, art, on ? (danger ? Ui.Blood : Ui.Steel) : danger ? Ui.BloodDim : Ui.SteelDim);
                slot.Q(className: "panel-frame").AddToClassList("thin");
                Icon(slot, icon, danger ? Ui.Blood : Ui.Steel);
                L(slot, (key++).ToString(), "key");
                Click(slot, act);
                Tip(slot, label, 200);
            }
        }

        // ---------- heads (ui-style.md 8-1, 8-3, 8-4) ----------

        const float HeadBarW = 72f;

        void Head(VisualElement layer, Battle b, Unit u, bool named, bool showMov, int damage)
        {
            var cells = Mathf.Max(1, u.MaxHp);
            var full = u.Hp;
            var lost = damage > 0 ? full - Mathf.Max(0, u.Hp - damage) : 0;
            var color = u.Side == Side.Ally ? Ui.Steel : u.Role == Role.Spawn ? Ui.Spawn : Ui.Blood;
            var rt = E(layer, "head");
            rt.style.display = DisplayStyle.None;
            E(rt, "head-back");
            var imgs = new VisualElement[cells];
            for (var i = 0; i < cells; i++)
            {
                var c = E(rt, "cell");
                c.style.left = HeadBarW * i / cells;
                c.style.width = HeadBarW / cells;
                c.style.backgroundColor = i < full ? color : Ui.Deep;
                imgs[i] = c;
                if (i >= full - lost && i < full) blink.Add(c);
            }
            for (var i = 1; i < cells; i++) E(rt, "divider").style.left = HeadBarW * i / cells;
            if (u.Def > 0)
            {
                var shields = E(rt, "shields");
                for (var i = 0; i < u.Def; i++) Icon(shields, Kit.Icon.BoardDef, Ui.Bone, "icon shield");
            }
            if (damage > 0)
            {
                var at = E(rt, "damage");
                if (damage >= u.Hp) Icon(at, "ICON_DarkFantasy_Status_Dead_01_Stroke", Color.white);
                else L(at, "−" + damage, "outline", u.Side == Side.Ally ? Ui.Blood : Ui.Bone);
            }
            // Stacked above the bar: name, enemy move/range (Alt), the vampire's target mark.
            var y = u.Def > 0 ? 20f : 12f;
            if (named)
            {
                L(rt, Loc.Text(u.Name) + "  " + u.Hp + "/" + u.MaxHp, "head-name outline").style.bottom = y - 4f;
                y += 18f;
            }
            if (showMov)
            {
                var m = E(rt, "head-mov");
                m.style.bottom = y - 4f;
                Icon(m, "ICON_DarkFantasy_Stat_Speed_01_Stroke", Ui.Blood);
                L(m, u.Mov.ToString(), "outline");
                if (u.Rng >= 2)
                {
                    Icon(m, Kit.Icon.Rng, Ui.Blood);
                    L(m, u.Rng.ToString(), "outline");
                }
                y += 24f;
            }
            if (b.EatTarget == u.Id) Icon(rt, Kit.Icon.Target, Ui.Blood, "icon head-target").style.bottom = y;
            heads[u.Id] = new HeadMark { Root = rt, Cells = imgs, MaxHp = u.MaxHp, Color = color };
        }

        // ---------- result (battle-hud.md 5) ----------

        public void Over(VisualElement root, OverInfo o, BattleActions a)
        {
            root.Clear();
            var veil = E(root, "veil", true);
            veil.RegisterCallback<ClickEvent>(e => { if (e.target == veil) a.CloseOver?.Invoke(); });
            var box = E(veil, "window over-box col", true);
            var edge = o.Won ? Ui.Steel : Ui.Blood;
            Frame(box, art, edge);
            // ui-style.md 14-4: a big emblem carries the result, like the reference's Mission Failed screen. Large_01 is the demon skull, so only defeat wears it.
            var emblem = E(box, "emblem decor");
            var crest = art.UiPrefab(o.Won ? MenuToolkit.Crest : "Crest_DarkFantasyMenus_Large_01");
            if (crest != null) Prefab(emblem, crest, edge);
            else Icon(emblem, o.Won ? Kit.Icon.Trophy : Kit.Icon.Dead, edge);
            var st = o.Stats;
            var stg = o.Stage;
            var title = L(box, Loc.T(o.Won ? "over.won" : "over.lost"), "over-title display", o.Won ? Ui.Sun : Ui.Blood);
            var titleTip = o.Won ? Loc.T("over.wonLine", o.Turn, stg.BossName) : Loc.T("over.lostLine");

            var tally = E(box, "tally row");
            Value(tally, Kit.Icon.Dead, st.Lost.ToString(), Ui.Muted, Ui.Bone, Loc.T("over.fallen"));
            Value(tally, Kit.Icon.Skulls, st.Executed.ToString(), Ui.Blood, Ui.Bone, Loc.T("over.execs"));
            Value(tally, Kit.Icon.Fang, st.Risen.ToString(), Ui.Spawn, Ui.Bone, Loc.T("over.risen"));
            Value(tally, Kit.Icon.Target, st.Drained.ToString(), Ui.MoonBlood, Ui.Bone, Loc.T("over.drained"));

            var ok = o.Won && o.BonusOk;
            if (o.Won)
            {
                var rew = E(box, "reward row");
                var bonusTip = Loc.T("over.bonus", Loc.Text(stg.Bonus.Text)) + " · " + (ok ? Loc.T("over.done") : Loc.T("over.failed"));
                if (o.Camp)
                {
                    var r = o.Result;
                    Value(rew, Kit.Icon.Gold, r.ClearGold > 0 ? "+" + r.ClearGold : "0", Ui.Candle, r.ClearGold > 0 ? Ui.Candle : Ui.Dim,
                        r.FirstClear ? Loc.T("over.firstClear") : Loc.T("over.replayClear"));
                    Value(rew, Kit.Icon.Star, !ok ? "✕" : r.BonusGold > 0 ? "+" + r.BonusGold : "✓", ok ? Ui.Candle : Ui.Dim, ok ? Ui.Candle : Ui.Dim, bonusTip);
                    var nxt = Array.IndexOf(Campaign.Stages, stg) + 1;
                    var next = nxt < Campaign.Stages.Length ? Campaign.Stages[nxt] : null;
                    if (r.FirstClear && next != null)
                    {
                        var news = E(box, "unlocked row", true);
                        Icon(news, Kit.Icon.Flag, Ui.Sun);
                        L(news, Loc.Text(next.Title));
                        foreach (var u in next.Units.Where(u => GameData.Type(u).UnlockAt == nxt + 1))
                            Tip(Icon(news, art.Icon(u), Ui.Candle, "icon new"), Loc.T("camp.new", GameData.Type(u).Name), 200);
                        Tip(news, Loc.T("over.nextOpen", next.Label, next.Title), 400);
                    }
                    if (r.FirstClear && next == null) titleTip += "\n" + Loc.T("over.campaignDone");
                }
                else Value(rew, Kit.Icon.Star, ok ? "✓" : "✕", ok ? Ui.Candle : Ui.Dim, ok ? Ui.Candle : Ui.Dim, bonusTip);
            }
            Tip(title, titleTip, 300);

            var acts = E(box, "over-acts row");
            var w = o.Camp ? 178f : 270f;
            if (o.Camp) Btn(acts, Loc.T("over.toCamp"), a.ToCamp, BtnKind.Primary).style.width = w;
            Btn(acts, Loc.T("over.retry"), a.Retry, o.Camp ? BtnKind.Normal : BtnKind.Primary).style.width = w;
            Btn(acts, Loc.T("over.redeploy"), a.Redeploy, BtnKind.Normal).style.width = w;
            UseFonts(root);
        }
    }
}
