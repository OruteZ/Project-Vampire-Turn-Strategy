using System;
using System.Collections.Generic;
using System.Linq;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    public sealed class BattleActions
    {
        public Action Primary, Leave, Clear, Sample, Exec, Mend, Wait, Retry, Redeploy, ToCamp, CloseOver, ToggleRules, ToggleLog;
        public Action<string> Pick;
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
        public static string Subtitle(Stage s, bool demo) => demo ? Loc.T("battle.demo") : Loc.T("battle.campaignSub", s.Label);

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

        // ui-style.md 14-1-6: the guide line stays only where it is the sole feedback for a mode (placing units, execute).
        public static bool ShowsHint(BattleSession s, bool enemyPlaying) => !enemyPlaying && (s.Battle.Phase == Phase.Deploy || s.Mode != SelectMode.Act);

        public static string Hint(BattleSession s, bool enemyPlaying) => Loc.Text(enemyPlaying ? new Message("hint.enemyActing") : s.Hint);

        public static string SkillName(Ability a) => Loc.T("skill." + char.ToLowerInvariant(a.ToString()[0]) + a.ToString().Substring(1));

        public static string Skills(Unit o) => o.Abil.Count == 0 ? null : Loc.T("unit.card.skills", string.Join(", ", o.Abil.Select(SkillName)));

        public static string Note(Unit o)
        {
            switch (o.Role)
            {
                case Role.Ally: return Loc.Text(GameData.Type(o.Type).Desc);
                case Role.Minion: return Loc.Text(GameData.Minion(o.Minion).Note);
                case Role.Vampire: return Loc.T("unit.card.vampireNote", Rules.MoonWake, Rules.MoonMax, Rules.EatRange, o.Heal);
            }
            return Loc.T("unit.card.spawnNote", o.Origin);
        }

        public static string[] RuleLines => new[]
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
    }
}
