using UnityEngine;
using UnityEngine.UIElements;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Game.Screens
{
    // The Camp scene: docs/ui/stage-select.md, three layers over the baked expedition map (docs/art/campaign-map.md).
    public sealed class CampFlow : MonoBehaviour
    {
        [Tooltip("게임이 쓰는 데이터. 비었거나 문제가 있으면 HTML 기본값으로 돈다")] public GameDataAsset data;
        public CampaignMapView map;

        CampNav nav;
        CampToolkit ui;
        CampMarkers markers;
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

        void Redraw(bool snap)
        {
            var empty = EmptyArea(nav.Layer);
            var size = ToolkitKit.PanelSize();
            ui.Fill(Campaign, nav, In);
            markers.Fill(Campaign, nav, empty);
            map.Look(nav.Layer, nav.Stage, empty, new Vector2(48f / size.x, 48f / size.y), snap);
            var tab = Mathf.Clamp(nav.Tab, 0, GameData.Types.Length - 1);
            map.ShowUnit(nav.Layer == CampLayer.Barracks && CampNav.TabOf(Campaign, GameData.Types[tab].Key) != TabState.Hidden ? tab : -1);
        }

        // stage-select.md 2-3: the screen minus the corner UI and, in L2/L3, the 560 px side panel; normalized viewport units.
        Rect EmptyArea(CampLayer layer)
        {
            var size = ToolkitKit.PanelSize();
            var right = layer == CampLayer.Map ? 24f : 608f;
            return new Rect(24f / size.x, 104f / size.y, (size.x - 24f - right) / size.x, (size.y - 112f - 104f) / size.y);
        }

        bool PointerOverUi() => ToolkitKit.PointerOverUi(ui?.Doc);

        // stage-select.md 5-2 keys, the 2.5 s confirm expiry, and a click on bare map (5-1) all go through CampNav.
        void Update()
        {
            if (ui == null) return;
            if (nav.Armed != CampConfirm.None) Send(In(CampKey.Tick));
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && keys.Pressed("click", mouse.leftButton.isPressed) && !PointerOverUi()) Send(In(CampKey.EmptyMap));
            if (kb == null) return;
            if (keys.Pressed("up", kb.upArrowKey.isPressed)) Send(In(CampKey.Up));
            if (keys.Pressed("down", kb.downArrowKey.isPressed)) Send(In(CampKey.Down));
            if (keys.Pressed("left", kb.leftArrowKey.isPressed)) Send(In(CampKey.Left));
            if (keys.Pressed("right", kb.rightArrowKey.isPressed)) Send(In(CampKey.Right));
            if (keys.Pressed("enter", kb.enterKey.isPressed || kb.numpadEnterKey.isPressed)) Send(In(CampKey.Enter));
            if (keys.Pressed("esc", kb.escapeKey.isPressed)) Send(In(CampKey.Esc));
            if (keys.Pressed("b", kb.bKey.isPressed)) Send(In(CampKey.Barracks));
        }
    }
}
