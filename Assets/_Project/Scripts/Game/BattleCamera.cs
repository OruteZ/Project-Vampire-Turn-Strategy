using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // docs/art/battle-camera.md: slides the fixed board view toward what is playing and back. Runs after BoardView so its fitted pose is the base.
    [DefaultExecutionOrder(100)]
    public sealed class BattleCamera : MonoBehaviour
    {
        const float SmoothSeconds = 0.6f, PreviewSeconds = 2f;

        struct Shot
        {
            public float Pan, Zoom, Saturation, Vignette;
            public Shot(float pan, float zoom, float saturation = 0f, float vignette = 0f) { Pan = pan; Zoom = zoom; Saturation = saturation; Vignette = vignette; }
        }

        static readonly Shot Rise = new Shot(0.6f, 0.35f, -60f), RiseChain = new Shot(0.6f, 0.25f, -60f),
            Follow = new Shot(0.3f, 0.1f), Moon = new Shot(0.5f, 0.25f), MoonPreview = new Shot(0.4f, 0.15f), Drain = new Shot(0.6f, 0.35f, 0f, 0.6f);

        BattleView view;
        Camera cam;
        Volume volume;
        ColorAdjustments color;
        Vignette vignette;

        Shot shot;
        Vector3 focus, at, atVelocity;
        int? followId, previewedTarget, moonTargetId;
        int rises;
        bool moonOn, riseOn, drainOn, drainNext;
        float previewUntil, saturation, saturationVelocity, vignetteAmount, vignetteVelocity;

        Battle Battle => view.Battle;
        BoardView Board => view.board;
        float Speed => view.speed;

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
            previewedTarget = null;
            moonTargetId = null;
            previewUntil = 0f;
            at = Board.BasePosition;
            atVelocity = Vector3.zero;
            saturation = vignetteAmount = 0f;
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
            if (view != null) view.Played -= OnPlayed;
            if (volume != null) Destroy(volume.profile);
        }

        void OnPlayed(BattleEvent e)
        {
            if (Battle == null) return;
            switch (e.Kind)
            {
                case BattleEventKind.Rose:
                    rises++;
                    riseOn = true;
                    followId = null;
                    focus = Board.HexToWorld(e.At);
                    shot = rises == 1 ? Rise : RiseChain;
                    break;
                case BattleEventKind.Banner when e.Text.Key == "banner.fullMoon":
                    riseOn = false;
                    moonOn = true;
                    followId = null;
                    focus = Between(Battle.Vampire?.Id, moonTargetId);
                    shot = Moon;
                    break;
                case BattleEventKind.Banner:
                    var playerTurn = e.Text.Key == "banner.playerTurn";
                    if (playerTurn) Rest();
                    else riseOn = moonOn = false;
                    break;
                case BattleEventKind.Walk when IsEnemy(e.UnitId):
                    riseOn = moonOn = false;
                    followId = e.UnitId;
                    shot = Follow;
                    break;
                case BattleEventKind.Popup:
                    drainNext = e.Style == "blood";
                    break;
                case BattleEventKind.Hit when IsEnemy(e.OtherId):
                    riseOn = false;
                    followId = null;
                    focus = Between(e.OtherId, e.UnitId);
                    drainOn = drainNext;
                    drainNext = false;
                    shot = drainOn ? Drain : moonOn ? Moon : Follow;
                    break;
                case BattleEventKind.Died:
                    moonOn = drainOn = false;
                    break;
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

        void LateUpdate()
        {
            if (cam == null || Battle == null) return;
            if (Speed <= 0f) { Snap(); Apply(); return; }
            if (!view.IsBusy) Idle();
            if (followId.HasValue) focus = Where(followId);

            var basePos = Board.BasePosition;
            var toLook = Board.BaseLook - basePos;
            var target = basePos + (focus - Board.BaseLook) * shot.Pan + toLook * shot.Zoom;
            var smooth = SmoothSeconds / Speed;
            at = Vector3.SmoothDamp(at, target, ref atVelocity, smooth);
            var tinted = riseOn || drainOn;
            saturation = Mathf.SmoothDamp(saturation, tinted ? shot.Saturation : 0f, ref saturationVelocity, smooth);
            vignetteAmount = Mathf.SmoothDamp(vignetteAmount, tinted ? shot.Vignette : 0f, ref vignetteVelocity, smooth);
            Apply();
        }

        void Idle()
        {
            var target = Battle.Phase == Phase.Player ? Battle.EatTarget : null;
            if (target != previewedTarget)
            {
                previewedTarget = target;
                if (target.HasValue)
                {
                    moonTargetId = target;
                    previewUntil = Time.time + PreviewSeconds / Speed;
                }
            }
            if (Time.time < previewUntil)
            {
                Rest();
                focus = Between(Battle.Vampire?.Id, moonTargetId);
                shot = MoonPreview;
            }
            else Rest();
        }

        void Apply()
        {
            cam.transform.position = at;
            color.saturation.Override(saturation);
            vignette.intensity.Override(vignetteAmount);
        }
    }
}
