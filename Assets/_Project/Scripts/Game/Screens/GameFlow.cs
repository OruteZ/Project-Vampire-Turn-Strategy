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
        const float SideWidth = 340f, BarHeight = 84f, Gap = 16f;

        public string Current { get; private set; }
        public Campaign Campaign { get; private set; }
        public bool OverShown => overRoot != null && overRoot.gameObject.activeSelf;

        bool demo, resetArm, leaveArm, rulesOpen, finished, enemyPlaying;
        float leaveUntil, bannerUntil, buyUntil;
        string buyArm;
        Stage stage;
        OverInfo over;
        RectTransform screen, bar, side, boardArea, overRoot;
        Text hint, banner;
        ScrollRect sideScroll;
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
            campActions = new CampaignActions
            {
                Play = i => { buyArm = null; demo = false; StartBattle(Campaign.Stages[i], null, false); },
                Buy = id =>
                {
                    if (buyArm == id) { buyArm = null; if (Campaign.Buy(id)) CampaignStore.Save(Campaign); }
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
                Threat = on => input.Act(s => s.ShowThreat = on),
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
        void OnLocaleChanged(UnityEngine.Localization.Locale _) { if (Current == "menu") ShowMenu(); }

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
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            screen = Ui.Node(go.transform, "Screen");
            Ui.Stretch(screen);
            // The board camera draws only into the board area; without a full-screen clear the overlay UI blends over stale pixels.
            if (viewCamera != null)
            {
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
            if (viewCamera != null) viewCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }

        public void ShowMenu()
        {
            Current = "menu";
            ClearScreen(true);
            MenuScreen.Build(screen, Campaign, () => { demo = true; StartBattle(GameData.Stages[0], null, false); }, () => { demo = false; ShowCamp(); });
        }

        public void ShowCamp()
        {
            Current = "camp";
            ClearScreen(true);
            CampaignScreen.Build(screen, Campaign, resetArm, buyArm, campActions);
        }

        public void StartBattle(Stage s, IEnumerable<Placement<UnitKind>> formation, bool startNow)
        {
            Current = "battle";
            stage = s;
            finished = enemyPlaying = leaveArm = false;
            ClearScreen(false);

            bar = Ui.Row(screen, "Bar", 24, 0, null, null, TextAnchor.LowerLeft);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = Vector2.one;
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(Gap, -BarHeight);
            bar.offsetMax = new Vector2(-Gap, -8f);
            bar.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(0, 0, 0, 10);
            var line = Ui.Node(screen, "BarLine");
            Ui.Bg(line, Ui.Line, null, false);
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = Vector2.one;
            line.offsetMin = new Vector2(Gap, -BarHeight - 1f);
            line.offsetMax = new Vector2(-Gap, -BarHeight);

            var sideView = Ui.Scroll(screen, "Side", out sideScroll);
            var sv = (RectTransform)sideScroll.transform;
            sv.anchorMin = new Vector2(1f, 0f);
            sv.anchorMax = Vector2.one;
            sv.pivot = new Vector2(1f, 0.5f);
            sv.offsetMin = new Vector2(-Gap - SideWidth, 12f);
            sv.offsetMax = new Vector2(-Gap, -BarHeight - 8f);
            side = sideView;

            boardArea = Ui.Node(screen, "BoardArea");
            Ui.Stretch(boardArea, Gap, BarHeight + 8f, Gap * 2 + SideWidth, 40f);
            banner = Ui.Text(boardArea, "", 1.05f, Ui.Candle, true, true, TextAnchor.UpperCenter);
            var br = banner.rectTransform;
            br.anchorMin = new Vector2(0f, 1f);
            br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(0f, -48f);
            br.offsetMax = new Vector2(0f, -18f);
            banner.gameObject.AddComponent<Outline>().effectColor = Ui.Deep;
            banner.gameObject.SetActive(false);

            hint = Ui.Text(screen, "", 0.85f, Ui.Muted);
            var hr = hint.rectTransform;
            hr.anchorMin = Vector2.zero;
            hr.anchorMax = new Vector2(1f, 0f);
            hr.offsetMin = new Vector2(Gap + 4f, 8f);
            hr.offsetMax = new Vector2(-Gap * 2 - SideWidth, 34f);

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
            boardArea.GetWorldCorners(corners);
            float w = Screen.width, h = Screen.height;
            viewCamera.rect = new Rect(corners[0].x / w, corners[0].y / h, (corners[2].x - corners[0].x) / w, (corners[2].y - corners[0].y) / h);
        }

        void RenderBattle()
        {
            if (Current != "battle" || Session == null) return;
            Ui.Clear(bar);
            BattleScreen.Header(bar, Session, demo, enemyPlaying, leaveArm, battleActions);
            var scroll = sideScroll.verticalNormalizedPosition;
            Ui.Clear(side);
            BattleScreen.SidePanel(side, Session, demo ? Enumerable.Empty<string>() : Campaign.Upgrades, rulesOpen, battleActions, open => { rulesOpen = open; RenderBattle(); });
            sideScroll.verticalNormalizedPosition = scroll;
            hint.text = BattleScreen.Hint(Session, enemyPlaying);
        }

        void OnBattleChanged()
        {
            if (Current != "battle") return;
            if (!View.IsBusy) enemyPlaying = false;
            RenderBattle();
            if (!finished && Session.Battle.Phase == Phase.Over && !View.IsBusy) Finish();
        }

        void OnPlayed(BattleEvent e)
        {
            if (e.Kind != BattleEventKind.Banner || banner == null) return;
            banner.text = Loc.Text(e.Text);
            banner.color = e.Style == "enemy" ? Ui.Blood : Ui.Candle;
            banner.gameObject.SetActive(true);
            bannerUntil = Time.unscaledTime + BannerSeconds;
        }

        void Update()
        {
            if (leaveArm && Time.unscaledTime > leaveUntil) { leaveArm = false; RenderBattle(); }
            if (buyArm != null && Time.unscaledTime > buyUntil) { buyArm = null; if (Current == "camp") ShowCamp(); }
            if (banner != null && banner.gameObject.activeSelf && Time.unscaledTime > bannerUntil) banner.gameObject.SetActive(false);
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
            BattleScreen.Over(overRoot, over, battleActions);
            overRoot.gameObject.SetActive(true);
        }
    }
}
