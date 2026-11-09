using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // docs/art/battle-camera.md: slides the fixed board view toward what is playing and back. Runs after BoardView so its fitted pose is the base.
    [DefaultExecutionOrder(100)]
    public sealed class BattleCamera : MonoBehaviour
    {
        const float SmoothSeconds = 0.6f, PreviewSeconds = 2f, OpeningSeconds = 1.4f, SlowSeconds = 1.6f, SlowScale = 0.35f;

        struct Shot
        {
            public float Pan, Zoom, Saturation, Vignette;
            public Shot(float pan, float zoom, float saturation = 0f, float vignette = 0f) { Pan = pan; Zoom = zoom; Saturation = saturation; Vignette = vignette; }
        }

        static readonly Shot Rise = new Shot(0.6f, 0.35f, -60f), RiseChain = new Shot(0.6f, 0.25f, -60f),
            Follow = new Shot(0.3f, 0.1f), Moon = new Shot(0.5f, 0.25f), MoonPreview = new Shot(0.4f, 0.15f), Drain = new Shot(0.6f, 0.35f, 0f, 0.6f),
            Kill = new Shot(0.5f, 0.3f), Opening = new Shot(0.7f, 0.35f), Final = new Shot(0.8f, 0.45f), Execute = new Shot(0.4f, 0.25f);

        // Shake amplitude is a fraction of the base camera distance; shake, stops and slow motion run in real seconds over speed, so a stop never freezes its own timer (battle-camera.md 6).
        const float Shake = 0.006f, ShakeSeconds = 0.4f, BigStop = 0.09f, ExecuteStop = 0.12f;
        // A big hit (battle-camera.md 6-2): ordinary damage in the data is 1-3.
        const int BigDamage = 4;

        BattleView view;
        Camera cam;
        Volume volume;
        ColorAdjustments color;
        Vignette vignette;

        Shot shot;
        Vector3 focus, at, atVelocity;
        int? followId, previewedTarget, moonTargetId, vampireId;
        readonly HashSet<int> allies = new HashSet<int>(), notable = new HashSet<int>();
        int rises;
        bool moonOn, riseOn, drainOn, drainNext, finished;
        float previewUntil, openingUntil, saturation, saturationVelocity, vignetteAmount, vignetteVelocity;
        float shakeUntil, shakeStart = -1f, stopUntil, slowUntil;

        Battle Battle => view.Battle;
        BoardView Board => view.board;
        float Speed => view.speed;
        float Now => Time.unscaledTime;

        public void Attach(BattleView battleView)
        {
            view = battleView;
            cam = Board.viewCamera;
            if (cam == null) return;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            volume = new GameObject("Battle Camera FX").AddComponent<Volume>();
            volume.transform.SetParent(transform, false);
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            color = volume.profile.Add<ColorAdjustments>(true);
            vignette = volume.profile.Add<Vignette>(true);
            vignette.color.Override(Screens.Ui.MoonBlood);
            vignette.intensity.Override(0f);
            vignette.smoothness.Override(0.7f);
            view.Played += OnPlayed;
            Snap();
        }

        public void Snap()
        {
            Rest();
            finished = false;
            previewedTarget = null;
            moonTargetId = null;
            previewUntil = stopUntil = slowUntil = openingUntil = 0f;
            shakeStart = -1f;
            Time.timeScale = 1f;
            at = Board.BasePosition;
            atVelocity = Vector3.zero;
            saturation = vignetteAmount = 0f;
            Learn();
            if (Speed > 0f && vampireId.HasValue && cam != null)
            {
                openingUntil = Now + OpeningSeconds / Speed;
                focus = Where(vampireId);
                shot = Opening;
                at = Aim();
            }
        }

        void Rest()
        {
            shot = default;
            followId = null;
            rises = 0;
            moonOn = riseOn = drainOn = drainNext = false;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (view != null) view.Played -= OnPlayed;
            if (volume != null) Destroy(volume.profile);
        }

        // Extra playback seconds (before pace) the view waits after an event so its shot can be seen (battle-camera.md 5, 7, 9, 10).
        public float HoldFor(BattleEvent e)
        {
            if (Battle == null || cam == null || Speed <= 0f) return 0f;
            return e.Kind switch
            {
                BattleEventKind.Hit when drainOn => 0.8f,
                BattleEventKind.Died when finished => 1f,
                BattleEventKind.Died when notable.Contains(e.UnitId) => 0.5f,
                BattleEventKind.Popup when e.PopupKind == PopupKind.Execute => 0.5f,
                _ => 0f,
            };
        }

        void OnPlayed(BattleEvent e)
        {
            if (Battle == null || cam == null || Speed <= 0f) return;
            switch (e.Kind)
            {
                case BattleEventKind.Rose:
                    rises++;
                    riseOn = true;
                    followId = null;
                    notable.Add(e.UnitId);
                    focus = Board.HexToWorld(e.At);
                    shot = rises == 1 ? Rise : RiseChain;
                    break;
                case BattleEventKind.Banner when e.BannerKind == BannerKind.FullMoon:
                    riseOn = false;
                    moonOn = true;
                    followId = null;
                    focus = Between(vampireId, moonTargetId);
                    shot = Moon;
                    break;
                case BattleEventKind.Banner:
                    if (e.BannerKind == BannerKind.PlayerTurn) Rest();
                    else riseOn = moonOn = false;
                    break;
                case BattleEventKind.Walk when IsEnemy(e.UnitId):
                    riseOn = moonOn = false;
                    followId = e.UnitId;
                    shot = Follow;
                    break;
                case BattleEventKind.Popup when e.PopupKind == PopupKind.Execute:
                    followId = null;
                    focus = Where(e.UnitId);
                    shot = Execute;
                    Stop(ExecuteStop);
                    break;
                case BattleEventKind.Popup:
                    drainNext = e.PopupKind == PopupKind.Drain;
                    break;
                case BattleEventKind.Hit:
                    OnHit(e);
                    break;
                case BattleEventKind.Died:
                    allies.Remove(e.UnitId);
                    moonOn = drainOn = false;
                    if (!finished && notable.Contains(e.UnitId))
                    {
                        followId = null;
                        focus = Where(e.UnitId);
                        shot = Kill;
                    }
                    break;
            }
        }

        void OnHit(BattleEvent e)
        {
            var big = drainNext || e.Value >= BigDamage;
            if (big) StartShake();
            drainOn = drainNext;
            drainNext = false;
            if (IsFinalBlow(e.UnitId))
            {
                finished = true;
                followId = null;
                focus = Where(e.UnitId);
                shot = drainOn ? new Shot(Final.Pan, Final.Zoom, 0f, Drain.Vignette) : Final;
                slowUntil = Now + SlowSeconds / Speed;
                return;
            }
            if (big) Stop(BigStop);
            if (!IsEnemy(e.OtherId)) return;
            riseOn = false;
            followId = null;
            focus = Between(e.OtherId, e.UnitId);
            shot = drainOn ? Drain : moonOn ? Moon : Follow;
        }

        // The battle state is already final while playback runs, so a target missing from it dies within this command.
        bool IsFinalBlow(int targetId) =>
            Battle.Phase == Phase.Over && Battle.Find(targetId) == null
            && (targetId == vampireId || (allies.Count == 1 && allies.Contains(targetId)));

        void StartShake()
        {
            shakeStart = Now;
            shakeUntil = Now + ShakeSeconds / Speed;
        }

        void Stop(float seconds) => stopUntil = Mathf.Max(stopUntil, Now + seconds / Speed);

        void Learn()
        {
            allies.Clear();
            notable.Clear();
            vampireId = Battle?.Vampire?.Id;
            if (Battle == null) return;
            foreach (var u in Battle.Units)
            {
                if (u.Side == Side.Ally) allies.Add(u.Id);
                else if (u.Role == Role.Vampire || u.Role == Role.Spawn) notable.Add(u.Id);
            }
        }

        bool IsEnemy(int id) => Battle.Find(id)?.Side == Side.Enemy;

        Vector3 Where(int? id) => id.HasValue && view.ViewOf(id.Value) is UnitView v ? v.transform.position : focus;

        Vector3 Between(int? a, int? b)
        {
            var hasA = a.HasValue && view.ViewOf(a.Value) != null;
            var hasB = b.HasValue && view.ViewOf(b.Value) != null;
            if (hasA && hasB) return (Where(a) + Where(b)) * 0.5f;
            return hasA ? Where(a) : Where(b);
        }

        Vector3 Aim()
        {
            var basePos = Board.BasePosition;
            return basePos + (focus - Board.BaseLook) * shot.Pan + (Board.BaseLook - basePos) * shot.Zoom;
        }

        void LateUpdate()
        {
            if (cam == null || Battle == null) return;
            if (Speed <= 0f) { Snap(); Apply(Vector3.zero); return; }
            Time.timeScale = Now < stopUntil ? 0f : Now < slowUntil ? SlowScale : 1f;
            if (!view.IsBusy) Idle();
            if (followId.HasValue) focus = Where(followId);

            var smooth = SmoothSeconds / Speed;
            at = Vector3.SmoothDamp(at, Aim(), ref atVelocity, smooth);
            var tinted = riseOn || drainOn;
            saturation = Mathf.SmoothDamp(saturation, tinted ? shot.Saturation : 0f, ref saturationVelocity, smooth);
            vignetteAmount = Mathf.SmoothDamp(vignetteAmount, tinted ? shot.Vignette : 0f, ref vignetteVelocity, smooth);
            Apply(ShakeOffset());
        }

        Vector3 ShakeOffset()
        {
            if (shakeStart < 0f) return Vector3.zero;
            if (Now >= shakeUntil) { shakeStart = -1f; return Vector3.zero; }
            var t = Now - shakeStart;
            var a = Shake * (Board.BaseLook - Board.BasePosition).magnitude * (Now - shakeUntil) / (shakeStart - shakeUntil);
            return cam.transform.right * (Mathf.Sin(t * 55f) * a) + cam.transform.up * (Mathf.Sin(t * 43f + 1f) * a * 0.6f);
        }

        void Idle()
        {
            if (finished) return;
            Learn();
            if (Now < openingUntil)
            {
                if (!AnyInput()) return;
                openingUntil = 0f;
            }
            var target = Battle.Phase == Phase.Player ? Battle.EatTarget : null;
            if (target != previewedTarget)
            {
                previewedTarget = target;
                if (target.HasValue)
                {
                    moonTargetId = target;
                    previewUntil = Now + PreviewSeconds / Speed;
                }
            }
            Rest();
            if (Now < previewUntil)
            {
                focus = Between(vampireId, moonTargetId);
                shot = MoonPreview;
            }
        }

        static bool AnyInput() =>
            (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
            || (Keyboard.current != null && Keyboard.current.anyKey.isPressed);

        void Apply(Vector3 shake)
        {
            cam.transform.position = at + shake;
            color.saturation.Override(saturation);
            vignette.intensity.Override(vignetteAmount);
        }
    }
}
