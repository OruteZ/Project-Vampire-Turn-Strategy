using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
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

        public bool OverShown => overRoot != null && overRoot.ClassListContains("shown");

        bool demo, leaveArm, rulesOpen, logOpen, finished, enemyPlaying, fullMoon, moonFading, altDown;
        float leaveUntil, bannerUntil;
        Vector2Int fittedSize;
        Stage stage;
        PlayLog playLog;
        OverInfo over;
        UIDocument doc;
        VisualElement overRoot, hudRoot;
        BattleHud hud;
        int? hoverId, hoverCorpse;
        Hex? hoverArrival;
        Label banner;
        BattleActions actions;
        readonly KeyEdges keys = new KeyEdges();

        BattleSession Session => input.Session;
        BattleView View => input.battle;
        Campaign Campaign => App.Campaign;

        // battle-hud.md 1: the battle HUD draws at this fraction of the shared 1920x1080 panel.
        public const float HudZoom = 0.55f;

        void Start()
        {
            App.Ensure(data);
            Sound.Use(View.board.art.sound);
            doc = ToolkitKit.Document(transform, "Battle (UI Toolkit)", ToolkitKit.Zoomed(View.board.art.panel, HudZoom), View.board.art.battleDocument);
            var root = doc.rootVisualElement;
            banner = root.Q<Label>("banner");
            hudRoot = root.Q("hud");
            overRoot = root.Q("over");
            hud = new BattleHud(View.board.art, root.Q<Label>("tip"));
            if (viewCamera != null)
            {
                viewCamera.clearFlags = CameraClearFlags.SolidColor;
                viewCamera.backgroundColor = Ui.Ink;
            }
            input.Changed += OnBattleChanged;
            View.Played += OnPlayed;
            input.PointerBlocked = () => ToolkitKit.PointerOverUi(doc);
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
                Mend = () => input.Act(s => s.ToggleMend()),
                Wait = () => input.Run(s => s.Wait()),
                Aim = () => input.Act(s => s.ClearMode()),
                ToggleRules = () => { rulesOpen = !rulesOpen; RenderBattle(); },
                ToggleLog = () => { logOpen = !logOpen; RenderBattle(); },
                Retry = () => StartBattle(stage, Session.Battle.StartFormation, true),
                Redeploy = () => StartBattle(stage, Session.Battle.StartFormation, false),
                ToCamp = () => App.GoCamp(Back()),
                CloseOver = () => overRoot.RemoveFromClassList("shown"),
            };
            demo = App.BattleStage == null || App.Demo;
            StartBattle(App.BattleStage ?? GameData.Stages[0], App.Formation, App.StartNow);
            Fx.FadeIn(root);
        }

        public void StartBattle(Stage s, IEnumerable<Placement> formation, bool startNow)
        {
            stage = s;
            View.speed = GameSettings.Speed;
            Sound.Music(x => x.battleMusic);
            Sound.Ambience(x => x.battleAmbience);
            finished = enemyPlaying = leaveArm = rulesOpen = logOpen = false;
            hoverId = hoverCorpse = null;
            hoverArrival = null;
            over = null;
            banner.RemoveFromClassList("shown");
            hudRoot.Clear();
            overRoot.Clear();
            overRoot.RemoveFromClassList("shown");
            FitCameraRect();
            var battle = demo ? new Battle(s, App.DemoUpgrades, formation) : Campaign.NewBattle(s, formation);
            input.Begin(new BattleSession(battle));
            playLog = new PlayLog(s.Id);
            if (startNow) { playLog.Started(); input.Run(x => x.Start()); }
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
            fittedSize = new Vector2Int(Screen.width, Screen.height);
            // stage-dressing.md 1-4: the world fills the screen behind the HUD; only the board is framed into the board area
            // (.board-area in Battle.uss: 360/150/360/190 px, ui-style.md 1-3), in viewport units with y up.
            var size = ToolkitKit.PanelSize(HudZoom);
            viewCamera.rect = new Rect(0f, 0f, 1f, 1f);
            View.board.FrameInto(new Rect(360f / size.x, 190f / size.y, (size.x - 720f) / size.x, (size.y - 340f) / size.y));
        }

        void RenderBattle()
        {
            if (Session == null) return;
            hud.Build(hudRoot, Session, HudNow(), actions);
        }

        HudState HudNow()
        {
            var b = Session.Battle;
            return new HudState
            {
                Hover = hoverId is int id ? b.Find(id) : null,
                HoverCorpse = hoverCorpse is int cid ? b.Corpses.FirstOrDefault(c => c.Id == cid) : null,
                HoverArrival = hoverArrival is Hex ah && b.Pending.Any(p => p.Col == ah.Col && p.Row == ah.Row)
                    ? b.Pending.First(p => p.Col == ah.Col && p.Row == ah.Row) : (Placement?)null,
                HoverReinforce = hoverArrival is Hex rh && !b.Reinforcing ? b.Upcoming.FirstOrDefault(r => r.Hexes.Contains(rh)) : null,
                Speed = View.speed,
                Alt = altDown, EnemyPlaying = enemyPlaying, LeaveArmed = leaveArm, RulesOpen = rulesOpen, LogOpen = logOpen, Demo = demo,
                FullMoon = fullMoon, MoonFading = moonFading,
                Upgrades = (demo ? App.DemoUpgrades : Campaign.Upgrades) ?? Enumerable.Empty<string>(),
            };
        }

        // battle-hud.md 6: Esc closes a window, then leaves execute or mend mode, and only then drops the selection.
        bool CloseBattlePopup()
        {
            if (rulesOpen || logOpen)
            {
                rulesOpen = logOpen = false;
                RenderBattle();
                return true;
            }
            if (Session.Mode == SelectMode.Execute) input.Act(x => x.ToggleExecute());
            else if (Session.Mode == SelectMode.Mend) input.Act(x => x.ToggleMend());
            else return false;
            return true;
        }

        void OnBattleChanged()
        {
            if (!View.IsBusy)
            {
                if (enemyPlaying) playLog?.EnemyEnds(Session.Battle);
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
            if (e.BannerKind == BannerKind.FullMoon) { fullMoon = true; RenderBattle(); }
            banner.text = Loc.Text(e.Text);
            banner.style.color = e.BannerKind != BannerKind.PlayerTurn ? Ui.Blood : Ui.Steel;
            banner.AddToClassList("shown");
            bannerUntil = Time.unscaledTime + BannerSeconds;
        }

        void Update()
        {
            if (Session == null) return;
            if (leaveArm && Time.unscaledTime > leaveUntil) { leaveArm = false; RenderBattle(); }
            if (banner != null && banner.ClassListContains("shown") && Time.unscaledTime > bannerUntil) banner.RemoveFromClassList("shown");
            if (Screen.width != fittedSize.x || Screen.height != fittedSize.y) FitCameraRect();
            TrackHover();
            Hotkeys();
        }

        void Hotkeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var digits = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key };
            var digit = 0;
            for (var i = 0; i < digits.Length; i++) if (keys.Pressed((i + 1).ToString(), digits[i].isPressed)) digit = i + 1;
            var space = keys.Pressed("space", kb.spaceKey.isPressed);
            // docs/ui/battle-hud.md 4-5: F cycles enemy playback 1x, 2x, 4x and is remembered.
            if (keys.Pressed("f", kb.fKey.isPressed))
            {
                View.speed = View.speed >= 4f ? 1f : View.speed * 2f;
                GameSettings.Speed = View.speed;
                RenderBattle();
            }
            if (View.IsBusy || enemyPlaying || OverShown) return;
            var b = Session.Battle;
            if (digit > 0) BattleScreen.Slots(Session, actions).FirstOrDefault(x => x.Key == digit && x.Ready)?.Act?.Invoke();
            if (space && (b.Phase == Phase.Deploy ? b.Reinforcing || b.Allies.Any() : b.Phase == Phase.Player)) Primary();
        }

        void TrackHover()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var alt = kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
            var m = UnityEngine.InputSystem.Mouse.current;
            var at = m != null && !input.PointerBlocked() ? input.HexAt(m.position.ReadValue()) : null;
            var over = at is Hex h ? Session.Battle.UnitAt(h)?.Id : null;
            var corpse = at is Hex hc && over == null ? Session.Battle.CorpseAt(hc)?.Id : null;
            var arrival = at is Hex ha && over == null && corpse == null && (Session.Battle.Pending.Any(p => p.Col == ha.Col && p.Row == ha.Row)
                || (!Session.Battle.Reinforcing && Session.Battle.Upcoming.Any(r => r.Hexes.Contains(ha)))) ? at : null;
            input.overlay.SetHover(at);
            if (over == hoverId && corpse == hoverCorpse && arrival == hoverArrival && alt == altDown) return;
            var altChanged = alt != altDown;
            hoverId = over;
            hoverCorpse = corpse;
            hoverArrival = arrival;
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
            if (b.Phase == Phase.Deploy) { if (!b.Reinforcing) playLog.Started(); input.Run(s => s.Start()); }
            else if (b.Phase == Phase.Player) { enemyPlaying = true; playLog.EnemyBegins(); input.Run(s => s.EndTurn()); }
            else if (b.Phase == Phase.Over && over != null) overRoot.AddToClassList("shown");
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
            Debug.Log("플레이 기록: " + playLog.Finish(Session.Battle, over.Won));
            Sound.Music(null);
            if (over.Won) Sound.Play(x => x.victory); else Sound.Play(x => x.defeat);
            if (over.Won && !demo) CampaignStore.Save(Campaign);
            hud.Over(overRoot, over, actions);
            overRoot.AddToClassList("shown");
        }
    }
}
