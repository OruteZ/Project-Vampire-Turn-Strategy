using System.Collections.Generic;

namespace VampireSpawn.Core
{
    public enum BattleEventKind { Log, Banner, Walk, Popup, Hit, Hint, Died, CorpseLeft, Rose, Woke, HpChanged }

    public readonly struct BattleEvent
    {
        public readonly BattleEventKind Kind;
        public readonly int UnitId, OtherId, Value;
        public readonly Message Text;
        public readonly string Style;
        public readonly IReadOnlyList<Hex> Path;
        public readonly Hex At;

        BattleEvent(BattleEventKind kind, int unitId = 0, Message text = default, string style = null, IReadOnlyList<Hex> path = null,
            int otherId = 0, int value = 0, Hex at = default)
        {
            Kind = kind; UnitId = unitId; Text = text; Style = style; Path = path; OtherId = otherId; Value = value; At = at;
        }

        public static BattleEvent Log(Message text, string style) => new BattleEvent(BattleEventKind.Log, text: text, style: style);
        public static BattleEvent Banner(Message text, string style) => new BattleEvent(BattleEventKind.Banner, text: text, style: style);
        public static BattleEvent Walk(int unitId, IReadOnlyList<Hex> path) => new BattleEvent(BattleEventKind.Walk, unitId, path: path);
        public static BattleEvent Popup(int unitId, Message text, string style) => new BattleEvent(BattleEventKind.Popup, unitId, text, style);
        public static BattleEvent Hit(int unitId, int attackerId, int damage = 0) => new BattleEvent(BattleEventKind.Hit, unitId, otherId: attackerId, value: damage);
        public static BattleEvent Hint(Message text) => new BattleEvent(BattleEventKind.Hint, text: text);
        public static BattleEvent Died(int unitId, Hex at) => new BattleEvent(BattleEventKind.Died, unitId, at: at);
        public static BattleEvent CorpseLeft(int corpseId, Hex at) => new BattleEvent(BattleEventKind.CorpseLeft, corpseId, at: at);
        public static BattleEvent Rose(int unitId, int corpseId, Hex at) => new BattleEvent(BattleEventKind.Rose, unitId, otherId: corpseId, at: at);
        public static BattleEvent Woke(int unitId) => new BattleEvent(BattleEventKind.Woke, unitId);
        public static BattleEvent HpChanged(int unitId, int hp) => new BattleEvent(BattleEventKind.HpChanged, unitId, value: hp);

        // Kinds the prototype emits through log/showBanner/walk/popup/hitFx/flashHint; the rest exist for the view and have no HTML twin.
        public bool InPrototype => Kind <= BattleEventKind.Hint;

        public override string ToString() => Format(m => m.ToString());

        public string Format(System.Func<Message, string> text) => Kind switch
        {
            BattleEventKind.Log => "log|" + text(Text) + "|" + Style,
            BattleEventKind.Banner => "banner|" + text(Text) + "|" + Style,
            BattleEventKind.Walk => "walk|" + UnitId + "|" + string.Join(" ", Path),
            BattleEventKind.Popup => "popup|" + UnitId + "|" + text(Text) + "|" + Style,
            BattleEventKind.Hit => "hit|" + UnitId,
            BattleEventKind.Hint => "hint|" + text(Text),
            BattleEventKind.Died => "died|" + UnitId + "|" + At,
            BattleEventKind.CorpseLeft => "corpse|" + UnitId + "|" + At,
            BattleEventKind.Rose => "rose|" + UnitId + "|" + OtherId + "|" + At,
            BattleEventKind.Woke => "woke|" + UnitId,
            _ => "hp|" + UnitId + "|" + Value,
        };
    }

    public readonly struct CommandResult
    {
        public readonly bool Ok;
        public readonly Message Reason;
        public readonly IReadOnlyList<BattleEvent> Events;

        CommandResult(bool ok, Message reason, IReadOnlyList<BattleEvent> events)
        {
            Ok = ok; Reason = reason; Events = events;
        }

        public static CommandResult Done(List<BattleEvent> events) => new CommandResult(true, default, events.ToArray());
        public static CommandResult Rejected(Message reason) => new CommandResult(false, reason, System.Array.Empty<BattleEvent>());
    }
}
