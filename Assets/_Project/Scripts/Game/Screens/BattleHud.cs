using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    // Battle HUD in the "달과 피" style (docs/art/ui-style.md). Sizes are 1920x1080 px.
    public sealed class BattleHud
    {
        const float Edge = 24f, FramePpu = 9f;

        readonly ArtCatalog art;
        public readonly List<Graphic> Pulse = new List<Graphic>(), Blink = new List<Graphic>();
        public readonly Dictionary<int, RectTransform> Heads = new Dictionary<int, RectTransform>();

        public BattleHud(ArtCatalog art) => this.art = art;

        Sprite S(string name) => art.Ui(name);

        static Text T(Transform p, string s, float px, Color c, bool bold = false, bool display = false, TextAnchor a = TextAnchor.UpperLeft)
        {
            var t = Ui.Text(p, s, px / Ui.Base, c, bold, display, a);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        public void Build(RectTransform root, BattleSession s, Unit hover, bool alt, bool enemyPlaying, bool leaveArmed, bool rulesOpen, BattleActions a, Action<bool> rulesToggled)
        {
            Pulse.Clear();
            Blink.Clear();
            Heads.Clear();
            var b = s.Battle;
            var hot = b.MoonNext();
            if (hot) Vignette(root, 0.15f);
            Stats(root, b);
            Moon(root, b, hot);
            if (b.Vampire is Unit v && !v.Asleep) Boss(root, v);
            Log(root, b);

            var sel = s.SelectedUnit;
            var ally = sel ?? (hover != null && hover.Side == Side.Ally ? hover : null);
            if (ally != null) Panel(root, b, ally, new Vector2(0f, 0f), new Vector2(Edge, Edge + 48f), sel == ally);
            if (hover != null && hover.Side == Side.Enemy) Panel(root, b, hover, new Vector2(1f, 0f), new Vector2(-Edge - 216f, Edge + 48f), true);
            if (sel != null && b.Phase == Phase.Player && !sel.Acted) Actions(root, s, a);
            EndTurn(root, b, enemyPlaying, a);
            Corner(root, leaveArmed, rulesOpen, a, rulesToggled);
            var hint = T(Ui.Place(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, Edge + 96f), new Vector2(1000f, 20f)), BattleScreen.Hint(s, enemyPlaying), 14, Ui.Dim, false, false, TextAnchor.MiddleCenter);
            Ui.Stretch(hint.rectTransform);

            var heads = Ui.Node(root, "Heads");
            Ui.Stretch(heads);
            heads.SetAsFirstSibling();
            var atk = new HashSet<Hex>(s.Overlay().Where(m => m.Mark == Mark.Atk).Select(m => m.Hex));
            foreach (var u in b.Units) Head(heads, b, u, u == hover || u == sel, alt && u.Side == Side.Enemy,
                sel != null && hover == u && atk.Contains(u.Pos) ? b.Damage(sel, u) : 0);
            if (hover != null && Heads.TryGetValue(hover.Id, out var top)) top.SetAsLastSibling();
            if (rulesOpen) RulesPopup(root, rulesToggled);
        }

        static Sprite edgeFade;

        // ui-style.md 7-2: a 120 px fade on all four edges, as one full-screen texture.
        static Sprite EdgeFade()
        {
            if (edgeFade != null) return edgeFade;
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < N; y++)
                for (var x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                    var a = Mathf.Max(Mathf.Clamp01(1f - Mathf.Min(u, 1f - u) / (120f / 1920f)), Mathf.Clamp01(1f - Mathf.Min(v, 1f - v) / (120f / 1080f)));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return edgeFade = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        void Vignette(RectTransform root, float alpha)
        {
            var img = Ui.Sprite(root, "Vignette", EdgeFade(), Ui.Alpha(Ui.MoonBlood, alpha));
            img.preserveAspect = false;
            Ui.Stretch(img.rectTransform);
            img.rectTransform.SetAsFirstSibling();
        }

        void Stats(RectTransform root, Battle b)
        {
            var row = Ui.Row(root, "Stats", 28, 0, null, null, TextAnchor.UpperLeft);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0f, 1f);
            row.anchoredPosition = new Vector2(Edge, -Edge);
            row.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            row.sizeDelta = new Vector2(0f, 48f);
            var spawns = b.Enemies.Count(e => e.Role == Role.Spawn);
            foreach (var (v, l, c) in new[]
            {
                (b.Turn.ToString(), "턴", Ui.Bone), (b.Allies.Count().ToString(), "아군", Ui.Steel),
                (b.Enemies.Count(e => e.Role != Role.Spawn).ToString(), "적", Ui.Blood),
                (spawns + (b.Corpses.Count > 0 ? " +" + b.Corpses.Count : ""), "스폰", Ui.Spawn),
            })
            {
                var col = Ui.Col(row, "Stat " + l, 0);
                T(col, v, 20, c, true);
                T(col, l, 14, Ui.Muted);
            }
        }

        void Moon(RectTransform root, Battle b, bool hot)
        {
            const float D = 112f;
            var box = Ui.Place(root, "Moon", new Vector2(0.5f, 1f), new Vector2(0f, -Edge), new Vector2(D, D));
            var disk = S("SPR_DarkFantasyMenus_Frame_Ring_01_Background");
            Ui.Stretch(Ui.Sprite(box, "Back", disk, Ui.Deep).rectTransform);
            var max = Rules.MoonMax;
            for (var i = 0; i < max; i++)
            {
                var img = Ui.Sprite(box, "Phase " + i, disk, i < b.Moon ? (hot ? Ui.MoonBlood : Ui.Sun) : Ui.Alpha(Ui.Panel2, 1f));
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = (int)Image.Origin360.Top;
                img.fillClockwise = true;
                img.fillAmount = 1f / max - 0.015f;
                img.preserveAspect = true;
                Ui.Stretch(img.rectTransform, 12, 12, 12, 12);
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, -360f * i / max);
                if (hot && i < b.Moon) Pulse.Add(img);
            }
            var v = b.Vampire;
            if (v != null && v.Asleep)
            {
                var pivot = Ui.Place(box, "WakeAt", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                pivot.localEulerAngles = new Vector3(0f, 0f, -360f * Rules.MoonWake / max);
                var bar = Ui.Place(pivot, "Coffin", new Vector2(0.5f, 0.5f), new Vector2(0f, D / 2f - 14f), new Vector2(5f, 26f));
                Ui.Bg(bar, Ui.Bone, null, false);
            }
            Ui.Stretch(Ui.Sprite(box, "Ring", S("SPR_DarkFantasyMenus_Frame_Ring_01"), hot ? Ui.MoonBlood : Ui.Steel).rectTransform, -6, -6, -6, -6);
            var label = Ui.Col(root, "MoonLabel", 0);
            label.anchorMin = label.anchorMax = label.pivot = new Vector2(0.5f, 1f);
            label.anchoredPosition = new Vector2(0f, -Edge - D - 6f);
            label.sizeDelta = new Vector2(300f, 44f);
            T(label, "달 " + b.Moon + "/" + max, 20, Ui.Bone, true, false, TextAnchor.UpperCenter);
            var state = hot ? "다음 턴 보름달" : v != null && v.Asleep ? Rules.MoonWake + "에 관이 열림" : "";
            if (state.Length > 0) T(label, state, 14, hot ? Ui.Blood : Ui.Muted, hot, false, TextAnchor.UpperCenter);
        }

        void Boss(RectTransform root, Unit v)
        {
            var bar = Ui.Place(root, "Boss", new Vector2(0.5f, 1f), new Vector2(0f, -Edge - 112f - 60f), new Vector2(560f, 40f));
            Ui.Stretch(Ui.Sprite(bar, "Back", S("SPR_DarkFantasyMenus_Frame_Bar_Medium_01_Background"), Ui.Deep, 12f).rectTransform);
            var fill = Ui.Node(bar, "Fill");
            Ui.Bg(fill, Ui.Blood, null, false);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01((float)v.Hp / v.MaxHp), 1f);
            fill.offsetMin = new Vector2(24f, 10f);
            fill.offsetMax = new Vector2(-24f * (float)v.Hp / v.MaxHp, -10f);
            Ui.Stretch(Ui.Sprite(bar, "Frame", S("SPR_DarkFantasyMenus_Frame_Bar_Medium_01"), Ui.Blood, 12f).rectTransform);
            var name = T(bar, v.Name + "   " + v.Hp + "/" + v.MaxHp, 17, Ui.Bone, true, true, TextAnchor.MiddleCenter);
            Ui.Stretch(name.rectTransform);
            name.gameObject.AddComponent<Outline>().effectColor = Ui.Deep;
            foreach (var x in new[] { 0f, 1f })
            {
                var skull = Ui.Sprite(bar, "Skull", S("SPR_DarkFantasyMenus_Greeble_DemonSkull_01"), Ui.Blood);
                var rt = skull.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(x, 0.5f);
                rt.anchoredPosition = new Vector2(x == 0f ? -30f : 30f, 0f);
                rt.sizeDelta = new Vector2(48f, 48f);
            }
        }

        void Log(RectTransform root, Battle b)
        {
            var col = Ui.Col(root, "Log", 4);
            col.anchorMin = col.anchorMax = col.pivot = new Vector2(1f, 1f);
            col.anchoredPosition = new Vector2(-Edge, -Edge);
            col.sizeDelta = new Vector2(320f, 0f);
            col.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            T(col, "전투 기록 ▾", 14, Ui.Dim, false, false, TextAnchor.UpperRight);
            foreach (var l in b.Log.Take(3))
            {
                var t = T(col, l.Text, 17, BattleScreen.LogColor.TryGetValue(l.Style, out var c) ? Color.Lerp(c, Ui.Muted, 0.3f) : Ui.Muted, false, false, TextAnchor.UpperRight);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
        }

        void Panel(RectTransform root, Battle b, Unit o, Vector2 anchor, Vector2 pos, bool lit)
        {
            var spawn = o.Role == Role.Spawn;
            var side = o.Side == Side.Ally ? Ui.Steel : spawn ? Ui.Spawn : Ui.Blood;
            var dim = o.Side == Side.Ally ? Ui.SteelDim : spawn ? Ui.Alpha(Ui.Spawn, 0.6f) : Ui.BloodDim;
            var p = Ui.Place(root, "Panel " + o.Id, anchor, pos, new Vector2(340f, 190f));
            Ui.Frame(p, S("SPR_DarkFantasyMenus_Frame_Box_Medium_02"), S("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"), lit ? side : dim, FramePpu);
            if (o.Side == Side.Enemy && !spawn)
                foreach (var (x, n) in new[] { (0f, "SPR_DarkFantasyMenus_Greeble_Spikes_02_TopLeft"), (1f, "SPR_DarkFantasyMenus_Greeble_Spikes_02_TopRight") })
                {
                    var g = Ui.Sprite(p, "Spikes", S(n), side).rectTransform;
                    g.anchorMin = g.anchorMax = g.pivot = new Vector2(x, 1f);
                    g.anchoredPosition = new Vector2(x == 0f ? -10f : 10f, 10f);
                    g.sizeDelta = new Vector2(32f, 32f);
                }
            if (spawn)
            {
                var g = Ui.Sprite(p, "Vampiric", S("ICON_DarkFantasy_Status_Vampiric_01_Clean"), Ui.Spawn).rectTransform;
                g.anchorMin = g.anchorMax = g.pivot = new Vector2(0f, 1f);
                g.anchoredPosition = new Vector2(-6f, 6f);
                g.sizeDelta = new Vector2(24f, 24f);
            }
            var col = Ui.Col(p, "Body", 6, 16);
            Ui.Stretch(col);
            col.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(20, 20, 16, 14);
            var head = Ui.Row(col, "Head", 10, 0, null, null, TextAnchor.LowerLeft);
            T(head, spawn ? o.Origin + " · 스폰" : o.Name, 22, Ui.Bone, true, true);
            T(head, o.Side == Side.Ally ? "아군" : o.Role == Role.Vampire ? "보스" : spawn ? "스폰" : "하수인", 14, side);
            var big = Ui.Row(col, "Big", 22);
            Stat(big, "ICON_DarkFantasy_Stat_Health_01_Clean", o.Hp + "/" + o.MaxHp, "체력", 34, side, o.Side == Side.Ally && o.Hp * 3 <= o.MaxHp ? Ui.Blood : Ui.Bone);
            Stat(big, "ICON_DarkFantasy_Stat_Speed_01_Clean", o.Mov.ToString(), "이동", 34, side, Ui.Bone);
            var small = Ui.Row(col, "Small", 18);
            Stat(small, "ICON_DarkFantasy_Stat_Strength_01_Clean", o.Atk + (o.Bane > 0 ? "+" + o.Bane : ""), "공격", 20, side, Ui.Muted);
            Stat(small, "ICON_DarkFantasy_Status_Defense_01_Clean", o.Def.ToString(), "방어", 20, side, Ui.Muted);
            Stat(small, "ICON_DarkFantasy_Stat_Accuracy_01_Clean", o.Rng.ToString(), "사거리", 20, side, Ui.Muted);
            string warn = null;
            var warnColor = Ui.Blood;
            if (o.Side == Side.Ally && b.EatTarget == o.Id) warn = "보름달 표적 · 다음 적 턴에 흡혈당한다";
            else if (o.Side == Side.Ally && b.Board.Lit(o.Pos)) { warn = "빛 속 · 표적이 되지 않는다"; warnColor = Ui.Candle; }
            else if (o.JustRisen) { warn = "방금 일어남 · 이번 적 페이즈에 행동하지 않는다"; warnColor = Ui.Spawn; }
            if (warn != null) T(col, warn, 14, warnColor, true);
        }

        void Stat(Transform row, string icon, string value, string label, float px, Color iconColor, Color valueColor)
        {
            var cell = Ui.Row(row, "Stat " + label, 6, 0, null, null, TextAnchor.LowerLeft);
            Ui.Size(Ui.Sprite(cell, "Icon", S(icon), iconColor), 24, 24);
            T(cell, value, px, valueColor, true);
            T(cell, label, px / 2f < 14f ? 14f : px / 2f, Ui.Muted);
        }

        void Actions(RectTransform root, BattleSession s, BattleActions a)
        {
            var exec = s.Mode == SelectMode.Execute;
            var row = Ui.Row(root, "ActionBar", 8, 0, null, null, TextAnchor.LowerCenter);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, Edge);
            row.sizeDelta = new Vector2(400f, 84f);
            foreach (var (icon, label, act, on, danger) in new[]
            {
                ("ICON_DarkFantasy_Status_Dead_01_Clean", exec ? "처치 취소" : "아군 처치", a.Exec, exec, true),
                ("ICON_DarkFantasy_Status_Defense_01_Clean", "대기", a.Wait, false, false),
            })
            {
                var col = Ui.Col(row, "Slot " + label, 4);
                col.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
                col.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
                var slot = Ui.Node(col, "Slot");
                Ui.Size(slot, 64, 64);
                Ui.Frame(slot, S("SPR_DarkFantasyMenus_Frame_Box_Medium_02"), S("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"), on ? Ui.Blood : danger ? Ui.BloodDim : Ui.SteelDim, 14f);
                var ic = Ui.Sprite(slot, "Icon", S(icon), danger ? Ui.Blood : Ui.Steel);
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ic.rectTransform.sizeDelta = new Vector2(40f, 40f);
                var btn = slot.gameObject.AddComponent<Button>();
                btn.targetGraphic = slot.GetComponentInChildren<Image>();
                var go = act;
                btn.onClick.AddListener(() => go?.Invoke());
                T(col, label, 14, Ui.Muted, false, false, TextAnchor.UpperCenter);
            }
        }

        void EndTurn(RectTransform root, Battle b, bool enemyPlaying, BattleActions a)
        {
            var label = BattleScreen.PrimaryLabel(b, enemyPlaying, out var enabled);
            var rt = Ui.Place(root, "EndTurn", new Vector2(1f, 0f), new Vector2(-Edge, Edge), new Vector2(200f, 56f));
            var back = Ui.Sprite(rt, "Fill", S("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"), enabled ? Ui.Steel : Ui.SteelDim, FramePpu);
            Ui.Stretch(back.rectTransform);
            back.raycastTarget = true;
            Ui.Stretch(Ui.Sprite(rt, "Frame", S("SPR_DarkFantasyMenus_Frame_Box_Medium_02"), Ui.Bone, FramePpu).rectTransform);
            var t = T(rt, label, 17, Ui.Ink, true, false, TextAnchor.MiddleCenter);
            Ui.Stretch(t.rectTransform);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = back;
            btn.interactable = enabled;
            btn.onClick.AddListener(() => a.Primary?.Invoke());
            var left = b.Phase == Phase.Player ? b.Allies.Count(u => !u.Acted) : 0;
            if (left > 0)
            {
                var badge = Ui.Place(rt, "Badge", new Vector2(0f, 1f), new Vector2(-10f, 10f), new Vector2(28f, 28f));
                Ui.Stretch(Ui.Sprite(badge, "Disk", S("SPR_DarkFantasyMenus_Frame_Ring_01_Background"), Ui.Ink).rectTransform);
                Ui.Stretch(Ui.Sprite(badge, "Ring", S("SPR_DarkFantasyMenus_Frame_Ring_01"), Ui.Steel).rectTransform, -3, -3, -3, -3);
                Ui.Stretch(T(badge, left.ToString(), 14, Ui.Bone, true, false, TextAnchor.MiddleCenter).rectTransform);
            }
            var phase = T(Ui.Place(root, "Phase", new Vector2(1f, 0f), new Vector2(-Edge, Edge + 60f), new Vector2(200f, 20f)),
                b.Phase == Phase.Player ? "아군 페이즈" : b.Phase == Phase.Enemy ? "적 페이즈" : "", 14, Ui.Muted, false, false, TextAnchor.LowerCenter);
            Ui.Stretch(phase.rectTransform);
        }

        void Corner(RectTransform root, bool leaveArmed, bool rulesOpen, BattleActions a, Action<bool> rulesToggled)
        {
            var row = Ui.Row(root, "Corner", 4, 0, null, null, TextAnchor.LowerLeft);
            row.anchorMin = row.anchorMax = row.pivot = Vector2.zero;
            row.anchoredPosition = new Vector2(Edge - 12f, Edge - 6f);
            row.sizeDelta = new Vector2(400f, 36f);
            Ui.Button(row, "?  규칙", () => rulesToggled?.Invoke(!rulesOpen), Look.Ghost, true);
            Ui.Button(row, leaveArmed ? "전투를 포기할까요?" : "나가기", a.Leave, Look.Ghost, true);
        }

        void RulesPopup(RectTransform root, Action<bool> toggled)
        {
            var p = Ui.Col(root, "RulesPopup", 8, 32);
            p.anchorMin = p.anchorMax = p.pivot = new Vector2(0.5f, 0.5f);
            p.sizeDelta = new Vector2(720f, 0f);
            p.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Ui.Frame(p, S("SPR_DarkFantasyMenus_Frame_Box_Medium_02"), S("SPR_DarkFantasyMenus_Frame_Box_Medium_02_Background"), Ui.Steel, FramePpu);
            T(p, "규칙 요약", 32, Ui.Bone, true, true);
            foreach (var line in BattleScreen.RuleLines) Ui.Text(p, line, 17f / Ui.Base, Ui.Muted);
            Ui.Button(p, "닫기", () => toggled?.Invoke(false), Look.Ghost, true);
        }

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
            for (var i = 0; i < cells; i++)
            {
                var c = Ui.Place(rt, "Cell", new Vector2(0f, 0.5f), new Vector2(i * 7f, 0f), new Vector2(6f, 8f));
                c.pivot = new Vector2(0f, 0.5f);
                var img = Ui.Bg(c, i < full ? color : Ui.Deep, null, false);
                if (i >= full - lost && i < full) Blink.Add(img);
            }
            if (per > 1) T(Ui.Place(rt, "Num", new Vector2(1f, 0.5f), new Vector2(4f, 0f), new Vector2(40f, 14f)), u.Hp.ToString(), 14, Ui.Bone, true).rectTransform.pivot = new Vector2(0f, 0.5f);
            var y = 12f;
            if (damage > 0)
            {
                var at = Ui.Place(rt, "Damage", new Vector2(1f, 0.5f), new Vector2(per > 1 ? 34f : 8f, 0f), new Vector2(60f, 28f));
                at.pivot = new Vector2(0f, 0.5f);
                if (damage >= u.Hp) Ui.Place(at, "Dead", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(24f, 24f)).gameObject.AddComponent<Image>().sprite = S("ICON_DarkFantasy_Status_Dead_01_Stroke");
                else Ui.Stretch(T(at, "−" + damage, 20, u.Side == Side.Ally ? Ui.Blood : Ui.Bone, true, false, TextAnchor.MiddleLeft).rectTransform);
            }
            if (named)
            {
                var n = T(Ui.Place(rt, "Name", new Vector2(0.5f, 1f), new Vector2(0f, y - 4f), new Vector2(200f, 18f)), u.Name + "  " + u.Hp + "/" + u.MaxHp, 14, Ui.Bone, true, false, TextAnchor.LowerCenter);
                n.rectTransform.pivot = new Vector2(0.5f, 0f);
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
            Heads[u.Id] = rt;
        }
    }
}
