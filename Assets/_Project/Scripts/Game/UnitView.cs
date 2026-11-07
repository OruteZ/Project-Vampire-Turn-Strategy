using UnityEngine;

namespace VampireSpawn.Game
{
    public sealed class UnitView : MonoBehaviour
    {
        public int Id { get; private set; }
        public float HpFraction { get; private set; } = 1f;
        public bool Lying { get; private set; }

        static readonly int WalkParam = Animator.StringToHash("Walk");
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        Animator animator;
        UnitMotion motion;
        Transform model;
        MeshRenderer ring;
        Color ringColor;
        public bool Done { get; private set; }
        GameObject coffin;
        ArtCatalog art;
        UnitLook look;
        bool armed;
        float yaw;

        // armed: false for spawns and corpses, which lost their weapons (docs/art/unit-visuals.md 1).
        // inCoffin: a sleeping vampire is only its coffin until it wakes (docs/art/unit-visuals.md 5).
        public static UnitView Create(ArtCatalog art, UnitLook look, Transform parent, int id, Vector3 at, float yaw, Color ring, bool armed, bool inCoffin = false)
        {
            var root = new GameObject("Unit " + id);
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            var view = root.AddComponent<UnitView>();
            view.Id = id;
            view.art = art;
            view.look = look;
            view.armed = armed;
            view.yaw = yaw;

            if (inCoffin)
            {
                view.Lying = true;
                view.coffin = Quad(art, root.transform, "Coffin", art.coffin, new Vector3(0.8f, 0.3f, art.unitHeight * art.hexSize), PrimitiveType.Cube).gameObject;
            }
            else view.BuildModel();

            var r = Quad(art, root.transform, "Ring", ring * 0.55f, new Vector3(art.hexSize * 1.05f, 0.015f, art.hexSize * 1.05f), PrimitiveType.Cylinder);
            r.localPosition = Vector3.up * 0.01f;
            view.ring = r.GetComponent<MeshRenderer>();
            view.ringColor = ring * 0.55f;
            return view;
        }

        void BuildModel()
        {
            var prefab = look.Model;
            model = Instantiate(prefab, transform).transform;
            model.name = prefab.name;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.Euler(0f, yaw, 0f);
            FitHeight(model, art.unitHeight * art.hexSize);
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
                w.localPosition = g.position - rotation * GripPoint(w, g.hold);
                foreach (var c in w.GetComponentsInChildren<Collider>(true)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            }
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

        static Transform Quad(ArtCatalog art, Transform parent, string name, Color color, Vector3 scale, PrimitiveType shape)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = art.flat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            mr.SetPropertyBlock(block);
            return go.transform;
        }

        static void FitHeight(Transform t, float height)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            if (b.size.y > 0.01f) t.localScale *= height / b.size.y;
        }

        // Prototype .tk.done dims an ally that has acted this turn.
        public void SetDone(bool done)
        {
            if (Done == done || ring == null) return;
            Done = done;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, done ? ringColor * 0.3f : ringColor);
            ring.SetPropertyBlock(block);
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
            if (animator != null) animator.enabled = false;
            model.localRotation = Quaternion.Euler(-90f, model.localEulerAngles.y, 0f);
            model.localPosition = Vector3.up * 0.25f;
        }

        public void StandUp()
        {
            Lying = false;
            if (coffin != null) { if (Application.isPlaying) Destroy(coffin); else DestroyImmediate(coffin); coffin = null; }
            if (model == null) { BuildModel(); return; }
            model.localRotation = Quaternion.Euler(0f, model.localEulerAngles.y, 0f);
            model.localPosition = Vector3.zero;
            if (animator != null) animator.enabled = true;
        }
    }
}
