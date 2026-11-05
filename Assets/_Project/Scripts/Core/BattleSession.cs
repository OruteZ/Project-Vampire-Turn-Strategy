using System.Collections.Generic;
using System.Linq;

namespace VampireSpawn.Core
{
    public enum SelectMode { Act, Execute }

    // One per prototype overlay class: ov-deploy, ov-threat, ov-move, ov-atk, ov-exec, ov-sel, ov-markzone, ov-mark.
    public enum Mark { Deploy, Threat, Move, Atk, Exec, Sel, MarkZone, Target }

    public readonly struct MarkedHex
    {
        public readonly Mark Mark;
        public readonly Hex Hex;
        public MarkedHex(Mark mark, Hex hex) { Mark = mark; Hex = hex; }
    }

    public sealed class MoonMark
    {
        public Hex Vampire;
        public string VampireLabel;
        public Hex? Target;
        public string TargetLabel;
    }

    // The prototype's click and selection state (S.selected, S.inspect, S.mode, S.deployType, S.showThreat) and onHex.
    public sealed class BattleSession
    {
        public readonly Battle Battle;
        public int? Selected { get; private set; }
        public int? InspectUnit { get; private set; }
        public int? InspectCorpse { get; private set; }
        public SelectMode Mode { get; private set; }
        public UnitKind DeployType { get; private set; }
        public bool ShowThreat { get => showThreat; set { showThreat = value; Flash = null; } }
        bool showThreat;
        public string Flash { get; private set; }

        static readonly CommandResult Nothing = CommandResult.Done(new List<BattleEvent>());

        public BattleSession(Battle battle)
        {
            Battle = battle;
            DeployType = battle.Stage.Units[0];
        }

        public Unit SelectedUnit => Selected is int id ? Battle.Find(id) : null;

        public string Hint => Flash ?? HintText();

        public void ClearFlash() => Flash = null;

        void Inspect(Unit u) { InspectUnit = u?.Id; InspectCorpse = null; }

        CommandResult After(CommandResult r)
        {
            if (Selected is int id && Battle.Find(id) == null) Selected = null;
            foreach (var e in r.Events) if (e.Kind == BattleEventKind.Hint) Flash = e.Text;
            return r;
        }

        public CommandResult Click(Hex h)
        {
            Flash = null;
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
            Flash = null;
            Selected = null;
            Inspect(null);
            Mode = SelectMode.Act;
        }

        public void Pick(UnitKind kind) { Flash = null; DeployType = kind; }

        public void ToggleExecute() { Flash = null; Mode = Mode == SelectMode.Execute ? SelectMode.Act : SelectMode.Execute; }

        public CommandResult Wait()
        {
            Flash = null;
            var s = SelectedUnit;
            if (s == null) return Nothing;
            var r = Battle.Wait(s.Id);
            Selected = null;
            Mode = SelectMode.Act;
            return r;
        }

        public CommandResult Start()
        {
            Flash = null;
            var r = Battle.Start();
            if (r.Ok) Inspect(null);
            return r;
        }

        public CommandResult EndTurn()
        {
            Flash = null;
            if (Battle.Phase != Phase.Player) return Nothing;
            Selected = null;
            Inspect(null);
            Mode = SelectMode.Act;
            return After(Battle.EndTurn());
        }

        // A drop outside the board passes null; the prototype treats it like a drop outside the deploy zone.
        public CommandResult Drop(int id, Hex? to)
        {
            Flash = null;
            var r = After(Battle.Relocate(id, to ?? new Hex(-1, -1)));
            if (!r.Ok) return r;
            if (Battle.Find(id) == null) { if (InspectUnit == id) InspectUnit = null; }
            else Inspect(Battle.Find(id));
            return r;
        }

        public CommandResult Recall(int id)
        {
            Flash = null;
            var r = Battle.Recall(id);
            if (r.Ok) Inspect(null);
            return r;
        }

        public CommandResult ClearAllies()
        {
            Flash = null;
            var r = Battle.ClearAllies();
            if (r.Ok) Inspect(null);
            return r;
        }

        public CommandResult UseSample() { Flash = null; return Battle.UseSample(); }

        string HintText()
        {
            switch (Battle.Phase)
            {
                case Phase.Deploy: return GameData.Type(DeployType).Name + ": 배치 구역의 빈 칸을 눌러 배치합니다. 배치된 유닛은 끌어서 옮기거나 바꿉니다.";
                case Phase.Enemy: return "적이 행동하고 있습니다.";
                case Phase.Over: return "전투가 끝났습니다.";
            }
            var sel = SelectedUnit;
            if (sel == null) return "아군을 눌러 선택하세요. 적이나 시신을 누르면 정보와 위협 범위를 볼 수 있습니다.";
            if (Mode == SelectMode.Execute) return "흰 점선 고리의 아군을 누르면 처치합니다. 다른 곳을 누르면 취소합니다.";
            if (sel.Acted) return "이 유닛은 행동을 마쳤습니다.";
            return (sel.Moved ? "" : "밝은 칸으로 이동합니다. ") + "붉은 칸의 적을 누르면 공격합니다.";
        }

        public HashSet<Hex> Threat(IEnumerable<Unit> enemies)
        {
            var set = new HashSet<Hex>();
            foreach (var e in enemies)
            {
                if (e.JustRisen || e.Asleep) continue;
                foreach (var h in Battle.Reach(e).Keys)
                {
                    set.Add(h);
                    foreach (var n in Battle.Board.Neighbors(h)) if (Battle.Board.Walkable(n)) set.Add(n);
                }
            }
            return set;
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
            if (b.Phase != Phase.Over)
            {
                var ins = InspectUnit is int id ? b.Find(id) : null;
                HashSet<Hex> th = null;
                if (ShowThreat && b.Phase != Phase.Deploy) th = Threat(b.Enemies);
                else if (ins != null && ins.Side == Side.Enemy) th = Threat(new[] { ins });
                if (th != null) o.AddRange(th.Select(h => new MarkedHex(Mark.Threat, h)));
            }
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
                if (moon.Target is Hex t) o.Add(new MarkedHex(moon.TargetLabel == "보름달 표적" ? Mark.Target : Mark.Exec, t));
            }
            return o;
        }

        public MoonMark Moon()
        {
            var b = Battle;
            var v = b.Vampire;
            if (v == null || !b.MoonNext() || b.Phase != Phase.Player) return null;
            var m = new MoonMark { Vampire = v.Pos, VampireLabel = b.EatTarget.HasValue ? "다음 턴 보름달" : "보름달 · 굶주림" };
            var t = b.EatTarget is int id ? b.Find(id) : null;
            if (t != null)
            {
                var safe = Hex.Distance(v.Pos, t.Pos) > Rules.EatRange || b.Board.Lit(t.Pos);
                m.Target = t.Pos;
                m.TargetLabel = safe ? "표적 · 지금은 안전" : "보름달 표적";
            }
            return m;
        }
    }
}
