using System;
using System.Collections.Generic;
using System.Linq;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    public sealed class BattleActions
    {
        public Action Primary, Leave, Restart, Clear, Sample, Exec, Mend, Trap, Wait, Undo, Aim, Retry, Redeploy, ToCamp, CloseOver, ToggleRules, ToggleLog, NextCard;
        public Action<string> Pick, RecallKind;
        public Action<int> Recall, Detail;
        public Action<string> DetailKind;
    }

    public enum Arm { None, Leave, Restart }

    // battle-hud.md 6: leave and restart act on a second press inside the window; arming one disarms the other.
    public sealed class TwoPress
    {
        float until;
        public Arm Armed { get; private set; }

        public bool Press(Arm which, float now, float seconds)
        {
            if (Armed == which && now <= until) { Armed = Arm.None; return true; }
            Armed = which;
            until = now + seconds;
            return false;
        }

        public bool Expire(float now)
        {
            if (Armed == Arm.None || now <= until) return false;
            Armed = Arm.None;
            return true;
        }
    }

    public sealed class OverInfo
    {
        // Prototype finish(): a campaign win pays out and is saved by the caller; a demo win only reports the bonus.
        public static OverInfo From(Battle b, bool demo, Campaign c)
        {
            var won = b.Won == true;
            var ok = won && b.BonusMet;
            var result = won && !demo ? c.Finish(b.Stage, true, ok) : default;
            return new OverInfo { Won = won, Camp = !demo, Stage = b.Stage, Line = b.EndLine, Stats = b.Stats, BonusOk = ok, Result = result };
        }

        public bool Won, Camp;
        public Stage Stage;
        public Message Line;
        public BattleStats Stats;
        public bool BonusOk;
        public StageResult Result;
    }

    // docs/ui/battle-hud.md 3-4: one action bar slot; Key 0 is a passive skill that is only shown.
    public sealed class ActionSlot
    {
        public int Key;
        // Shown instead of Key when the slot has a letter key (undo is Z).
        public string Hotkey;
        public string Icon, Label;
        public UnityEngine.Color Color;
        public Action Act;
        public bool Ready, On;
        // Uses left in this battle ("2/3"), for slots with a per-battle allowance.
        public string Count;
    }

    public enum WaveWhen { Soon, Turn, Reach, Kills }

    // docs/systems/waves.md 4, docs/art/ui-style.md 8-7: when one wave of reinforcements comes, as its cell badge and the brief's row show it.
    public sealed class Wave
    {
        // What "the same wave" means: one arrival turn, or one trigger.
        public string Key;
        public WaveWhen When;
        // Turn: ally turns left before it comes, this one included. Kills: Done of Need minions.
        public int Left, Need, Done;
        public readonly List<Placement> Who = new List<Placement>();

        // Soon, then the fewest turns left, then conditions.
        public int Order => When == WaveWhen.Turn ? 1000 + Left : (int)When * 100000;
    }

    public static class BattleScreen
    {
        public static Wave When(Battle b, string trigger, int arrive = 0)
        {
            if (trigger == null) return Turns(b, "turn " + arrive, arrive);
            var key = "wave " + trigger;
            if (b.Fired(trigger)) return new Wave { Key = key, When = WaveWhen.Soon, Left = 1 };
            var t = b.Stage.Triggers.FirstOrDefault(x => x.Id == trigger);
            if (t != null && t.Kind == TriggerKind.Turn) return Turns(b, key, t.Value);
            if (t != null && t.Kind == TriggerKind.Kills) return new Wave { Key = key, When = WaveWhen.Kills, Need = t.Value, Done = Math.Min(b.MinionsKilled, t.Value) };
            return new Wave { Key = key, When = WaveWhen.Reach };
        }

        // The enemy phase of turn n brings it, so on turn t the allies still move n - t + 1 times; a blocked arrival stays at 1.
        static Wave Turns(Battle b, string key, int turn)
        {
            var left = Math.Max(1, turn - b.Turn + 1);
            return new Wave { Key = key, When = left == 1 ? WaveWhen.Soon : WaveWhen.Turn, Left = left };
        }

        // Every enemy reinforcement not on the board yet, one entry per wave.
        public static List<Wave> Waves(Battle b)
        {
            var list = new List<Wave>();
            foreach (var p in b.Pending)
            {
                var w = When(b, p.Wave, p.Arrive);
                var same = list.FirstOrDefault(x => x.Key == w.Key);
                if (same == null) list.Add(same = w);
                same.Who.Add(p);
            }
            return list.OrderBy(w => w.Order).ToList();
        }

        // docs/systems/triggers.md 1-5.
        public static string WhenText(Wave w, bool ally = false)
        {
            switch (w.When)
            {
                case WaveWhen.Soon: return Loc.T(ally ? "trigger.soonAlly" : "trigger.soon");
                case WaveWhen.Turn: return Loc.T("battle.turnsLeft", w.Left);
                case WaveWhen.Kills: return Loc.T("trigger.kills", w.Need);
            }
            return Loc.T("trigger.reach");
        }

        public static string WaveTip(Wave w) => WhenText(w) + " · " + string.Join(" · ",
            GameData.Minions.Select(m => (m, n: w.Who.Count(p => p.Kind == m.Key))).Where(x => x.n > 0).Select(x => Loc.Text(x.m.Name) + " " + x.n));

        public static List<ActionSlot> Slots(BattleSession s, BattleActions a)
        {
            var list = new List<ActionSlot>();
            var u = s.SelectedUnit;
            if (u == null || u.Acted || s.Battle.Phase != Phase.Player) return list;
            var marks = s.Overlay();
            var exec = s.Mode == SelectMode.Execute;
            var mend = s.Mode == SelectMode.Mend;
            var trap = s.Mode == SelectMode.Trap;
            void Add(int key, string icon, UnityEngine.Color c, string label, Action act, bool ready, bool on = false) =>
                list.Add(new ActionSlot { Key = key, Icon = icon, Color = c, Label = label, Act = act, Ready = ready, On = on });
            Add(1, Kit.Icon.Mov, Ui.Steel, Loc.T("hud.act.move"), a.Aim, !u.Moved);
            Add(2, Kit.Icon.Atk, Ui.Steel, Loc.T("hud.act.attack"), a.Aim, marks.Any(m => m.Mark == Mark.Atk));
            Add(3, Kit.Icon.Wait, Ui.Steel, Loc.T("unit.card.wait"), a.Wait, true);
            if (u.Role != Role.Escort) Add(4, Kit.Icon.Dead, Ui.Blood, Loc.T(exec ? "unit.card.cancelExec" : "unit.card.exec"), a.Exec, true, exec);
            if (u.Has(Ability.Mend)) Add(5, Kit.Icon.Heal, Ui.Steel, Loc.T(mend ? "hud.cancelMend" : "hud.mend"), a.Mend, true, mend);
            else if (u.Has(Ability.Trap))
            {
                var left = s.Battle.TrapsLeft(u);
                Add(5, Kit.Icon.Target, Ui.Steel, Loc.T(trap ? "hud.cancelTrap" : "hud.trap"), a.Trap, left > 0, trap);
                list[list.Count - 1].Count = left + "/" + Battle.TrapsCarried(u);
            }
            else if (u.Has(Ability.Bury)) Add(5, Kit.Icon.Fang, Ui.Spawn, SkillName(Ability.Bury), a.Aim, marks.Any(m => m.Mark == Mark.Bury));
            list.Add(new ActionSlot { Key = -1, Hotkey = "Z", Icon = Kit.Icon.Undo, Color = Ui.Steel, Label = Loc.T("hud.act.undo"), Act = a.Undo, Ready = s.Battle.CanUndo });
            return list;
        }

        // battle-hud.md 3-4 8: skills that work by themselves are not actions; the unit block shows them as small rounds.
        public static IEnumerable<string> Passives(Unit u) =>
            u.Abil.Where(k => k != Ability.Mend && k != Ability.Bury && k != Ability.Trap && k != Ability.None).Distinct().Select(SkillName);

        public static string Subtitle(Stage s, bool demo) => demo ? Loc.T("battle.demo") : Loc.T("battle.campaignSub", s.Label);

        public static string PrimaryLabel(Battle b, bool enemyPlaying, out bool enabled, float speed = 1f)
        {
            enabled = true;
            if (enemyPlaying) { enabled = false; return Loc.T("hud.enemyActingSpeed", speed); }
            switch (b.Phase)
            {
                case Phase.Deploy:
                    enabled = b.Reinforcing || b.Allies.Any();
                    return Loc.T(b.Reinforcing ? "battle.reinforceDone" : "battle.start");
                case Phase.Player: return Loc.T("battle.endTurn");
                case Phase.Enemy: enabled = false; return Loc.T("hud.enemyActingSpeed", speed);
            }
            return Loc.T("battle.results");
        }

        // ui-style.md 14-1-6: the guide line stays only where it is the sole feedback for a mode (placing units, execute).
        public static bool ShowsHint(BattleSession s, bool enemyPlaying) => !enemyPlaying && (s.Battle.Phase == Phase.Deploy || s.Mode != SelectMode.Act);

        public static string Hint(BattleSession s, bool enemyPlaying) => Loc.Text(enemyPlaying ? new Message("hint.enemyActing") : s.Hint);

        public static string SkillName(Ability a) => Loc.T("skill." + char.ToLowerInvariant(a.ToString()[0]) + a.ToString().Substring(1));

        public static string Atk(Unit u) => u.Atk + (u.Bane > 0 ? "+" + u.Bane : "");

        public static string Skills(Unit o) => o.Abil.Count == 0 ? null : Loc.T("unit.card.skills", string.Join(", ", o.Abil.Select(SkillName)));

        public static string Note(Unit o)
        {
            switch (o.Role)
            {
                case Role.Ally: return Loc.Text(GameData.Type(o.Type).Desc);
                case Role.Minion: return Loc.Text(GameData.Minion(o.Minion).Note);
                case Role.Vampire: return Loc.T("unit.card.vampireNote", Rules.MoonWake, Rules.MoonMax, Rules.EatRange, o.Heal);
                case Role.Escort: return Loc.T("unit.card.escortNote");
            }
            return Loc.T("unit.card.spawnNote", o.Origin);
        }

        // battle-hud.md 6-1: the rules window as terms, the same name·colour·description shape as the detail window's states.
        public static List<DetailModel.State> RuleTerms => new List<DetailModel.State>
        {
            Term("turn", Kit.Icon.Fang, Ui.Spawn),
            Term("corpse", Kit.Icon.Skulls, Ui.Spawn),
            Term("coffin", Kit.Icon.Boss, Ui.Candle, Rules.MoonWake),
            Term("moon", Kit.Icon.Turn, Ui.Sun, Rules.MoonMax),
            Term("target", Kit.Icon.Target, Ui.Blood, Rules.EatRange),
            Term("drain", Kit.Icon.Hp, Ui.Blood, Rules.EatRange),
            Term("exec", Kit.Icon.Dead, Ui.Blood),
            Term("light", Kit.Icon.Star, Ui.Candle),
            Term("spawn", Kit.Icon.Pop, Ui.Spawn),
            Term("damage", Kit.Icon.Atk, Ui.Steel),
        };

        public sealed class RuleCard { public string Icon, Name, Line; public UnityEngine.Color Color; }

        // docs/ui/rule-cards.md 3: a card id is "<kind>:<key>" (Campaign.NewRules); rules reuse their term from the rules window.
        public static RuleCard Card(string id)
        {
            var at = id.IndexOf(':');
            var kind = id.Substring(0, at);
            var key = id.Substring(at + 1);
            switch (kind)
            {
                case "rule":
                    var t = RuleTerms.First(x => x.Id == key);
                    return new RuleCard { Icon = t.Icon, Color = t.Color, Name = t.Name, Line = t.Desc };
                case "unit":
                    return new RuleCard { Icon = key, Color = Ui.Steel, Name = Loc.Text(GameData.Type(key).Name), Line = Loc.T("card.unit." + key) };
                case "foe":
                    return new RuleCard { Icon = key, Color = Ui.Blood, Name = Loc.Text(GameData.Minion(key).Name), Line = Loc.T("card.foe." + key) };
            }
            return new RuleCard { Icon = Kit.Icon.Flag, Color = Ui.Steel, Name = Loc.T("card.goal." + key + ".name"), Line = Loc.T("card.goal." + key) };
        }

        public static DetailModel.State Term(string id, string icon, UnityEngine.Color c, params object[] args) =>
            new DetailModel.State { Id = id, Icon = icon, Color = c, Name = Loc.T("rules.term." + id), Desc = Loc.T("rules.term." + id + ".desc", args) };
    }
}
