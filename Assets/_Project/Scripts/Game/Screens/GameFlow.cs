using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Game.Screens
{
    // The prototype's screen switching (show/renderMenu/renderCamp), battle controls and finish().
    public sealed class GameFlow : MonoBehaviour
    {
        public BoardInput input;
        [Tooltip("게임이 쓰는 데이터. 비었거나 문제가 있으면 HTML 기본값으로 돈다")] public GameDataAsset data;
        public Camera viewCamera;

        // Prototype timers: leave confirmation 2500 ms, phase banner 1300 ms.
        const float LeaveSeconds = 2.5f, BannerSeconds = 1.3f;

        public string Current { get; private set; }
        public Campaign Campaign { get; private set; }
        public bool OverShown => overRoot != null && overRoot.gameObject.activeSelf;

        bool demo, resetArm, leaveArm, rulesOpen, logOpen, finished, enemyPlaying, fullMoon, moonFading;
        float leaveUntil, bannerUntil, buyUntil;
        string buyArm, bought;
        int? campSel;
        int menuSel = 1;
        RenderTexture backdropTex;
        Vector2Int fittedSize;
        Stage stage;
        OverInfo over;
        RectTransform screen, boardArea, overRoot, hudRoot;
        CanvasScaler scaler;
        BattleHud hud;
        int? hoverId, hoverCorpse;
        bool altDown;
        readonly Dictionary<string, bool> keyDown = new Dictionary<string, bool>();
        Text banner;
        CampaignActions campActions;
        BattleActions battleActions;

        BattleSession Session => input.Session;
        BattleView View => input.battle;

        void Awake()
        {
            UseData();
            BuildCanvas();
            Campaign = CampaignStore.Load();
            input.Changed += OnBattleChanged;
            View.Played += OnPlayed;
            input.PointerBlocked = () => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            input.EscapeFirst = CloseBattlePopup;
            campActions = new CampaignActions
            {
                Play = i => { buyArm = null; demo = false; campSel = i; StartBattle(Campaign.Stages[i], null, false); },
                Select = i => { buyArm = null; resetArm = false; campSel = i; ShowCamp(); },
                Buy = id =>
                {
                    if (buyArm == id) { buyArm = null; if (Campaign.Buy(id)) { CampaignStore.Save(Campaign); bought = id; } }
                    else { buyArm = id; buyUntil = Time.unscaledTime + LeaveSeconds; }
                    ShowCamp();
                },
                Reset = () => { buyArm = null; if (resetArm) { Campaign = new Campaign(); CampaignStore.Save(Campaign); resetArm = false; } else resetArm = true; ShowCamp(); },
                Menu = () => { resetArm = false; buyArm = null; ShowMenu(); },
            };
            battleActions = new BattleActions
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
                ToCamp = () => { LeaveBattle(); ShowCamp(); },
                CloseOver = () => overRoot.gameObject.SetActive(false),
            };
        }

        void Start()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            ShowMenu();
        }

        void OnDestroy() => LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        // Only the main menu offers a language switch (docs/ui/localization.md 9).
        void OnLocaleChanged(UnityEngine.Localization.Locale _) { if (Current == "menu") ShowMenu(false); }

        void UseData()
        {
            if (data == null) { GameData.Use(null); return; }
            var problems = data.Problems();
            if (problems.Count > 0)
            {
                Debug.LogError("게임 데이터에 문제가 있어 HTML 기본값으로 돈다:\n" + string.Join("\n", problems));
                GameData.Use(null);
                return;
            }
            GameData.Use(data.ToDataSet());
        }

        void BuildCanvas()
        {
            var go = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            screen = Ui.Node(go.transform, "Screen");
            Ui.Stretch(screen);
            // The board camera draws only into the board area; without a full-screen clear the overlay UI blends over stale pixels.
            if (viewCamera != null)
            {
                // Same clear colour as the backdrop so the board area edge does not show (battle-hud.md 1-2).
                viewCamera.clearFlags = CameraClearFlags.SolidColor;
                viewCamera.backgroundColor = Ui.Ink;
                var back = new GameObject("Backdrop Camera").AddComponent<Camera>();
                back.transform.SetParent(transform, false);
                back.depth = viewCamera.depth - 1f;
                back.clearFlags = CameraClearFlags.SolidColor;
                back.backgroundColor = Ui.Ink;
                back.cullingMask = 0;
            }
            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform, false);
        }

        void ClearScreen(bool opaque)
        {
            Ui.Clear(screen);
            if (!screen.TryGetComponent<Image>(out var bg)) bg = screen.gameObject.AddComponent<Image>();
            bg.color = Ui.Ink;
            bg.enabled = opaque;
            if (viewCamera != null)
            {
                viewCamera.rect = new Rect(0f, 0f, 1f, 1f);
                viewCamera.targetTexture = null;
                View.board.FrameInto(new Rect(0f, 0f, 1f, 1f));
            }
            if (Current != "battle") View.board.Clear();
        }

        // ui-style.md 11-1: the stage board, blurred and darkened 60%. A tiny render target upscaled is the blur.
        void Backdrop(Stage s)
        {
            if (viewCamera == null) return;
            if (backdropTex == null) backdropTex = new RenderTexture(240, 135, 16) { filterMode = FilterMode.Bilinear };
            View.board.Show(s);
            viewCamera.targetTexture = backdropTex;
            var img = Ui.Node(screen, "Backdrop").gameObject.AddComponent<RawImage>();
            img.texture = backdropTex;
            img.color = new Color(0.4f, 0.4f, 0.4f);
            img.raycastTarget = false;
            Ui.Stretch(img.rectTransform);
            img.transform.SetAsFirstSibling();
        }

        public void ShowMenu() => ShowMenu(true);

        void ShowMenu(bool fade)
        {
            Current = "menu";
            ClearScreen(true);
            Backdrop(Campaign.Stages[0]);
            MenuScreen.Build(screen, Campaign, MenuDemo, MenuCamp, View.board.art, menuSel, i => { if (menuSel != i) { menuSel = i; ShowMenu(false); } });
            if (fade) Fx.FadeIn(screen);
        }

        void MenuDemo() { demo = true; StartBattle(GameData.Stages[0], null, false); }

        void MenuCamp() { demo = false; ShowCamp(); }

        void MenuKeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var left = Pressed("left", kb.leftArrowKey.isPressed);
            var right = Pressed("right", kb.rightArrowKey.isPressed);
            var enter = Pressed("enter", kb.enterKey.isPressed || kb.numpadEnterKey.isPressed);
            if ((left && menuSel != 0) || (right && menuSel != 1)) { menuSel = left ? 0 : 1; ShowMenu(false); }
            else if (enter) { if (menuSel == 0) MenuDemo(); else MenuCamp(); }
        }

        public void ShowCamp()
        {
            Current = "camp";
            ClearScreen(true);
            var sel = campSel ?? CampaignScreen.DefaultStage(Campaign);
            Backdrop(Campaign.Stages[sel]);
            CampaignScreen.Build(screen, Campaign, new CampView { Selected = sel, ResetArm = resetArm, BuyArm = buyArm, Bought = bought }, campActions, View.board.art);
            bought = null;
        }

        public void StartBattle(Stage s, IEnumerable<Placement<UnitKind>> formation, bool startNow)
        {
            Current = "battle";
            stage = s;
            finished = enemyPlaying = leaveArm = false;
            ClearScreen(false);
            rulesOpen = logOpen = false;
            hoverId = hoverCorpse = null;

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
            var battle = demo ? new Battle(s, null, formation) : Campaign.NewBattle(s, formation);
            input.Begin(new BattleSession(battle));
            if (startNow) input.Run(x => x.Start());
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
            if (Current != "battle" || Session == null) return;
            var b = Session.Battle;
            Ui.Clear(hudRoot);
            hud.Build(hudRoot, Session, new HudState
            {
                Hover = hoverId is int id ? b.Find(id) : null,
                HoverCorpse = hoverCorpse is int cid ? b.Corpses.FirstOrDefault(c => c.Id == cid) : null,
                Alt = altDown, EnemyPlaying = enemyPlaying, LeaveArmed = leaveArm, RulesOpen = rulesOpen, LogOpen = logOpen, Demo = demo,
                FullMoon = fullMoon, MoonFading = moonFading,
                Upgrades = demo ? Enumerable.Empty<string>() : Campaign.Upgrades,
            }, battleActions);
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
            if (Current != "battle") return;
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
            if (leaveArm && Time.unscaledTime > leaveUntil) { leaveArm = false; RenderBattle(); }
            if (buyArm != null && Time.unscaledTime > buyUntil) { buyArm = null; if (Current == "camp") ShowCamp(); }
            if (banner != null && banner.gameObject.activeSelf && Time.unscaledTime > bannerUntil) banner.gameObject.SetActive(false);
            if (Current == "battle" && Session != null)
            {
                if (Screen.width != fittedSize.x || Screen.height != fittedSize.y) FitCameraRect();
                TrackHover();
                Hotkeys();
            }
            if (Current == "camp") CampKeys();
            else if (Current == "menu") MenuKeys();
        }

        bool Pressed(string name, bool down)
        {
            keyDown.TryGetValue(name, out var was);
            keyDown[name] = down;
            return down && !was;
        }

        void CampKeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var up = Pressed("up", kb.upArrowKey.isPressed);
            var down = Pressed("down", kb.downArrowKey.isPressed);
            var enter = Pressed("enter", kb.enterKey.isPressed || kb.numpadEnterKey.isPressed);
            var esc = Pressed("esc", kb.escapeKey.isPressed);
            var sel = campSel ?? CampaignScreen.DefaultStage(Campaign);
            if (up && sel > 0) campActions.Select(sel - 1);
            else if (down && sel < Campaign.Stages.Length - 1) campActions.Select(sel + 1);
            else if (enter && Campaign.StageOpen(sel)) campActions.Play(sel);
            else if (esc) campActions.Menu();
        }

        void Hotkeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var one = Pressed("1", kb.digit1Key.isPressed);
            var two = Pressed("2", kb.digit2Key.isPressed);
            var space = Pressed("space", kb.spaceKey.isPressed);
            if (View.IsBusy || enemyPlaying || OverShown) return;
            var b = Session.Battle;
            var sel = Session.SelectedUnit;
            var canAct = b.Phase == Phase.Player && sel != null && !sel.Acted;
            if (one && canAct) battleActions.Exec();
            if (two && canAct) battleActions.Wait();
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
            hoverId = over;
            hoverCorpse = corpse;
            altDown = alt;
            RenderBattle();
        }

        void LateUpdate()
        {
            if (hud != null && Current == "battle") hud.Tick(viewCamera != null ? viewCamera : Camera.main, View);
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
            LeaveBattle();
            if (demo) ShowMenu(); else ShowCamp();
        }

        void LeaveBattle()
        {
            leaveArm = false;
            over = null;
            input.End();
            View.Clear();
        }

        void Finish()
        {
            finished = true;
            over = OverInfo.From(Session.Battle, demo, Campaign);
            if (over.Won && !demo) CampaignStore.Save(Campaign);
            Ui.Clear(overRoot);
            hud.Over(overRoot, over, battleActions);
            overRoot.gameObject.SetActive(true);
        }
    }
}
