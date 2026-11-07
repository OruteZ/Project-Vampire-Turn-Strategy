using System.Collections.Generic;
using System.Linq;

namespace VampireSpawn.Core
{
    public enum SelectMode { Act, Execute }

    // One per prototype overlay class: ov-deploy, ov-threat, ov-move, ov-atk, ov-exec, ov-sel, ov-markzone, ov-mark.
    public enum Mark { Deploy, Move, Atk, Exec, Sel, MarkZone, Target }

    public readonly struct MarkedHex
    {
        public readonly Mark Mark;
        public readonly Hex Hex;
        public MarkedHex(Mark mark, Hex hex) { Mark = mark; Hex = hex; }
    }

    public sealed class MoonMark
    {
        public Hex Vampire;
        public Message VampireLabel;
        public Hex? Target;
        public Message TargetLabel;
    }

    // The prototype's click and selection state (S.selected, S.inspect, S.mode, S.deployType) and onHex. Threat cells are gone: docs/ui/battle-hud.md 1-4.
    public sealed class BattleSession
    {
        public readonly Battle Battle;
        public int? Selected { get; private set; }
        public int? InspectUnit { get; private set; }
        public int? InspectCorpse { get; private set; }
        public SelectMode Mode { get; private set; }
        public UnitKind DeployType { get; private set; }
        public Message Flash { get; private set; }

        static readonly CommandResult Nothing = CommandResult.Done(new List<BattleEvent>());

        public BattleSession(Battle battle)
        {
            Battle = battle;
            DeployType = battle.Stage.Units[0];
        }

        public Unit SelectedUnit => Selected is int id ? Battle.Find(id) : null;

        public Message Hint => Flash.IsEmpty ? HintText() : Flash;

        public void ClearFlash() => Flash = default;

        void Inspect(Unit u) { InspectUnit = u?.Id; InspectCorpse = null; }

        CommandResult After(CommandResult r)
        {
            if (Selected is int id && Battle.Find(id) == null) Selected = null;
            foreach (var e in r.Events) if (e.Kind == BattleEventKind.Hint) Flash = e.Text;
            return r;
        }

        public CommandResult Click(Hex h)
        {
            Flash = default;
            if (Battle.Phase == Phase.Enemy || Battle.Phase == Phase.Over) return Nothing;
            var u = Battle.UnitAt(h);
            var co = Battle.CorpseAt(h);
            if (Battle.Phase == Phase.Deploy)
            {
                if (u != null) { Inspect(u); return Nothing; }
                if (!Battle.InDeploy(h)) return Nothing;
                var placed = Battle.Place(DeployType, h);
                if (!placed.Ok) { Flash = placed.Reason; return placed; }
                Inspect(Battle.UnitAt(h));
                return placed;
            }
            var sel = SelectedUnit;
            if (sel != null && !sel.Acted)
            {
                if (Mode == SelectMode.Execute)
                {
                    if (u != null && u.Side == Side.Ally && u != sel && Hex.Distance(sel.Pos, u.Pos) <= sel.Rng) return After(Battle.Execute(sel.Id, u.Id));
                    Mode = SelectMode.Act;
                    return Nothing;
                }
                if (u != null && u.Side == Side.Enemy && !u.Asleep && Hex.Distance(sel.Pos, u.Pos) <= sel.Rng) return After(Battle.Attack(sel.Id, u.Id));
                if (!sel.Moved && u == null && co == null && Battle.Reach(sel).ContainsKey(h)) return After(Battle.Move(sel.Id, h));
            }
            if (u != null && u.Side == Side.Ally) { Selected = u.Acted ? (int?)null : u.Id; Inspect(u); Mode = SelectMode.Act; }
            else if (u != null) Inspect(u);
            else if (co != null) { InspectUnit = null; InspectCorpse = co.Id; }
            else { Selected = null; Inspect(null); Mode = SelectMode.Act; }
            return Nothing;
        }

        public void Escape()
        {
            if (Battle.Phase != Phase.Player) return;
            Flash = default;
            Selected = null;
            Inspect(null);
            Mode = SelectMode.Act;
        }

        public void Pick(UnitKind kind) { Flash = default; DeployType = kind; }

        public void ToggleExecute() { Flash = default; Mode = Mode == SelectMode.Execute ? SelectMode.Act : SelectMode.Execute; }

        public CommandResult Wait()
        {
            Flash = default;
            var s = SelectedUnit;
            if (s == null) return Nothing;
            var r = Battle.Wait(s.Id);
            Selected = null;
            Mode = SelectMode.Act;
            return r;
        }

