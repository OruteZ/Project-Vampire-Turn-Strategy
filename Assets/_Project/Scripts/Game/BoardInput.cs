using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    public sealed class BoardInput : MonoBehaviour
    {
        public BattleView battle;
        public OverlayView overlay;
        public Camera viewCamera;

        // Prototype pointermove threshold (px) before a press on a deployed unit becomes a drag; flashHint lasts 1800 ms.
        const float DragPixels = 6f, FlashSeconds = 1.8f;

        public BattleSession Session { get; private set; }
        public event Action Changed;
        public Func<bool> PointerBlocked = () => false;
        public Func<bool> EscapeFirst = () => false;

        class Drag { public int Id; public Vector2 Start; public bool Moved; public Hex? Target; }
        Drag drag;
        float flashUntil;
        bool wasBusy, wasDown, escDown;

        BoardView Board => battle.board;

        public void Begin(BattleSession session)
        {
            Session = session;
            drag = null;
            battle.Begin(session.Battle);
            Redraw();
        }

        public void End()
        {
            Session = null;
            drag = null;
            overlay.Clear();
        }

        public CommandResult Run(Func<BattleSession, CommandResult> command)
        {
            var selected = Session.Selected;
            var allies = Session.Battle.Allies.Count();
            var r = battle.Run(_ => command(Session));
            if (!r.Ok) Sound.Play(s => s.denied);
            else if (Session.Battle.Phase == Phase.Deploy && Session.Battle.Allies.Count() > allies) Sound.Play(s => s.place);
            else if (Session.Selected != null && Session.Selected != selected) Sound.Play(s => s.select);
            Redraw();
            return r;
        }

        public void Act(Action<BattleSession> action)
        {
            action(Session);
            Redraw();
        }

        public void Redraw()
        {
            if (Session == null) return;
            overlay.Show(Session, drag != null && drag.Moved ? drag.Target : null);
            if (!battle.IsBusy) battle.ShowDone();
            flashUntil = !Session.Flash.IsEmpty ? Time.unscaledTime + FlashSeconds : 0f;
            Changed?.Invoke();
        }

        void Update()
        {
            if (Session == null) return;
            if (wasBusy && !battle.IsBusy) Redraw();
            wasBusy = battle.IsBusy;
            if (flashUntil > 0f && Time.unscaledTime > flashUntil)
            {
                Session.ClearFlash();
                Redraw();
            }
            var kb = Keyboard.current;
            var esc = kb != null && kb.escapeKey.isPressed;
            if (esc && !escDown && !battle.IsBusy && !EscapeFirst()) { Session.Escape(); Redraw(); }
            escDown = esc;
            var m = Mouse.current;
            if (m == null) return;
            var pos = m.position.ReadValue();
            // Edges come from isPressed: in the background-ticked Editor, wasPressedThisFrame can belong to an Editor input update.
            var down = m.leftButton.isPressed;
            if (down && !wasDown) Press(pos);
            else if (down && drag != null) DragTo(pos);
            if (!down && wasDown) Release(pos);
            wasDown = down;
        }

        void Press(Vector2 pos)
        {
            if (battle.IsBusy || PointerBlocked() || Session.Battle.Phase != Phase.Deploy) return;
            var h = HexAt(pos);
            var u = h is Hex at ? Session.Battle.UnitAt(at) : null;
            if (u != null && u.Side == Side.Ally) drag = new Drag { Id = u.Id, Start = pos };
        }

        void DragTo(Vector2 pos)
        {
            if (!drag.Moved && Vector2.Distance(pos, drag.Start) < DragPixels) return;
            drag.Moved = true;
            drag.Target = HexAt(pos);
            var view = battle.ViewOf(drag.Id);
            if (view != null && GroundAt(pos) is Vector3 w) view.transform.position = w + Vector3.up * 0.3f;
            overlay.Show(Session, drag.Target);
        }

        void Release(Vector2 pos)
        {
            var d = drag;
            drag = null;
            if (d != null && d.Moved)
            {
                Run(s => s.Drop(d.Id, d.Target));
                battle.SyncDeploy();
                return;
            }
            if (battle.IsBusy || PointerBlocked()) return;
            if (HexAt(pos) is Hex h) Run(s => s.Click(h));
        }

        Vector3? GroundAt(Vector2 screen)
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            var ray = cam.ScreenPointToRay(screen);
            // Pick on the drawn hologram (battle-terrain.md 4): its floor plane, or ground that rises above it and so is hit first.
            var floor = new Plane(Vector3.up, new Vector3(0f, Board.HologramFloor, 0f));
            var onFloor = floor.Raycast(ray, out var t) ? t : float.MaxValue;
            var terrain = Board.Ground != null ? Board.Ground.GetComponent<TerrainCollider>() : null;
            if (terrain != null && terrain.Raycast(ray, out var hit, 1000f) && hit.distance < onFloor) return hit.point;
            return onFloor < float.MaxValue ? ray.GetPoint(onFloor) : (Vector3?)null;
        }

        // Prototype hexAt: nearest centre, and only if the point is within one corner radius of it.
        public Hex? HexAt(Vector2 screen)
        {
            if (!(GroundAt(screen) is Vector3 p) || Board.Board == null) return null;
            Hex? best = null;
            var bd = float.MaxValue;
            for (var r = 0; r < Board.Board.Rows; r++)
                for (var c = 0; c < Board.Board.Cols; c++)
                {
                    var h = new Hex(c, r);
                    var w = Board.HexToWorld(h);
                    var d = (w.x - p.x) * (w.x - p.x) + (w.z - p.z) * (w.z - p.z);
                    if (d < bd) { bd = d; best = h; }
                }
            var size = Board.art.hexSize;
            return bd <= size * size ? best : null;
        }

        public Vector2 ScreenOf(Hex h)
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            return cam.WorldToScreenPoint(Board.Stand(h));
        }
    }
}
