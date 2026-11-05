using System.Collections.Generic;

namespace VampireSpawn.Core
{
    public enum BattleEventKind { Log, Banner, Walk, Popup, Hit }

    public readonly struct BattleEvent
    {
        public readonly BattleEventKind Kind;
        public readonly int UnitId;
        public readonly string Text, Style;
        public readonly IReadOnlyList<Hex> Path;

        BattleEvent(BattleEventKind kind, int unitId, string text, string style, IReadOnlyList<Hex> path)
        {
            Kind = kind; UnitId = unitId; Text = text; Style = style; Path = path;
        }

        public static BattleEvent Log(string text, string style) => new BattleEvent(BattleEventKind.Log, 0, text, style, null);
        public static BattleEvent Banner(string text, string style) => new BattleEvent(BattleEventKind.Banner, 0, text, style, null);
        public static BattleEvent Walk(int unitId, IReadOnlyList<Hex> path) => new BattleEvent(BattleEventKind.Walk, unitId, null, null, path);
        public static BattleEvent Popup(int unitId, string text, string style) => new BattleEvent(BattleEventKind.Popup, unitId, text, style, null);
        public static BattleEvent Hit(int unitId) => new BattleEvent(BattleEventKind.Hit, unitId, null, null, null);

        public override string ToString() => Kind switch
        {
            BattleEventKind.Log => "log|" + Text + "|" + Style,
            BattleEventKind.Banner => "banner|" + Text + "|" + Style,
            BattleEventKind.Walk => "walk|" + UnitId + "|" + string.Join(" ", Path),
            BattleEventKind.Popup => "popup|" + UnitId + "|" + Text + "|" + Style,
            _ => "hit|" + UnitId,
        };
    }

    public readonly struct CommandResult
    {
        public readonly bool Ok;
        public readonly string Reason;
        public readonly IReadOnlyList<BattleEvent> Events;

        CommandResult(bool ok, string reason, IReadOnlyList<BattleEvent> events)
        {
            Ok = ok; Reason = reason; Events = events;
        }

        public static CommandResult Done(List<BattleEvent> events) => new CommandResult(true, null, events.ToArray());
        public static CommandResult Rejected(string reason) => new CommandResult(false, reason, System.Array.Empty<BattleEvent>());
    }
}
