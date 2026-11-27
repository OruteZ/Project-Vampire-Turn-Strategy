using UnityEngine;

namespace VampireSpawn.Game
{
    public sealed class UnitView : MonoBehaviour
    {
        public int Id { get; private set; }
        public float HpFraction { get; private set; } = 1f;
        public bool Lying { get; private set; }
        public float Height => art.unitHeight * art.hexSize * (look != null && look.scale > 0f ? look.scale : 1f);
        public UnitSounds Sounds { get; set; }

        static readonly int WalkParam = Animator.StringToHash("Walk");

        Animator animator;
        UnitMotion motion;
        Transform model;
        GameObject coffin, ring;
        Mesh ringMesh;
        ArtCatalog art;
        UnitLook look, own;
        bool armed;
        float yaw;

        // armed: false for spawns and corpses, which lost their weapons (docs/art/unit-visuals.md 1).
        // inCoffin: a sleeping vampire is only its coffin until it wakes (docs/art/unit-visuals.md 5).
        public static UnitView Create(ArtCatalog art, UnitLook look, Transform parent, int id, Vector3 at, float yaw, bool armed, bool inCoffin = false)
        {
            var root = new GameObject("Unit " + id);
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            var view = root.AddComponent<UnitView>();
            view.Id = id;
            view.art = art;
            view.look = view.own = look;
            view.armed = armed;
            view.yaw = yaw;

            if (inCoffin)
            {
                view.Lying = true;
                view.coffin = Coffin(art, root.transform);
            }
            else view.BuildModel();
            return view;
        }

        // null: back to its own look.
        public void Shift(UnitLook to)
        {
            look = to != null ? to : own;
            if (model == null) return;
            yaw = model.localEulerAngles.y;
            BoardView.Kill(model.gameObject);
            BuildModel();
        }

        void BuildModel()
        {
            var prefab = look.Model;
            model = Instantiate(prefab, transform).transform;
            model.name = prefab.name;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.Euler(0f, yaw, 0f);
            FitHeight(model, Height);
            if (armed) Arm(model, look);
            animator = model.GetComponentInChildren<Animator>();
            if (animator == null) return;
            animator.runtimeAnimatorController = !armed && look.rig == Rig.Sidekick && art.spawnAnimator != null ? art.spawnAnimator
                : look.animator != null ? look.animator : art.unitAnimator;
            animator.applyRootMotion = true;
            motion = animator.gameObject.AddComponent<UnitMotion>();
            motion.model = model;
        }

        static void Arm(Transform model, UnitLook look)
        {
            if (look.weapons == null) return;
            var bones = model.GetComponentsInChildren<Transform>(true);
            foreach (var g in look.weapons)
            {
                if (g.prefab == null) continue;
                var name = UnitLook.Bone(look.rig, g.hand);
                var bone = System.Array.Find(bones, b => b.name == name);
                if (bone == null) { Debug.LogWarning(look.name + ": 뼈 " + name + " 이 없어 " + g.prefab.name + " 를 붙이지 못했다"); continue; }
                var w = Instantiate(g.prefab, bone, false).transform;
                var rotation = UnitLook.Socket(look.rig, g.hand, g.hold) * Quaternion.Euler(g.rotation);
                w.localRotation = rotation;
                w.localPosition = Palm(look.rig, bone) + g.position - rotation * GripPoint(w, g.hold);
                foreach (var c in w.GetComponentsInChildren<Collider>(true)) BoardView.Kill(c);
            }
        }

        // Polygon Hand_L/Hand_R sit at the wrist joint, so a weapon on the bone origin is held by the wrist; Sidekick prop bones already sit in the palm.
        public const float PalmShare = 0.6f;

        public static Vector3 Palm(Rig rig, Transform hand)
        {
            if (rig != Rig.Polygon) return Vector3.zero;
            foreach (Transform c in hand) if (c.name.StartsWith("Finger_01")) return c.localPosition * PalmShare;
            return Vector3.zero;
        }

        // Where the hand closes, in the weapon's own space: its pivot, or a share up the shaft for poles.
        static Vector3 GripPoint(Transform weapon, Hold hold)
        {
            if (hold != Hold.Pole && hold != Hold.Spear) return Vector3.zero;
            float min = float.MaxValue, max = float.MinValue;
            foreach (var mf in weapon.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var y = weapon.InverseTransformPoint(mf.transform.TransformPoint(corner)).y;
                    min = Mathf.Min(min, y);
                    max = Mathf.Max(max, y);
                }
            }
            return min > max ? Vector3.zero : Vector3.up * Mathf.Lerp(min, max, UnitLook.PoleGrip);
        }

