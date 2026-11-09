using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Game.Screens
{
    // The Battle scene: deploy, turns and the result window (docs/ui/battle-hud.md). Play started in this scene runs the demo stage.
    public sealed class BattleFlow : MonoBehaviour
    {
        public BoardInput input;
        [Tooltip("게임이 쓰는 데이터. 비었거나 문제가 있으면 HTML 기본값으로 돈다")] public GameDataAsset data;
        public Camera viewCamera;

        // Prototype timers: leave confirmation 2500 ms, phase banner 1300 ms.
        const float LeaveSeconds = 2.5f, BannerSeconds = 1.3f;

        public bool OverShown => overRoot != null && overRoot.gameObject.activeSelf;

        bool demo, leaveArm, rulesOpen, logOpen, finished, enemyPlaying, fullMoon, moonFading, altDown;
        float leaveUntil, bannerUntil;
        Vector2Int fittedSize;
        Stage stage;
        OverInfo over;
        RectTransform screen, boardArea, overRoot, hudRoot;
        BattleHud hud;
        int? hoverId, hoverCorpse;
        Text banner;
        BattleActions actions;
        readonly KeyEdges keys = new KeyEdges();

        BattleSession Session => input.Session;
        BattleView View => input.battle;
        Campaign Campaign => App.Campaign;

        void Start()
        {
            App.Ensure(data);
            screen = App.Canvas(transform);
            if (viewCamera != null)
            {
                viewCamera.clearFlags = CameraClearFlags.SolidColor;
                viewCamera.backgroundColor = Ui.Ink;
            }
            input.Changed += OnBattleChanged;
            View.Played += OnPlayed;
            input.PointerBlocked = () => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            input.EscapeFirst = CloseBattlePopup;
            actions = new BattleActions
            {
                Primary = Primary,
                Leave = Leave,
                Clear = () => input.Run(s => s.ClearAllies()),
                Sample = () => input.Run(s => s.UseSample()),
                Pick = k => input.Act(s => s.Pick(k)),
                Recall = id => input.Run(s => s.Recall(id)),
                Exec = () => input.Act(s => s.ToggleExecute()),
                Wait = () => input.Run(s => s.Wait()),
                ToggleRules = () => { rulesOpen = !rulesOpen; RenderBattle(); },
                ToggleLog = () => { logOpen = !logOpen; RenderBattle(); },
                Retry = () => StartBattle(stage, Session.Battle.StartFormation, true),
                Redeploy = () => StartBattle(stage, Session.Battle.StartFormation, false),
                ToCamp = () => App.GoCamp(Back()),
                CloseOver = () => overRoot.gameObject.SetActive(false),
            };
            demo = App.BattleStage == null || App.Demo;
            StartBattle(App.BattleStage ?? GameData.Stages[0], App.Formation, App.StartNow);
            Fx.FadeIn(screen);
        }

        public void StartBattle(Stage s, IEnumerable<Placement<UnitKind>> formation, bool startNow)
        {
            stage = s;
            finished = enemyPlaying = leaveArm = rulesOpen = logOpen = false;
            hoverId = hoverCorpse = null;
            over = null;
            Ui.Clear(screen);

            boardArea = Ui.Node(screen, "BoardArea");
            // ui-style.md 1-3: the HUD keeps off a 360/150/360/190 px margin around the board.
            Ui.Stretch(boardArea, 360f, 150f, 360f, 190f);
            banner = Ui.Text(boardArea, "", 32f / Ui.Base, Ui.Steel, true, true, TextAnchor.UpperCenter);
            var br = banner.rectTransform;
            br.anchorMin = new Vector2(0f, 1f);
            br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(0f, -60f);
            br.offsetMax = new Vector2(0f, -12f);
            var ol = banner.gameObject.AddComponent<Outline>();
            ol.effectColor = Ui.Ink;
            ol.effectDistance = new Vector2(2f, -2f);
            banner.gameObject.SetActive(false);

            hudRoot = Ui.Node(screen, "Hud");
            Ui.Stretch(hudRoot);
            hud = new BattleHud(View.board.art);

            overRoot = Ui.Node(screen, "OverRoot");
            Ui.Stretch(overRoot);
            overRoot.gameObject.SetActive(false);

            Canvas.ForceUpdateCanvases();
            FitCameraRect();
            var battle = demo ? new Battle(s, App.DemoUpgrades, formation) : Campaign.NewBattle(s, formation);
            input.Begin(new BattleSession(battle));
            if (startNow) input.Run(x => x.Start());
        }

        // stage-select.md 5-3 2: the stage just played; a first clear that opened the next one shows it on the map.
        CampNav Back()
        {
            var i = System.Array.FindIndex(Campaign.Stages, x => x.Id == stage?.Id);
            return CampNav.FromBattle(Campaign, Mathf.Max(0, i), over != null && over.Won && over.Result.FirstClear, App.Nav?.Tab ?? -1);
        }

        void FitCameraRect()
        {
            if (viewCamera == null) return;
            var corners = new Vector3[4];
            Canvas.ForceUpdateCanvases();
            boardArea.GetWorldCorners(corners);
            float w = Screen.width, h = Screen.height;
            fittedSize = new Vector2Int(Screen.width, Screen.height);
            // stage-dressing.md 1-4: the world fills the screen behind the HUD; only the board is framed into the board area.
            viewCamera.rect = new Rect(0f, 0f, 1f, 1f);
            View.board.FrameInto(new Rect(corners[0].x / w, corners[0].y / h, (corners[2].x - corners[0].x) / w, (corners[2].y - corners[0].y) / h));
        }

        void RenderBattle()
        {
            if (Session == null) return;
            Ui.Clear(hudRoot);
            hud.Build(hudRoot, Session, HudNow(), actions);
        }

        HudState HudNow()
        {
            var b = Session.Battle;
            return new HudState
            {
                Hover = hoverId is int id ? b.Find(id) : null,
                HoverCorpse = hoverCorpse is int cid ? b.Corpses.FirstOrDefault(c => c.Id == cid) : null,
                Alt = altDown, EnemyPlaying = enemyPlaying, LeaveArmed = leaveArm, RulesOpen = rulesOpen, LogOpen = logOpen, Demo = demo,
                FullMoon = fullMoon, MoonFading = moonFading,
                Upgrades = (demo ? App.DemoUpgrades : Campaign.Upgrades) ?? Enumerable.Empty<string>(),
            };
        }

        // battle-hud.md 6: Esc closes a window, then leaves execute mode, and only then drops the selection.
        bool CloseBattlePopup()
        {
            if (rulesOpen || logOpen)
            {
                rulesOpen = logOpen = false;
                RenderBattle();
                return true;
            }
            if (Session.Mode != SelectMode.Execute) return false;
            input.Act(x => x.ToggleExecute());
            return true;
        }

        void OnBattleChanged()
        {
            if (!View.IsBusy)
            {
                enemyPlaying = false;
                if (fullMoon) { fullMoon = false; moonFading = true; }
            }
            RenderBattle();
            moonFading = false;
            if (!finished && Session.Battle.Phase == Phase.Over && !View.IsBusy) Finish();
        }

        void OnPlayed(BattleEvent e)
        {
            if (e.Kind != BattleEventKind.Banner || banner == null) return;
            // The model has already left the full-moon turn while it plays back, so the banner is the only cue (ui-style.md 7-2).
            if (e.Text.Key == "banner.fullMoon") { fullMoon = true; RenderBattle(); }
            banner.text = Loc.Text(e.Text);
            banner.color = e.Style == "enemy" ? Ui.Blood : Ui.Steel;
            banner.gameObject.SetActive(true);
            bannerUntil = Time.unscaledTime + BannerSeconds;
        }

        void Update()
        {
            if (Session == null) return;
            if (leaveArm && Time.unscaledTime > leaveUntil) { leaveArm = false; RenderBattle(); }
            if (banner != null && banner.gameObject.activeSelf && Time.unscaledTime > bannerUntil) banner.gameObject.SetActive(false);
            if (Screen.width != fittedSize.x || Screen.height != fittedSize.y) FitCameraRect();
            TrackHover();
            Hotkeys();
        }

        void Hotkeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var one = keys.Pressed("1", kb.digit1Key.isPressed);
            var two = keys.Pressed("2", kb.digit2Key.isPressed);
            var space = keys.Pressed("space", kb.spaceKey.isPressed);
            if (View.IsBusy || enemyPlaying || OverShown) return;
            var b = Session.Battle;
            var sel = Session.SelectedUnit;
            var canAct = b.Phase == Phase.Player && sel != null && !sel.Acted;
            if (one && canAct) actions.Exec();
            if (two && canAct) actions.Wait();
            if (space && (b.Phase == Phase.Deploy ? b.Allies.Any() : b.Phase == Phase.Player)) Primary();
        }

        void TrackHover()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var alt = kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
            var m = UnityEngine.InputSystem.Mouse.current;
            var at = m != null && !input.PointerBlocked() ? input.HexAt(m.position.ReadValue()) : null;
            var over = at is Hex h ? Session.Battle.UnitAt(h)?.Id : null;
            var corpse = at is Hex hc && over == null ? Session.Battle.CorpseAt(hc)?.Id : null;
            input.overlay.SetHover(at);
            if (over == hoverId && corpse == hoverCorpse && alt == altDown) return;
            var altChanged = alt != altDown;
            hoverId = over;
            hoverCorpse = corpse;
            altDown = alt;
            if (altChanged) RenderBattle();
            else hud.RebuildHover(Session, HudNow(), actions);
        }

        void LateUpdate()
        {
            if (hud != null) hud.Tick(viewCamera != null ? viewCamera : Camera.main, View);
        }

        void Primary()
        {
            if (View.IsBusy) return;
            var b = Session.Battle;
            if (b.Phase == Phase.Deploy) input.Run(s => s.Start());
            else if (b.Phase == Phase.Player) { enemyPlaying = true; input.Run(s => s.EndTurn()); }
            else if (b.Phase == Phase.Over && over != null) overRoot.gameObject.SetActive(true);
        }

        void Leave()
        {
            if (Session.Battle.Phase == Phase.Player && !leaveArm && !enemyPlaying)
            {
                leaveArm = true;
                leaveUntil = Time.unscaledTime + LeaveSeconds;
                RenderBattle();
                return;
            }
            if (enemyPlaying) return;
            if (demo) App.GoMenu(); else App.GoCamp(Back());
        }

        void Finish()
        {
            finished = true;
            over = OverInfo.From(Session.Battle, demo, Campaign);
            if (over.Won && !demo) CampaignStore.Save(Campaign);
            Ui.Clear(overRoot);
            hud.Over(overRoot, over, actions);
            overRoot.gameObject.SetActive(true);
        }
    }
}
