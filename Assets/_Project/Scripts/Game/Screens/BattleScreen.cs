using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    public sealed class BattleActions
    {
        public Action Primary, Leave, Clear, Sample, Exec, Wait, Retry, Redeploy, ToCamp, CloseOver;
        public Action<bool> Threat;
        public Action<UnitKind> Pick;
        public Action<int> Recall;
    }

    public sealed class OverInfo
    {
        // Prototype finish(): a campaign win pays out and is saved by the caller; a demo win only reports the bonus.
        public static OverInfo From(Battle b, bool demo, Campaign c)
        {
            var won = b.Won == true;
            var ok = won && b.BonusMet;
            var result = won && !demo ? c.Finish(b.Stage, true, ok) : default;
            return new OverInfo { Won = won, Camp = !demo, Stage = b.Stage, Turn = b.Turn, Stats = b.Stats, BonusOk = ok, Result = result };
        }

        public bool Won, Camp;
        public Stage Stage;
        public int Turn;
        public BattleStats Stats;
        public bool BonusOk;
        public StageResult Result;
    }

    public static class BattleScreen
    {
        static string Rich(Color c, string s) => "<color=#" + Ui.Hex(c) + ">" + s + "</color>";

        public static string Subtitle(Stage s, bool demo) => demo ? "데모 전투" : "캠페인 · " + s.Label;

        public static string Title(Stage s) => s.Label + " · " + Rich(Ui.Blood, s.Title);

        public static string PrimaryLabel(Battle b, bool enemyPlaying, out bool enabled)
        {
            enabled = true;
            if (enemyPlaying) { enabled = false; return "적 행동 중…"; }
            switch (b.Phase)
            {
                case Phase.Deploy: enabled = b.Allies.Any(); return "전투 개시";
                case Phase.Player: return "턴 종료";
                case Phase.Enemy: enabled = false; return "적 행동 중…";
            }
            return "결과 보기";
        }

        public static void Header(RectTransform bar, BattleSession s, bool demo, bool enemyPlaying, bool leaveArmed, BattleActions a)
        {
            var b = s.Battle;
            var title = Ui.Col(bar, "Title", 0);
            Ui.Size(title, 300);
            Ui.Text(title, Subtitle(b.Stage, demo), 0.75f, Ui.Muted);
            Ui.Text(title, Title(b.Stage), 1.45f, Ui.Bone, true, true);
            var stats = Ui.Row(bar, "Stats", 20, 0, null, null, TextAnchor.LowerLeft);
            Ui.Size(stats, -1, -1, 1);
            Stats(stats, b);
            var ctrl = Ui.Row(bar, "Ctrl", 10, 0, null, null, TextAnchor.MiddleRight);
            Toggle(ctrl, "적 위협 범위", s.ShowThreat, a.Threat);
            Ui.Button(ctrl, leaveArmed ? "전투를 포기할까요?" : "나가기", a.Leave, Look.Ghost, true);
            var label = PrimaryLabel(b, enemyPlaying, out var enabled);
            Ui.Button(ctrl, label, a.Primary, Look.Primary, false, enabled);
        }

        static void Stat(Transform parent, string value, string label, bool warn = false)
        {
            var col = Ui.Col(parent, "Stat " + label, 0);
            Ui.Size(col, 52, -1, 0);
            Ui.Text(col, value, 1.1f, warn ? Ui.Blood : Ui.Bone, true).horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Text(col, label, 0.72f, warn ? Ui.Blood : Ui.Muted).horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        public static void Stats(Transform parent, Battle b)
        {
            if (b.Phase == Phase.Deploy)
            {
                Stat(parent, b.Used(t => t.Pop) + "/" + b.Stage.Pop, "인구");
                if (b.Stage.Silver > 0) Stat(parent, b.Used(t => t.Silver) + "/" + b.Stage.Silver, "은");
                Stat(parent, b.Allies.Count().ToString(), "배치");
                return;
            }
            var hot = b.MoonNext();
            var v = b.Vampire;
            Stat(parent, b.Moon + "/" + Rules.MoonMax, hot ? "다음 턴 보름달" : v != null && v.Asleep ? Rules.MoonWake + "에 관이 열림" : "달", hot);
            Stat(parent, b.Turn.ToString(), "턴");
            Stat(parent, b.Allies.Count().ToString(), "아군");
            Stat(parent, b.Enemies.Count(e => e.Role != Role.Spawn).ToString(), "적");
            Stat(parent, b.Corpses.Count.ToString(), "변이 대기", b.Corpses.Count > 0);
            Stat(parent, b.Stats.Risen.ToString(), "스폰 발생", b.Stats.Risen > 0);
        }

        static void Toggle(Transform parent, string label, bool on, Action<bool> changed)
        {
            var b = Ui.Button(parent, null, () => changed?.Invoke(!on), Look.Ghost, true);
            var box = Ui.Node(b.transform, "Box");
            Ui.Bg(box, on ? Ui.Candle : Ui.Panel2, Ui.Muted, false);
            Ui.Size(box, 14, 14);
            Ui.Text(b.transform, label, 0.85f, Ui.Bone).horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        public static string Hint(BattleSession s, bool enemyPlaying) => enemyPlaying ? "적이 행동하고 있습니다." : s.Hint;

        static Text Flag(Transform parent, string text, Color edge)
        {
            var row = Ui.Row(parent, "Flag", 8, 0, Ui.Alpha(edge, 0.12f));
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(0, 8, 6, 6);
            var bar = Ui.Node(row, "Edge");
            Ui.Bg(bar, edge, null, false);
            Ui.Size(bar, 3, -1, -1, 1);
            var t = Ui.Text(row, text, 0.82f, Ui.Bone);
            Ui.Size(t, -1, -1, 1);
            return t;
        }

        static Text H2(Transform card, string s) => Ui.Text(card, s, 1.05f, Ui.Bone, true, true);

        static readonly Dictionary<Ability, string> SkillName = new Dictionary<Ability, string>
        {
            [Ability.Pike] = "창벽", [Ability.Ambush] = "매복 사격", [Ability.Cleave] = "베어 넘기기", [Ability.Charge] = "돌격",
        };

        static readonly Dictionary<string, Color> LogColor = new Dictionary<string, Color>
        {
            ["k"] = Ui.Blood, ["s"] = Ui.Spawn, ["h"] = Ui.Sun, ["sys"] = Ui.Muted, ["t"] = Ui.Candle,
        };

        public static void SidePanel(RectTransform side, BattleSession s, IEnumerable<string> upgrades, bool rulesOpen, BattleActions a, Action<bool> rulesToggled)
        {
            var b = s.Battle;
            var stg = b.Stage;
            if (b.Phase == Phase.Deploy)
            {
                var brief = Ui.Card(side, "Brief");
                H2(brief, "작전 개요");
                Ui.Text(brief, stg.Brief, 0.9f, Ui.Muted);
                var obj = Ui.Col(brief, "Objectives", 4);
                Def(obj, "목표", stg.Boss.Name + " 처치");
                Def(obj, "추가 목표", stg.Bonus.Text);
                Def(obj, "자원", CampaignScreen.Resources(stg));
                Def(obj, "적", "스켈레톤 " + (stg.Enemies.Length - 1) + ", " + stg.Boss.Name);

                var roster = Ui.Card(side, "Roster");
                H2(roster, "편성");
                var pop = b.Used(t => t.Pop);
                var sil = b.Used(t => t.Silver);
                foreach (var k in stg.Units)
                {
                    var t = GameData.Type(k);
                    var can = pop + t.Pop <= stg.Pop && sil + t.Silver <= stg.Silver;
                    var on = s.DeployType == k;
                    var kind = k;
                    var btn = Ui.Button(roster, null, () => a.Pick?.Invoke(kind), on ? Look.RosterOn : Look.Roster, true);
                    btn.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
                    btn.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                    Ui.Glyph(btn.transform, t);
                    var col = Ui.Col(btn.transform, "Info", 2);
                    Ui.Size(col, -1, -1, 1);
                    var top = Ui.Row(col, "Top", 8);
                    Ui.Size(Ui.Text(top, t.Name, 0.95f, Ui.Bone, true), -1, -1, 1);
                    Ui.Text(top, "인구 " + t.Pop + (t.Silver > 0 ? " · 은 " + t.Silver : ""), 0.78f, can ? Ui.Muted : Ui.Blood).horizontalOverflow = HorizontalWrapMode.Overflow;
                    Ui.Text(col, CampaignScreen.StatLine(Battle.Preview(k, upgrades)), 0.72f, Ui.Muted);
                }
                Ui.Text(roster, "빈 칸을 눌러 배치. 배치한 유닛은 끌어서 옮기거나 다른 유닛과 바꿀 수 있고, 구역 밖으로 끌어내면 회수됩니다.", 0.8f, Ui.Muted);
                var acts = Ui.Row(roster, "Acts", 8);
                Ui.Button(acts, "모두 회수", a.Clear, Look.Normal, true);
                Ui.Button(acts, "예시 편성", a.Sample, Look.Normal, true);
                var ins = s.InspectUnit is int id ? b.Find(id) : null;
                if (ins != null) UnitCard(side, s, ins, a);
            }
            else
            {
                var corpse = s.InspectCorpse is int cid ? b.Corpses.FirstOrDefault(c => c.Id == cid) : null;
                var ins = corpse == null ? (s.InspectUnit is int id ? b.Find(id) : null) ?? s.SelectedUnit : null;
                if (corpse != null) CorpseCard(side, corpse);
                else if (ins != null) UnitCard(side, s, ins, a);
                else if (b.Phase == Phase.Player)
                {
                    var card = Ui.Card(side, "Turn");
                    H2(card, "아군 페이즈");
                    Ui.Text(card, "행동 가능한 아군 " + b.Allies.Count(u => !u.Acted) + "명. 아군을 눌러 명령하세요.", 0.9f, Ui.Muted);
                }
                var log = Ui.Card(side, "Log");
                H2(log, "전투 기록");
                var list = Ui.Col(log, "Lines", 3);
                foreach (var l in b.Log)
                {
                    var t = Ui.Text(list, l.Text, 0.82f, LogColor.TryGetValue(l.Style, out var c) ? c : Ui.Bone, l.Style == "t");
                    t.name = "Log";
                }
            }
            RulesCard(side, rulesOpen, rulesToggled);
        }

        static void Def(Transform parent, string term, string value)
        {
            var row = Ui.Row(parent, "Def", 10, 0, null, null, TextAnchor.UpperLeft);
            Ui.Size(Ui.Text(row, term, 0.88f, Ui.Muted), 72);
            Ui.Size(Ui.Text(row, value, 0.88f, Ui.Bone), -1, -1, 1);
        }

        static void RoleTag(Transform head, string label, Color color)
        {
            var box = Ui.Row(head, "Role", 0, 0, Ui.Alpha(Ui.Panel, 0f), color);
            box.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(7, 7, 2, 2);
            Ui.Text(box, label, 0.68f, color).horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        static void HpBar(Transform card, float fraction, Color color)
        {
            var back = Ui.Node(card, "HpBar");
            Ui.Bg(back, Ui.Deep, null, false);
            Ui.Size(back, -1, 6);
            var fill = Ui.Node(back, "Fill");
            Ui.Bg(fill, color, null, false);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }

        static void CorpseCard(Transform side, Corpse c)
        {
            var card = Ui.Card(side, "Corpse");
            var head = Ui.Row(card, "Head", 8);
            RoleTag(head, "시신", Ui.Spawn);
            Ui.Text(head, c.Name + "의 시신", 1f, Ui.Bone, true, true);
            Flag(card, "다음 적 페이즈 시작에 스폰으로 일어납니다. 일어난 턴에는 행동하지 않습니다.", Ui.Spawn);
            Ui.Text(card, "체력 " + c.MaxHp + ", 공격 " + c.Atk + ", 방어 " + c.Def + ", 이동 " + c.Mov + "를 물려받고 근접 공격만 합니다. 일어나기 전에 주변 아군을 물리거나 맞을 준비를 하세요.", 0.88f, Ui.Muted);
        }

        public static void UnitCard(Transform side, BattleSession s, Unit o, BattleActions a)
        {
            var b = s.Battle;
            var ally = o.Side == Side.Ally;
            var card = Ui.Card(side, "Unit " + o.Id);
            var head = Ui.Row(card, "Head", 8);
            if (ally) RoleTag(head, "아군", Ui.Steel);
            else if (o.Role == Core.Role.Vampire) RoleTag(head, "보스", Ui.Blood);
            else if (o.Role == Core.Role.Spawn) RoleTag(head, "스폰", Ui.Spawn);
            else RoleTag(head, "하수인", Ui.Muted);
            Ui.Text(head, o.Name, 1f, Ui.Bone, true, true);
            HpBar(card, (float)o.Hp / o.MaxHp, ally ? Ui.Steel : o.Role == Core.Role.Spawn ? Ui.Spawn : Ui.Blood);
            var grid = Ui.Row(card, "Stats", 4);
            grid.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            foreach (var (v, l) in new[] { (o.Hp + "/" + o.MaxHp, "체력"), (o.Atk + (o.Bane > 0 ? "+" + o.Bane : ""), "공격"), (o.Def.ToString(), "방어"), (o.Mov.ToString(), "이동"), (o.Rng.ToString(), "사거리") })
            {
                var cell = Ui.Col(grid, "Cell", 0, 4, Ui.Panel2);
                Ui.Size(cell, -1, -1, 1);
                Ui.Text(cell, v, 1f, Ui.Bone, true, false, TextAnchor.UpperCenter);
                Ui.Text(cell, l, 0.65f, Ui.Muted, false, false, TextAnchor.UpperCenter);
            }
            var v0 = b.Vampire;
            if (ally && b.EatTarget == o.Id)
            {
                var safe = v0 != null && (Hex.Distance(v0.Pos, o.Pos) > Rules.EatRange || b.Board.Lit(o.Pos));
                Flag(card, "보름달 표적. 다음 적 턴에 뱀파이어 " + Rules.EatRange + "칸 안, 빛 밖에 있으면 먹혀 스폰이 됩니다. 범위 밖으로 빼거나, 빛 속에 세우거나, 직접 처치하면 흡혈은 실패합니다." + (safe ? " 지금 위치는 안전합니다." : ""), Ui.Blood);
            }
            if (ally && b.Board.Lit(o.Pos)) Flag(card, "빛 속. 흡혈 대상이 되지 않고, 쓰러져도 변이하지 않습니다.", Ui.Candle);
            if (o.Role == Core.Role.Vampire)
            {
                if (o.Asleep) Flag(card, "관 속. 달이 " + Rules.MoonWake + "이 되면 깨어난다 (지금 " + b.Moon + ").", Ui.Candle);
                else if (b.MoonNext()) Flag(card, "다음 적 턴에 보름달이 뜬다. 표적을 먹으면 그 턴은 움직이지 않고, 놓치면 평소처럼 덤벼든다.", Ui.Blood);
                else Flag(card, "달 " + b.Moon + "/" + Rules.MoonMax + ". 적 턴마다, 유닛이 죽을 때마다 찬다.", Ui.Candle);
            }
            if (o.JustRisen) Flag(card, "방금 일어났습니다. 이번 적 페이즈에는 행동하지 않습니다.", Ui.Spawn);
            Ui.Text(card, Note(o), 0.88f, Ui.Muted);
            if (ally && o.Abil.Count > 0) Ui.Text(card, "기술: " + string.Join(", ", o.Abil.Select(x => SkillName[x])), 0.88f, Ui.Muted);
            if (ally && b.Phase == Phase.Deploy)
            {
                var acts = Ui.Row(card, "Acts", 8);
                var id = o.Id;
                Ui.Button(acts, "회수", () => a.Recall?.Invoke(id), Look.Normal, true);
            }
            if (ally && b.Phase == Phase.Player && s.Selected == o.Id)
            {
                if (o.Acted) Ui.Text(card, "이번 턴 행동을 마쳤습니다.", 0.88f, Ui.Muted);
                else
                {
                    var acts = Ui.Row(card, "Acts", 8);
                    var exec = s.Mode == SelectMode.Execute;
                    Ui.Button(acts, exec ? "처치 취소" : "아군 처치", a.Exec, exec ? Look.DangerOn : Look.Danger, true);
                    Ui.Button(acts, "대기", a.Wait, Look.Normal, true);
                }
            }
        }

        static string Note(Unit o)
        {
            switch (o.Role)
            {
                case Core.Role.Ally: return GameData.Type(o.Type.Value).Desc;
                case Core.Role.Skeleton: return "쓰러뜨린 아군을 변이시키지 않는다.";
                case Core.Role.Vampire:
                    return "관 속에서 잠들어 있다가 달이 " + Rules.MoonWake + "까지 차면 깨어난다. 잠든 동안은 공격받지 않는다. 깨어나면 이동해서 일반 공격을 한다. 달이 " + Rules.MoonMax
                        + "이 되는 적 턴에는 움직이지 않고, 미리 정한 표적이 " + Rules.EatRange + "칸 안·빛 밖에 있으면 먹어치운다(즉사, 체력 " + o.Heal
                        + " 회복). 표적이 없거나 벗어났으면 흡혈은 실패하고 대신 평소처럼 이동해 공격한다. 어느 쪽이든 달은 진다.";
            }
            return o.Origin + "의 체력·공격·방어·이동을 물려받았다. 근접 공격만 한다. 죽인 아군을 스폰으로 만든다.";
        }

        static readonly string[] RuleLines =
        {
            "뱀파이어나 스폰에게 죽은 아군은 시신으로 남고, <b>다음 적 페이즈 시작</b>에 스폰으로 일어납니다. 일어난 턴에는 행동하지 않습니다. 스켈레톤에게 죽으면 변이하지 않습니다.",
            "<b>관</b>: 뱀파이어는 관 속에서 시작해 달이 " + Rules.MoonWake + "이 되면 깨어납니다. 잠든 동안은 공격할 수 없습니다.",
            "<b>달</b>: 적 턴이 시작될 때마다 1, 유닛이 하나 죽을 때마다(아군·적 모두) 1씩 찹니다. " + Rules.MoonMax + "이 되면 보름달입니다.",
            "<b>보름달 표적</b>: 다음 적 턴이 보름달이면, 아군 턴이 시작될 때 뱀파이어 " + Rules.EatRange + "칸 안·빛 밖에서 가장 가까운 아군이 표적으로 정해지고 바뀌지 않습니다. 그때 범위 안에 아무도 없으면 표적은 정해지지 않고, 보름달은 찬 채로 남아 뱀파이어가 평소처럼 다가옵니다.",
            "<b>보름달 흡혈</b>: 보름달 턴에 뱀파이어는 움직이지 않고, 표적이 여전히 " + Rules.EatRange + "칸 안·빛 밖에 있으면 먹어치웁니다. 표적이 범위 밖에 있거나, 빛 속에 있거나, 이미 죽었으면 실패하고, 뱀파이어는 대신 평소처럼 이동해 공격합니다. 어느 쪽이든 달은 다시 0이 됩니다.",
            "<b>아군 처치</b>: 공격 범위 안의 아군을 직접 처치하면 변이 없이 잠듭니다. 보름달 표적을 처치하면 흡혈도 실패합니다.",
            "<b>빛</b>: 화톳불 칸과 그 주변 칸, 햇빛이 드는 칸. 빛 속의 아군은 표적이 되지 않고, 쓰러져도 변이하지 않습니다.",
            "스폰은 원본의 체력·공격·방어·이동을 물려받고 근접 공격만 합니다. 기술 강화는 물려받지 않습니다.",
            "피해 = 공격 − 방어 (최소 1). 무작위 요소 없음. 반격·기회 공격 없음.",
        };

        static void RulesCard(Transform side, bool open, Action<bool> toggled)
        {
            var card = Ui.Card(side, "Rules");
            var summary = Ui.Button(card, null, () => toggled?.Invoke(!open), Look.Ghost, true);
            summary.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            Ui.Text(summary.transform, "규칙 요약", 0.95f, Ui.Bone, true);
            var list = Ui.Col(card, "List", 6);
            foreach (var line in RuleLines) Ui.Text(list, line, 0.85f, Ui.Bone);
            list.gameObject.SetActive(open);
        }

        public static void Over(RectTransform root, OverInfo o, BattleActions a)
        {
            var backdrop = Ui.Node(root, "Over");
            Ui.Stretch(backdrop);
            Ui.Bg(backdrop, Ui.Alpha(Ui.Deep, 0.82f));
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(() => a.CloseOver?.Invoke());
            var box = Ui.Col(backdrop, "OverBox", 8, 22, Ui.Panel, Ui.Line);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(440f, 0f);
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            box.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            var st = o.Stats;
            var stg = o.Stage;
            Ui.Text(box, o.Won ? "승리" : "패배", 1.6f, o.Won ? Ui.Candle : Ui.Blood, true, true);
            Ui.Text(box, o.Won ? o.Turn + "턴에 " + Korean.Josa(stg.Boss.Name, "을", "를") + " 쓰러뜨렸다." : "부대가 전멸했다. 이 땅은 밤의 것이 되었다.", 0.95f, Ui.Muted);
            var tally = Ui.Col(box, "Tally", 6);
            Pair(tally, "전사", st.Lost.ToString());
            Pair(tally, "직접 처치한 아군", st.Executed.ToString());
            Pair(tally, "스폰 발생", st.Risen.ToString());
            Pair(tally, "흡혈 당함", st.Drained.ToString());
            var ok = o.Won && o.BonusOk;
            if (o.Won && o.Camp)
            {
                var r = o.Result;
                var g1 = r.ClearGold;
                var g2 = r.BonusGold;
                var rew = Ui.Col(box, "Reward", 6);
                Pair(rew, r.FirstClear ? "첫 클리어 보상" : "클리어 보상 (재도전)", g1 > 0 ? "+" + g1 : "이미 획득");
                Pair(rew, "추가 목표 · " + stg.Bonus.Text, !ok ? "실패" : g2 > 0 ? "+" + g2 : "이미 획득", !ok);
                Pair(rew, "획득", g1 + g2 + " 금화", false, true);
                var nxt = Array.IndexOf(Campaign.Stages, stg) + 1;
                var next = nxt < Campaign.Stages.Length ? Campaign.Stages[nxt] : null;
                if (r.FirstClear && next != null)
                {
                    var news = next.Units.Where(u => GameData.Type(u).UnlockAt == nxt + 1).Select(u => GameData.Type(u).Name).ToList();
                    Ui.Text(box, next.Label + " 「" + next.Title + "」이 열렸습니다." + string.Join(",", news.Select(n => " " + n)) + (news.Count > 0 ? " 해금." : ""), 0.9f, Ui.Muted);
                }
                if (r.FirstClear && next == null) Ui.Text(box, "캠페인을 끝냈습니다.", 0.9f, Ui.Muted);
                var armory = GameData.Types.Where(t => t.UnlockAt == nxt).Select(t => t.Name).ToList();
                if (r.FirstClear && armory.Count > 0) Ui.Text(box, "병영에서 " + string.Join(", ", armory) + " 강화가 열렸습니다.", 0.9f, Ui.Muted);
            }
            else if (o.Won)
            {
                var rew = Ui.Col(box, "Reward", 6);
                Pair(rew, "추가 목표 · " + stg.Bonus.Text, ok ? "달성" : "실패", !ok);
            }
            var acts = Ui.Row(box, "Acts", 8);
            if (o.Camp) Ui.Button(acts, "캠페인으로", a.ToCamp, Look.Primary);
            Ui.Button(acts, "같은 편성으로 다시", a.Retry, o.Camp ? Look.Normal : Look.Primary);
            Ui.Button(acts, "편성부터 다시", a.Redeploy);
        }

        static void Pair(Transform parent, string left, string right, bool dim = false, bool strong = false)
        {
            var row = Ui.Row(parent, "Pair", 12);
            var color = dim ? Ui.Muted : Ui.Bone;
            Ui.Size(Ui.Text(row, left, 0.95f, color, strong), -1, -1, 1);
            Ui.Text(row, right, 0.95f, strong ? Ui.Candle : color, strong, false, TextAnchor.UpperRight).horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }
}
