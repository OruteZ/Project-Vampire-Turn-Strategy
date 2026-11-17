using System.Collections.Generic;

namespace VampireSpawn.Core
{
    public enum BattleEventKind { Log, Banner, Walk, Popup, Hit, Hint, Died, CorpseLeft, Rose, Woke, HpChanged, Buried, MoonChanged, Arrived, Opened, Shifted }

    public enum BannerKind { PlayerTurn, EnemyTurn, Coffin, FullMoon, Reinforce }

    // Feed is the vampire's own heal after a drain, kept apart from a Mend heal so the view can tell them apart.
    public enum PopupKind { Damage, Heal, Feed, Execute, Bury, Drain, DrainFailed }

    public readonly struct BattleEvent
    {
        public readonly BattleEventKind Kind;
        public readonly int UnitId, OtherId, Value;
        public readonly Message Text;
        public readonly string Style;
        public readonly IReadOnlyList<Hex> Path;
        public readonly Hex At;

        public BannerKind BannerKind => (BannerKind)Value;
        public PopupKind PopupKind => (PopupKind)Value;

        BattleEvent(BattleEventKind kind, int unitId = 0, Message text = default, string style = null, IReadOnlyList<Hex> path = null,
            int otherId = 0, int value = 0, Hex at = default)
        {
            Kind = kind; UnitId = unitId; Text = text; Style = style; Path = path; OtherId = otherId; Value = value; At = at;
        }

        public static BattleEvent Log(Message text, string style) => new BattleEvent(BattleEventKind.Log, text: text, style: style);
        public static BattleEvent Banner(BannerKind kind, Message text) =>
            new BattleEvent(BattleEventKind.Banner, text: text, style: kind == BannerKind.PlayerTurn ? "player" : "enemy", value: (int)kind);
        public static BattleEvent Walk(int unitId, IReadOnlyList<Hex> path, Hex from) => new BattleEvent(BattleEventKind.Walk, unitId, path: path, at: from);
        public static BattleEvent Popup(int unitId, PopupKind kind, Message text) =>
            new BattleEvent(BattleEventKind.Popup, unitId, text, kind == PopupKind.Damage ? "dmg" : kind == PopupKind.Drain ? "blood" : "heal", value: (int)kind);
        public static BattleEvent Hit(int unitId, int attackerId, int damage = 0) => new BattleEvent(BattleEventKind.Hit, unitId, otherId: attackerId, value: damage);
        public static BattleEvent Hint(Message text) => new BattleEvent(BattleEventKind.Hint, text: text);
        public static BattleEvent Died(int unitId, Hex at) => new BattleEvent(BattleEventKind.Died, unitId, at: at);
        public static BattleEvent CorpseLeft(int corpseId, Hex at) => new BattleEvent(BattleEventKind.CorpseLeft, corpseId, at: at);
        public static BattleEvent Rose(int unitId, int corpseId, Hex at) => new BattleEvent(BattleEventKind.Rose, unitId, otherId: corpseId, at: at);
        public static BattleEvent Woke(int unitId) => new BattleEvent(BattleEventKind.Woke, unitId);
        public static BattleEvent HpChanged(int unitId, int hp) => new BattleEvent(BattleEventKind.HpChanged, unitId, value: hp);
        public static BattleEvent Buried(int corpseId, Hex at) => new BattleEvent(BattleEventKind.Buried, corpseId, at: at);
        public static BattleEvent Opened(Hex at) => new BattleEvent(BattleEventKind.Opened, at: at);
        public static BattleEvent Arrived(int unitId, Hex at) => new BattleEvent(BattleEventKind.Arrived, unitId, at: at);
        public static BattleEvent Shifted(int unitId, bool wolf) => new BattleEvent(BattleEventKind.Shifted, unitId, value: wolf ? 1 : 0);
        public static BattleEvent MoonChanged(int moon) => new BattleEvent(BattleEventKind.MoonChanged, value: moon);

        // docs/art/battle-camera.md 4-1: how many walks from start can play at once (consecutive, no shared hex); 0 if start is not a walk.
        public static int WalkRun(IReadOnlyList<BattleEvent> events, int start)
        {
            var used = new HashSet<Hex>();
            var n = 0;
            for (var i = start; i < events.Count && events[i].Kind == BattleEventKind.Walk; i++, n++)
            {
                var hexes = new List<Hex>(events[i].Path) { events[i].At };
                if (n > 0 && hexes.Exists(used.Contains)) break;
                used.UnionWith(hexes);
            }
            return n;
        }

        // docs/art/battle-camera.md 4-2: walks play before the attack events they can skip; a walk tangled with them stays and walls the rest.
        public static List<BattleEvent> MovesFirst(IReadOnlyList<BattleEvent> events)
        {
            var result = new List<BattleEvent>(events.Count);
            var pinned = new HashSet<int>();
            var at = 0;
            for (var i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.Kind != BattleEventKind.Walk)
                {
                    result.Add(e);
                    if (!Skippable(e.Kind) || pinned.Contains(e.UnitId)) at = result.Count;
                    continue;
                }
                if (CanSkip(events, i, result, at)) result.Insert(at++, e);
                else { result.Add(e); pinned.Add(e.UnitId); at = result.Count; }
            }
            return result;
        }

        static bool Skippable(BattleEventKind k) => k is BattleEventKind.Log or BattleEventKind.Hint or BattleEventKind.Hit
            or BattleEventKind.HpChanged or BattleEventKind.Popup or BattleEventKind.Died or BattleEventKind.CorpseLeft;

        static bool CanSkip(IReadOnlyList<BattleEvent> events, int i, List<BattleEvent> result, int at)
        {
            var w = events[i];
            for (var j = i + 1; j < events.Count; j++)
            {
                if (events[j].Kind == BattleEventKind.Log) continue;
                if (events[j].Kind != BattleEventKind.Walk && events[j].UnitId == w.UnitId) return false;
                break;
            }
            var hexes = new HashSet<Hex>(w.Path) { w.At };
            for (var j = at; j < result.Count; j++)
            {
                var s = result[j];
                if (s.UnitId == w.UnitId || (s.Kind == BattleEventKind.Hit && s.OtherId == w.UnitId)) return false;
                if ((s.Kind == BattleEventKind.Died || s.Kind == BattleEventKind.CorpseLeft) && hexes.Contains(s.At)) return false;
            }
            return true;
        }

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
            BattleEventKind.Buried => "buried|" + UnitId + "|" + At,
            BattleEventKind.MoonChanged => "moon|" + Value,
            BattleEventKind.Arrived => "arrived|" + UnitId + "|" + At,
            BattleEventKind.Opened => "opened|" + At,
            BattleEventKind.Shifted => "shift|" + UnitId + "|" + Value,
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
