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

        public static string Subtitle(Stage s, bool demo) => demo ? Loc.T("battle.demo") : Loc.T("battle.campaignSub", s.Label);

        public static string Title(Stage s) => Loc.Text(s.Label) + " · " + Rich(Ui.Blood, Loc.Text(s.Title));

        public static string PrimaryLabel(Battle b, bool enemyPlaying, out bool enabled)
        {
            enabled = true;
            if (enemyPlaying) { enabled = false; return Loc.T("battle.enemyActing"); }
            switch (b.Phase)
            {
                case Phase.Deploy: enabled = b.Allies.Any(); return Loc.T("battle.start");
                case Phase.Player: return Loc.T("battle.endTurn");
                case Phase.Enemy: enabled = false; return Loc.T("battle.enemyActing");
            }
            return Loc.T("battle.results");
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
            Toggle(ctrl, Loc.T("battle.threat"), s.ShowThreat, a.Threat);
            Ui.Button(ctrl, leaveArmed ? Loc.T("battle.leaveConfirm") : Loc.T("battle.leave"), a.Leave, Look.Ghost, true);
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
                Stat(parent, b.Used(t => t.Pop) + "/" + b.Stage.Pop, Loc.T("battle.pop"));
                if (b.Stage.Silver > 0) Stat(parent, b.Used(t => t.Silver) + "/" + b.Stage.Silver, Loc.T("battle.silver"));
                Stat(parent, b.Allies.Count().ToString(), Loc.T("battle.deployed"));
                return;
            }
            var hot = b.MoonNext();
            var v = b.Vampire;
            Stat(parent, b.Moon + "/" + Rules.MoonMax, hot ? Loc.T("moon.nextTurn") : v != null && v.Asleep ? Loc.T("battle.coffinAt", Rules.MoonWake) : Loc.T("battle.moon"), hot);
            Stat(parent, b.Turn.ToString(), Loc.T("battle.turn"));
            Stat(parent, b.Allies.Count().ToString(), Loc.T("battle.allies"));
            Stat(parent, b.Enemies.Count(e => e.Role != Role.Spawn).ToString(), Loc.T("battle.enemies"));
            Stat(parent, b.Corpses.Count.ToString(), Loc.T("battle.rising"), b.Corpses.Count > 0);
            Stat(parent, b.Stats.Risen.ToString(), Loc.T("battle.risen"), b.Stats.Risen > 0);
        }

        static void Toggle(Transform parent, string label, bool on, Action<bool> changed)
        {
            var b = Ui.Button(parent, null, () => changed?.Invoke(!on), Look.Ghost, true);
            var box = Ui.Node(b.transform, "Box");
            Ui.Bg(box, on ? Ui.Candle : Ui.Panel2, Ui.Muted, false);
            Ui.Size(box, 14, 14);
            Ui.Text(b.transform, label, 0.85f, Ui.Bone).horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        public static string Hint(BattleSession s, bool enemyPlaying) => Loc.Text(enemyPlaying ? new Message("hint.enemyActing") : s.Hint);

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

        static readonly Dictionary<Ability, string> SkillKey = new Dictionary<Ability, string>
        {
            [Ability.Pike] = "skill.pike", [Ability.Ambush] = "skill.ambush", [Ability.Cleave] = "skill.cleave", [Ability.Charge] = "skill.charge",
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
                H2(brief, Loc.T("battle.brief"));
                Ui.Text(brief, Loc.Text(stg.Brief), 0.9f, Ui.Muted);
                var obj = Ui.Col(brief, "Objectives", 4);
                Def(obj, Loc.T("battle.objective"), Loc.T("battle.defeatBoss", stg.BossName));
                Def(obj, Loc.T("battle.bonus"), Loc.Text(stg.Bonus.Text));
                Def(obj, Loc.T("battle.resources"), CampaignScreen.Resources(stg));
                Def(obj, Loc.T("battle.enemy"), Loc.T("battle.enemyList", stg.Enemies.Length - 1, stg.BossName));

                var roster = Ui.Card(side, "Roster");
                H2(roster, Loc.T("battle.formation"));
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
                    Ui.Size(Ui.Text(top, Loc.Text(t.Name), 0.95f, Ui.Bone, true), -1, -1, 1);
                    Ui.Text(top, CampaignScreen.Resources(t.Pop, t.Silver), 0.78f, can ? Ui.Muted : Ui.Blood).horizontalOverflow = HorizontalWrapMode.Overflow;
                    Ui.Text(col, CampaignScreen.StatLine(Battle.Preview(k, upgrades)), 0.72f, Ui.Muted);
                }
                Ui.Text(roster, Loc.T("battle.deployHelp"), 0.8f, Ui.Muted);
                var acts = Ui.Row(roster, "Acts", 8);
                Ui.Button(acts, Loc.T("battle.recallAll"), a.Clear, Look.Normal, true);
                Ui.Button(acts, Loc.T("battle.sample"), a.Sample, Look.Normal, true);
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
                    H2(card, Loc.T("battle.allyPhase"));
                    Ui.Text(card, Loc.T("battle.readyAllies", b.Allies.Count(u => !u.Acted)), 0.9f, Ui.Muted);
                }
                var log = Ui.Card(side, "Log");
                H2(log, Loc.T("battle.log"));
                var list = Ui.Col(log, "Lines", 3);
                foreach (var l in b.Log)
                {
                    var t = Ui.Text(list, Loc.Text(l.Text), 0.82f, LogColor.TryGetValue(l.Style, out var c) ? c : Ui.Bone, l.Style == "t");
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
            RoleTag(head, Loc.T("unit.card.corpse"), Ui.Spawn);
            Ui.Text(head, Loc.T("unit.card.corpseOf", c.Name), 1f, Ui.Bone, true, true);
            Flag(card, Loc.T("unit.card.corpseRise"), Ui.Spawn);
            Ui.Text(card, Loc.T("unit.card.corpseStats", c.MaxHp, c.Atk, c.Mov), 0.88f, Ui.Muted);
        }

        public static void UnitCard(Transform side, BattleSession s, Unit o, BattleActions a)
        {
            var b = s.Battle;
            var ally = o.Side == Side.Ally;
            var card = Ui.Card(side, "Unit " + o.Id);
            var head = Ui.Row(card, "Head", 8);
            if (ally) RoleTag(head, Loc.T("unit.card.ally"), Ui.Steel);
            else if (o.Role == Core.Role.Vampire) RoleTag(head, Loc.T("unit.card.boss"), Ui.Blood);
            else if (o.Role == Core.Role.Spawn) RoleTag(head, Loc.T("unit.card.spawn"), Ui.Spawn);
            else RoleTag(head, Loc.T("unit.card.minion"), Ui.Muted);
            Ui.Text(head, Loc.Text(o.Name), 1f, Ui.Bone, true, true);
            HpBar(card, (float)o.Hp / o.MaxHp, ally ? Ui.Steel : o.Role == Core.Role.Spawn ? Ui.Spawn : Ui.Blood);
            var grid = Ui.Row(card, "Stats", 4);
            grid.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            foreach (var (v, l) in new[] { (o.Hp + "/" + o.MaxHp, Loc.T("unit.card.hp")), (o.Atk + (o.Bane > 0 ? "+" + o.Bane : ""), Loc.T("unit.card.atk")), (o.Def.ToString(), Loc.T("unit.card.def")), (o.Mov.ToString(), Loc.T("unit.card.mov")), (o.Rng.ToString(), Loc.T("unit.card.rng")) })
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
                Flag(card, Loc.T("unit.card.target", Rules.EatRange) + (safe ? " " + Loc.T("unit.card.safeNow") : ""), Ui.Blood);
            }
            if (ally && b.Board.Lit(o.Pos)) Flag(card, Loc.T("unit.card.inLight"), Ui.Candle);
            if (o.Role == Core.Role.Vampire)
            {
                if (o.Asleep) Flag(card, Loc.T("unit.card.coffin", Rules.MoonWake, b.Moon), Ui.Candle);
                else if (b.MoonNext()) Flag(card, Loc.T("unit.card.moonNext"), Ui.Blood);
                else Flag(card, Loc.T("unit.card.moon", b.Moon, Rules.MoonMax), Ui.Candle);
            }
            if (o.JustRisen) Flag(card, Loc.T("unit.card.justRisen"), Ui.Spawn);
            Ui.Text(card, Note(o), 0.88f, Ui.Muted);
            if (ally && o.Abil.Count > 0) Ui.Text(card, Loc.T("unit.card.skills", string.Join(", ", o.Abil.Select(x => Loc.T(SkillKey[x])))), 0.88f, Ui.Muted);
            if (ally && b.Phase == Phase.Deploy)
            {
                var acts = Ui.Row(card, "Acts", 8);
                var id = o.Id;
                Ui.Button(acts, Loc.T("unit.card.recall"), () => a.Recall?.Invoke(id), Look.Normal, true);
            }
            if (ally && b.Phase == Phase.Player && s.Selected == o.Id)
            {
                if (o.Acted) Ui.Text(card, Loc.T("unit.card.acted"), 0.88f, Ui.Muted);
                else
                {
                    var acts = Ui.Row(card, "Acts", 8);
                    var exec = s.Mode == SelectMode.Execute;
                    Ui.Button(acts, exec ? Loc.T("unit.card.cancelExec") : Loc.T("unit.card.exec"), a.Exec, exec ? Look.DangerOn : Look.Danger, true);
                    Ui.Button(acts, Loc.T("unit.card.wait"), a.Wait, Look.Normal, true);
                }
            }
        }

        static string Note(Unit o)
        {
            switch (o.Role)
            {
                case Core.Role.Ally: return Loc.Text(GameData.Type(o.Type.Value).Desc);
                case Core.Role.Skeleton: return Loc.T("unit.card.skeletonNote");
                case Core.Role.Vampire:
                    return Loc.T("unit.card.vampireNote", Rules.MoonWake, Rules.MoonMax, Rules.EatRange, o.Heal);
            }
            return Loc.T("unit.card.spawnNote", o.Origin);
        }

        static string[] RuleLines => new[]
        {
            Loc.T("battle.rule.corpse"),
            Loc.T("battle.rule.coffin", Rules.MoonWake),
            Loc.T("battle.rule.moon", Rules.MoonMax),
            Loc.T("battle.rule.target", Rules.EatRange),
            Loc.T("battle.rule.drain", Rules.EatRange),
            Loc.T("battle.rule.exec"),
            Loc.T("battle.rule.light"),
            Loc.T("battle.rule.spawn"),
            Loc.T("battle.rule.damage"),
        };

        static void RulesCard(Transform side, bool open, Action<bool> toggled)
        {
            var card = Ui.Card(side, "Rules");
            var summary = Ui.Button(card, null, () => toggled?.Invoke(!open), Look.Ghost, true);
            summary.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            Ui.Text(summary.transform, Loc.T("battle.rules"), 0.95f, Ui.Bone, true);
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
            Ui.Text(box, o.Won ? Loc.T("over.won") : Loc.T("over.lost"), 1.6f, o.Won ? Ui.Candle : Ui.Blood, true, true);
            Ui.Text(box, o.Won ? Loc.T("over.wonLine", o.Turn, stg.BossName) : Loc.T("over.lostLine"), 0.95f, Ui.Muted);
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
                    Ui.Text(box, Loc.T("over.nextOpen", next.Label, next.Title) + (news.Count > 0 ? " " + Loc.T("over.unitsUnlocked", string.Join(", ", news)) : ""), 0.9f, Ui.Muted);
                }
                if (r.FirstClear && next == null) Ui.Text(box, Loc.T("over.campaignDone"), 0.9f, Ui.Muted);
                var armory = GameData.Types.Where(t => t.UnlockAt == nxt).Select(t => Loc.Text(t.Name)).ToList();
                if (r.FirstClear && armory.Count > 0) Ui.Text(box, Loc.T("over.armoryOpen", string.Join(", ", armory)), 0.9f, Ui.Muted);
            }
            else if (o.Won)
            {
                var rew = Ui.Col(box, "Reward", 6);
                Pair(rew, Loc.T("over.bonus", stg.Bonus.Text), ok ? Loc.T("over.done") : Loc.T("over.failed"), !ok);
            }
            var acts = Ui.Row(box, "Acts", 8);
            if (o.Camp) Ui.Button(acts, Loc.T("over.toCamp"), a.ToCamp, Look.Primary);
            Ui.Button(acts, Loc.T("over.retry"), a.Retry, o.Camp ? Look.Normal : Look.Primary);
            Ui.Button(acts, Loc.T("over.redeploy"), a.Redeploy);
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