        public CommandResult Start()
        {
            Flash = default;
            var r = Battle.Start();
            if (r.Ok) Inspect(null);
            return r;
        }

        public CommandResult EndTurn()
        {
            Flash = default;
            if (Battle.Phase != Phase.Player) return Nothing;
            Selected = null;
            Inspect(null);
            Mode = SelectMode.Act;
            return After(Battle.EndTurn());
        }

        // A drop outside the board passes null; the prototype treats it like a drop outside the deploy zone.
        public CommandResult Drop(int id, Hex? to)
        {
            Flash = default;
            var r = After(Battle.Relocate(id, to ?? new Hex(-1, -1)));
            if (!r.Ok) return r;
            if (Battle.Find(id) == null) { if (InspectUnit == id) InspectUnit = null; }
            else Inspect(Battle.Find(id));
            return r;
        }

        public CommandResult Recall(int id)
        {
            Flash = default;
            var r = Battle.Recall(id);
            if (r.Ok) Inspect(null);
            return r;
        }

        public CommandResult ClearAllies()
        {
            Flash = default;
            var r = Battle.ClearAllies();
            if (r.Ok) Inspect(null);
            return r;
        }

        public CommandResult UseSample() { Flash = default; return Battle.UseSample(); }

        Message HintText()
        {
            switch (Battle.Phase)
            {
                case Phase.Deploy: return new Message("hint.deploy", GameData.Type(DeployType).Name);
                case Phase.Enemy: return new Message("hint.enemyActing");
                case Phase.Over: return new Message("hint.over");
            }
            var sel = SelectedUnit;
            if (sel == null) return new Message("hint.select");
            if (Mode == SelectMode.Execute) return new Message("hint.execute");
            if (sel.Acted) return new Message("hint.acted");
            return new Message(sel.Moved ? "hint.attack" : "hint.moveAttack");
        }

        IEnumerable<Hex> Cells()
        {
            for (var r = 0; r < Battle.Board.Rows; r++)
                for (var c = 0; c < Battle.Board.Cols; c++) yield return new Hex(c, r);
        }

        public List<MarkedHex> Overlay()
        {
            var o = new List<MarkedHex>();
            var b = Battle;
            if (b.Phase == Phase.Deploy) o.AddRange(Cells().Where(b.InDeploy).Select(h => new MarkedHex(Mark.Deploy, h)));
            var sel = SelectedUnit;
            if (sel != null && b.Phase == Phase.Player)
            {
                if (!sel.Moved && !sel.Acted)
                    o.AddRange(b.Reach(sel).Keys.Where(h => h != sel.Pos).Select(h => new MarkedHex(Mark.Move, h)));
                if (!sel.Acted)
                {
                    if (Mode == SelectMode.Execute)
                        o.AddRange(b.Allies.Where(a => a != sel && Hex.Distance(sel.Pos, a.Pos) <= sel.Rng).Select(a => new MarkedHex(Mark.Exec, a.Pos)));
                    else
                        o.AddRange(b.Enemies.Where(e => Hex.Distance(sel.Pos, e.Pos) <= sel.Rng).Select(e => new MarkedHex(Mark.Atk, e.Pos)));
                }
                o.Add(new MarkedHex(Mark.Sel, sel.Pos));
            }
            var moon = Moon();
            if (moon != null)
            {
                var v = b.Vampire;
                o.AddRange(Cells().Where(h => b.Board.Walkable(h) && Hex.Distance(v.Pos, h) <= Rules.EatRange && h != v.Pos).Select(h => new MarkedHex(Mark.MarkZone, h)));
                if (moon.Target is Hex t) o.Add(new MarkedHex(moon.TargetLabel.Key == "moon.target" ? Mark.Target : Mark.Exec, t));
            }
            return o;
        }

        public MoonMark Moon()
        {
            var b = Battle;
            var v = b.Vampire;
            if (v == null || !b.MoonNext() || b.Phase != Phase.Player) return null;
            var m = new MoonMark { Vampire = v.Pos, VampireLabel = new Message(b.EatTarget.HasValue ? "moon.nextTurn" : "moon.starving") };
            var t = b.EatTarget is int id ? b.Find(id) : null;
            if (t != null)
            {
                var safe = Hex.Distance(v.Pos, t.Pos) > Rules.EatRange || b.Board.Lit(t.Pos);
                m.Target = t.Pos;
                m.TargetLabel = new Message(safe ? "moon.safe" : "moon.target");
            }
            return m;
        }
    }
}
