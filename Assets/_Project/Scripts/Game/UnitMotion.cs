using UnityEngine;

namespace VampireSpawn.Game
{
    // docs/art/unit-visuals.md 2-1: the clip's root motion moves the model, the unit root stays on its hex.
    [RequireComponent(typeof(Animator))]
    public sealed class UnitMotion : MonoBehaviour
    {
        static readonly int IdleState = Animator.StringToHash("Idle");
        // SmoothDamp time; about 0.25 s to settle back on the hex.
        const float Settle = 0.08f;

        public Transform model;
        public bool walking;

        Animator animator;
        Vector3 velocity;

        void Awake() => animator = GetComponent<Animator>();

        void OnAnimatorMove()
        {
            if (animator == null || model == null || walking) return;
            var d = animator.deltaPosition;
            d.y = 0f;
            model.position += d;
            if (animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).shortNameHash != IdleState) return;
            model.localPosition = Vector3.SmoothDamp(model.localPosition, Vector3.zero, ref velocity, Settle);
        }
    }
}
