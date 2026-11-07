using System.Linq;

namespace VampireSpawn.Core
{
    public enum CampLayer { Map, Info, Barracks }

    public enum CampKey { Up, Down, Left, Right, Enter, Esc, Barracks, PickStage, EmptyMap, Close, Reset, Buy, Tab, Tick }

    public enum CampDo { None, Redraw, Buy, Reset, March, Menu }

    public enum TabState { Hidden, Locked, Open }

    public enum CampConfirm { None, Reset, Buy }

    public readonly struct CampInput
    {
        public readonly CampKey Key;
        public readonly int Index;
        public readonly string Id;
        public readonly float Now;

        CampInput(CampKey key, int index = -1, string id = null, float now = 0f) { Key = key; Index = index; Id = id; Now = now; }

        public static CampInput Of(CampKey key, float now) => new CampInput(key, now: now);
        public static CampInput Pick(int stage, float now) => new CampInput(CampKey.PickStage, stage, now: now);
        public static CampInput Tab(int tab, float now) => new CampInput(CampKey.Tab, tab, now: now);
        public static CampInput Buy(string upgrade, float now) => new CampInput(CampKey.Buy, id: upgrade, now: now);
    }

    // docs/ui/stage-select.md 5 and docs/ui/upgrade.md 4·6: the camp screen's layers as one value. Barracks sits over Map or Info,
    // so closing it needs no memory of where it came from. Only one two-press confirm is armed at a time.
    public readonly struct CampNav
    {
        public const float ConfirmSeconds = 2.5f;

        public readonly int Stage, Tab;
        public readonly bool Info, Barracks;
        public readonly CampConfirm Armed;
        public readonly string ArmedId;
        public readonly float ArmedUntil;

        CampNav(int stage, bool info, bool barracks, int tab, CampConfirm armed = CampConfirm.None, string armedId = null, float armedUntil = 0f)
        {
            Stage = stage; Info = info; Barracks = barracks; Tab = tab; Armed = armed; ArmedId = armedId; ArmedUntil = armedUntil;
        }

        public CampLayer Layer => Barracks ? CampLayer.Barracks : Info ? CampLayer.Info : CampLayer.Map;
        public bool ResetArmed => Armed == CampConfirm.Reset;
        public string BuyArmed => Armed == CampConfirm.Buy ? ArmedId : null;

        static int Last => Campaign.Stages.Length - 1;

        // 5-3 1: the stage picked last this run, else the first open stage not yet cleared, else the last.
        public static CampNav FromMenu(Campaign c, CampNav? previous)
        {
            if (previous is CampNav p) return new CampNav(p.Stage, false, false, p.Tab);
            return new CampNav(DefaultStage(c), false, false, -1);
        }

        // 5-3 2: a first clear that opened a new stage shows the map on it; anything else reopens that stage's info.
        public static CampNav FromBattle(Campaign c, int stage, bool firstClear, int tab)
        {
            var opened = firstClear && stage < Last && c.StageOpen(stage + 1);
            return opened ? new CampNav(stage + 1, false, false, tab) : new CampNav(stage, true, false, tab);
        }

        public static CampNav AfterReset() => new CampNav(0, false, false, -1);

        public static int DefaultStage(Campaign c)
        {
            for (var i = 0; i < Campaign.Stages.Length; i++) if (c.StageOpen(i) && !c.IsCleared(Campaign.Stages[i])) return i;
            return Last;
        }

        public static TabState TabOf(Campaign c, UnitKind kind)
        {
            if (c.UpgradesOpen(kind)) return TabState.Open;
            var at = GameData.Type(kind).UnlockAt - 1;
            return at >= 0 && at <= Last && c.StageOpen(at) ? TabState.Locked : TabState.Hidden;
        }

        public static int Affordable(Campaign c, UnitKind? kind = null) =>
            GameData.Upgrades.Count(u => (kind == null || u.Unit == kind) && c.UpgradesOpen(u.Unit) && !c.Upgrades.Contains(u.Id) && u.Cost <= c.Gold);

        // upgrade.md 4-5: the tab picked last this run, else the first with something to buy, else the first open, else the first.
        public static int DefaultTab(Campaign c, int previous)
        {
            var kinds = GameData.Types.Select(t => t.Kind).ToArray();
            if (previous >= 0 && previous < kinds.Length && TabOf(c, kinds[previous]) != TabState.Hidden) return previous;
            for (var i = 0; i < kinds.Length; i++) if (Affordable(c, kinds[i]) > 0) return i;
            for (var i = 0; i < kinds.Length; i++) if (TabOf(c, kinds[i]) == TabState.Open) return i;
            return 0;
        }

        CampNav With(int? stage = null, bool? info = null, bool? barracks = null, int? tab = null) =>
            new CampNav(stage ?? Stage, info ?? Info, barracks ?? Barracks, tab ?? Tab);

        CampNav Arm(CampConfirm kind, string id, float now) => new CampNav(Stage, Info, Barracks, Tab, kind, id, now + ConfirmSeconds);

        public (CampNav next, CampDo act) Step(Campaign c, CampInput input)
        {
            if (input.Key == CampKey.Tick)
                return Armed != CampConfirm.None && input.Now > ArmedUntil ? (With(), CampDo.Redraw) : (this, CampDo.None);
            // 5-1: any other input drops a pending confirm, except the second press that completes it.
            var calm = With();
            var wasArmed = Armed != CampConfirm.None;
            switch (input.Key)
            {
                case CampKey.Up:
                case CampKey.Down:
                    if (Barracks) return Unchanged(calm, wasArmed);
                    var to = System.Math.Max(0, System.Math.Min(Last, Stage + (input.Key == CampKey.Up ? 1 : -1)));
                    return to == Stage ? Unchanged(calm, wasArmed) : (calm.With(stage: to), CampDo.Redraw);
                case CampKey.Left:
                case CampKey.Right:
                    if (!Barracks) return Unchanged(calm, wasArmed);
                    return calm.StepTab(c, input.Key == CampKey.Right ? 1 : -1, wasArmed);
                case CampKey.Tab:
                    if (!Barracks || input.Index < 0 || input.Index >= GameData.Types.Length || TabOf(c, GameData.Types[input.Index].Kind) == TabState.Hidden)
                        return Unchanged(calm, wasArmed);
                    return (calm.With(tab: input.Index), CampDo.Redraw);
                case CampKey.Enter:
                    if (Barracks) return Unchanged(calm, wasArmed);
                    if (!Info) return (calm.With(info: true), CampDo.Redraw);
                    return c.StageOpen(Stage) ? (calm, CampDo.March) : Unchanged(calm, wasArmed);
                case CampKey.Esc:
                    if (Barracks) return (calm.With(barracks: false), CampDo.Redraw);
                    if (Info) return (calm.With(info: false), CampDo.Redraw);
                    return (calm, CampDo.Menu);
                case CampKey.Close:
                    if (Barracks) return (calm.With(barracks: false), CampDo.Redraw);
                    if (Info) return (calm.With(info: false), CampDo.Redraw);
                    return Unchanged(calm, wasArmed);
                case CampKey.Barracks:
                    return Barracks ? (calm.With(barracks: false), CampDo.Redraw) : (calm.With(barracks: true, tab: DefaultTab(c, Tab)), CampDo.Redraw);
                case CampKey.PickStage:
                    if (Barracks || input.Index < 0 || input.Index > Last) return Unchanged(calm, wasArmed);
                    return (calm.With(stage: input.Index, info: true), CampDo.Redraw);
                case CampKey.EmptyMap:
                    return Info && !Barracks ? (calm.With(info: false), CampDo.Redraw) : Unchanged(calm, wasArmed);
                case CampKey.Reset:
                    if (Layer != CampLayer.Map) return Unchanged(calm, wasArmed);
                    return ResetArmed ? (calm, CampDo.Reset) : (Arm(CampConfirm.Reset, null, input.Now), CampDo.Redraw);
                case CampKey.Buy:
                    if (!Barracks || input.Id == null) return Unchanged(calm, wasArmed);
                    return BuyArmed == input.Id ? (calm, CampDo.Buy) : (Arm(CampConfirm.Buy, input.Id, input.Now), CampDo.Redraw);
            }
            return Unchanged(calm, wasArmed);
        }

        static (CampNav, CampDo) Unchanged(CampNav calm, bool wasArmed) => (calm, wasArmed ? CampDo.Redraw : CampDo.None);

        (CampNav, CampDo) StepTab(Campaign c, int dir, bool wasArmed)
        {
            var n = GameData.Types.Length;
            for (var i = Tab + dir; i >= 0 && i < n; i += dir)
                if (TabOf(c, GameData.Types[i].Kind) != TabState.Hidden) return (With(tab: i), CampDo.Redraw);
            return Unchanged(this, wasArmed);
        }
    }
}