        static GameObject Coffin(ArtCatalog art, Transform parent)
        {
            var c = Instantiate(art.coffin, parent, false);
            c.name = "Coffin";
            foreach (var col in c.GetComponentsInChildren<Collider>(true)) BoardView.Kill(col);
            if (!BoardView.Drawn(c.transform, out var b)) return c;
            if (b.size.x > b.size.z) c.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var length = Mathf.Max(b.size.x, b.size.z);
            if (length > 0.01f) c.transform.localScale *= art.unitHeight * art.hexSize / length;
            return c;
        }

        // docs/art/ui-style.md 8-1 8: a side-coloured ring on the ground under a standing unit.
        public const float RingOuter = 0.42f, RingInner = 0.3f, RingAlpha = 0.8f, RingLift = 0.05f;

        public void Ring(Color side)
        {
            if (art.overlay == null) return;
            const int sides = 40;
            var verts = new Vector3[sides * 2];
            var colors = new Color[sides * 2];
            var tris = new int[sides * 6];
            var c = Screens.Ui.Alpha(side, RingAlpha).linear;
            for (var i = 0; i < sides; i++)
            {
                var a = i * Mathf.PI * 2f / sides;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * art.hexSize;
                verts[i * 2] = d * RingInner + Vector3.up * RingLift;
                verts[i * 2 + 1] = d * RingOuter + Vector3.up * RingLift;
                colors[i * 2] = colors[i * 2 + 1] = c;
                int k = i * 2, n = (i + 1) % sides * 2;
                System.Array.Copy(new[] { k, n, k + 1, k + 1, n, n + 1 }, 0, tris, i * 6, 6);
            }
            ringMesh = new Mesh { name = "Unit Ring", vertices = verts, colors = colors, triangles = tris };
            ring = new GameObject("Ring");
            ring.transform.SetParent(transform, false);
            ring.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            var mr = ring.AddComponent<MeshRenderer>();
            mr.sharedMaterial = art.overlay;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.SetActive(!Lying);
        }

        // Each ring carries its side's vertex colour, so the mesh is the unit's own.
        void OnDestroy()
        {
            if (ringMesh == null) return;
            if (Application.isPlaying) Destroy(ringMesh); else DestroyImmediate(ringMesh);
        }

        static void FitHeight(Transform t, float height)
        {
            if (BoardView.Drawn(t, out var b) && b.size.y > 0.01f) t.localScale *= height / b.size.y;
        }

        public void SetHp(int hp, int max)
        {
            HpFraction = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
        }

        public void SetWalking(bool walking)
        {
            if (motion != null) motion.walking = walking;
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetBool(WalkParam, walking);
        }

        public void Trigger(string name)
        {
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(name);
        }

        readonly System.Collections.Generic.Stack<Quaternion> facingBeforeWalk = new System.Collections.Generic.Stack<Quaternion>();

        // battle-hud.md 3-8: an undone move puts the unit back facing the way it did before the walk.
        public void WalkStarts() { if (model != null) facingBeforeWalk.Push(model.rotation); }

        public void FaceBack() { if (model != null && facingBeforeWalk.Count > 0) model.rotation = facingBeforeWalk.Pop(); }

        public void Face(Vector3 worldTarget)
        {
            if (model == null) return;
            var d = worldTarget - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) model.rotation = Quaternion.LookRotation(d);
        }

        public void LieDown()
        {
            Lying = true;
            if (ring != null) ring.SetActive(false);
            if (animator != null) animator.enabled = false;
            model.localRotation = Quaternion.Euler(-90f, model.localEulerAngles.y, 0f);
            model.localPosition = Vector3.up * 0.25f;
        }

        public void StandUp()
        {
            Lying = false;
            if (ring != null) ring.SetActive(true);
            if (coffin != null) { BoardView.Kill(coffin); coffin = null; }
            if (model == null) { BuildModel(); return; }
            model.localRotation = Quaternion.Euler(0f, model.localEulerAngles.y, 0f);
            model.localPosition = Vector3.zero;
            if (animator != null) animator.enabled = true;
        }
    }
}
