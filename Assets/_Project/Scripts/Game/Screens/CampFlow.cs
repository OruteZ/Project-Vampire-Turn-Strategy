using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Game.Screens
{
    // The Camp scene: docs/ui/stage-select.md, three layers over the baked expedition map (docs/art/campaign-map.md).
    public sealed class CampFlow : MonoBehaviour
    {
        [Tooltip("게임이 쓰는 데이터. 비었거나 문제가 있으면 콘솔에 오류가 뜬다")] public GameDataAsset data;
        public CampaignMapView map;

        CampNav nav;
        CampToolkit ui;
        CampMarkers markers;
        UnitDetail detail;
        readonly KeyEdges keys = new KeyEdges();

        public CampNav Nav => nav;

        Campaign Campaign => App.Campaign;

        void Start()
        {
            App.Ensure(data);
            Sound.Use(map.art.sound);
            Sound.Music(s => s.campMusic);
            Sound.Ambience(s => s.campAmbience);
            nav = App.CampStart ?? CampNav.FromMenu(Campaign, App.Nav);
            App.CampStart = null;
            map.Show(Campaign);
            ui = CampToolkit.Build(transform, map.art, In, Send);
            ToolkitKit.Shade(ui.Root.Q("shade"));
            detail = new UnitDetail(transform, map.art);
            ui.Detail = ShowDetail;
            markers = CampMarkers.Build(transform, ui.Root.Q("markers"), map.art, map, i => Send(CampInput.Pick(i, Time.unscaledTime)));
            Redraw(true);
            Fx.FadeIn(ui.Root);
        }

        CampInput In(CampKey key) => CampInput.Of(key, Time.unscaledTime);

        public void Send(CampInput input)
        {
            var asked = nav.BuyArmed;
            var (next, act) = nav.Step(Campaign, input);
            nav = next;
            App.Nav = nav;
            switch (act)
            {
                case CampDo.Redraw: Redraw(false); break;
                case CampDo.Buy:
                    if (asked != null && Campaign.Buy(asked)) { CampaignStore.Save(Campaign); Sound.Play(s => s.purchase); }
                    Redraw(false);
                    break;
                case CampDo.Reset:
                    App.ResetCampaign();
                    nav = CampNav.AfterReset();
                    map.Show(Campaign);
                    Redraw(true);
                    break;
                case CampDo.March: Sound.Play(s => s.march); App.GoBattle(Campaign.Stages[nav.Stage], demo: false); break;
                case CampDo.Menu: App.GoMenu(); break;
            }
        }

        CampLayer? drawn;

        void Redraw(bool snap)
        {
            // upgrade.md 9-5: the barracks is another place, so the camera cuts and a black cover clears in 0.2 s.
            var studio = drawn.HasValue && drawn != nav.Layer && (drawn == CampLayer.Barracks || nav.Layer == CampLayer.Barracks);
            drawn = nav.Layer;
            if (studio) { snap = true; Fx.FadeIn(ui.Root, 0.2f); }
            var empty = EmptyArea(nav.Layer);
            var size = ToolkitKit.PanelSize();
            ui.Fill(Campaign, nav, In);
            markers.Fill(Campaign, nav, empty);
            map.Look(nav.Layer, nav.Stage, empty, new Vector2(48f / size.x, 48f / size.y), snap);
            var tab = Mathf.Clamp(nav.Tab, 0, GameData.Types.Length - 1);
            map.ShowUnit(nav.Layer == CampLayer.Barracks && CampNav.TabOf(Campaign, GameData.Types[tab].Key) != TabState.Hidden ? tab : -1);
        }

        // stage-select.md 2-3: the screen minus the corner UI and the side panel (760 in L2, 560 in L3); normalized viewport units.
        Rect EmptyArea(CampLayer layer)
        {
            var size = ToolkitKit.PanelSize();
            var right = layer == CampLayer.Map ? 24f : layer == CampLayer.Info ? 808f : 608f;
            return new Rect(24f / size.x, 104f / size.y, (size.x - 24f - right) / size.x, (size.y - 112f - 104f) / size.y);
        }

        bool PointerOverUi() => ToolkitKit.PointerOverUi(ui?.Doc);

        // unit-detail.md 2, 12: from the stage roster the arrows walk that stage's units; from the barracks they walk the shown tabs and move the tab along.
        public void ShowDetail(string kind)
        {
            if (kind == null || nav.BuyArmed != null) return;
            var owned = Campaign.Upgrades;
            if (nav.Layer == CampLayer.Info)
            {
                var units = Campaign.Stages[nav.Stage].Units;
                var at = System.Array.IndexOf(units, kind);
                if (at >= 0) detail.Browse(units, at, k => DetailModel.OfType(k, owned));
            }
            else if (nav.Layer == CampLayer.Barracks)
            {
                var tabs = Enumerable.Range(0, GameData.Types.Length).Where(i => CampNav.TabOf(Campaign, GameData.Types[i].Key) != TabState.Hidden).ToList();
                var at = tabs.FindIndex(i => GameData.Types[i].Key == kind);
                if (at >= 0) detail.Browse(tabs.Select(i => GameData.Types[i].Key).ToList(), at, k => DetailModel.OfType(k, owned), n => Send(CampInput.Tab(tabs[n], Time.unscaledTime)));
            }
        }

        bool held, turning;
        float heldX;

        // upgrade.md 9-6: in the barracks a drag that starts off the UI turns the unit, 0.4 degrees per 1920-wide screen px.
        void Turn(UnityEngine.InputSystem.Mouse mouse)
        {
            var down = mouse != null && mouse.leftButton.isPressed && nav.Layer == CampLayer.Barracks && nav.BuyArmed == null;
            var x = mouse != null ? mouse.position.ReadValue().x : 0f;
            if (down && !held) turning = !PointerOverUi();
            else if (down && turning) map.TurnUnit((heldX - x) * 0.4f * 1920f / Screen.width);
            if (!down) turning = false;
            held = down;
            heldX = x;
        }

        // stage-select.md 5-2 keys, the 2.5 s confirm expiry, and a click on bare map (5-1) all go through CampNav.
        void Update()
        {
            if (ui == null) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var info = kb != null && keys.Pressed("i", kb.iKey.isPressed);
            if (UnitDetail.IsOpen)
            {
                if (kb == null) return;
                if (info || keys.Pressed("esc", kb.escapeKey.isPressed)) detail.Close();
                else detail.Step((keys.Pressed("right", kb.rightArrowKey.isPressed) ? 1 : 0) - (keys.Pressed("left", kb.leftArrowKey.isPressed) ? 1 : 0));
                return;
            }
            if (info) ShowDetail(nav.Layer == CampLayer.Barracks ? GameData.Types[Mathf.Clamp(nav.Tab, 0, GameData.Types.Length - 1)].Key : PointerOverUi() ? ui.HoverKind : null);
            if (nav.Armed != CampConfirm.None) Send(In(CampKey.Tick));
            var mouse = UnityEngine.InputSystem.Mouse.current;
            Turn(mouse);
            if (mouse != null && keys.Pressed("click", mouse.leftButton.isPressed) && !PointerOverUi()) Send(In(CampKey.EmptyMap));
            if (kb == null) return;
            if (keys.Pressed("up", kb.upArrowKey.isPressed)) Send(In(CampKey.Up));
            if (keys.Pressed("down", kb.downArrowKey.isPressed)) Send(In(CampKey.Down));
            if (keys.Pressed("left", kb.leftArrowKey.isPressed)) Send(In(CampKey.Left));
            if (keys.Pressed("right", kb.rightArrowKey.isPressed)) Send(In(CampKey.Right));
            if (keys.Pressed("enter", kb.enterKey.isPressed || kb.numpadEnterKey.isPressed)) Send(In(CampKey.Enter));
            if (keys.Pressed("esc", kb.escapeKey.isPressed)) Send(In(CampKey.Esc));
            if (keys.Pressed("b", kb.bKey.isPressed)) Send(In(CampKey.Barracks));
            if (keys.Pressed("tab", kb.tabKey.isPressed)) Send(In(CampKey.Face));
        }
    }
}
