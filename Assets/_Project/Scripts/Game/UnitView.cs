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
        Transform model;
        MeshRenderer ring;
        Color ringColor;
        public bool Done { get; private set; }
        GameObject coffin;
        ArtCatalog art;

        // armed: false for spawns and corpses, which lost their weapons (docs/art/unit-visuals.md 1).
        public static UnitView Create(ArtCatalog art, UnitLook look, Transform parent, int id, Vector3 at, float yaw, Color ring, bool armed)
        {
            var root = new GameObject("Unit " + id);
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            var view = root.AddComponent<UnitView>();
            view.Id = id;
            view.art = art;

            view.model = Instantiate(look.model, root.transform).transform;
            view.model.name = look.model.name;
            view.model.localPosition = Vector3.zero;
            view.model.localRotation = Quaternion.Euler(0f, yaw, 0f);
            FitHeight(view.model, art.unitHeight * art.hexSize);
            if (armed) Arm(view.model, look);
            view.animator = view.model.GetComponentInChildren<Animator>();
            if (view.animator != null)
            {
                view.animator.runtimeAnimatorController = !armed && look.rig == Rig.Sidekick && art.spawnAnimator != null ? art.spawnAnimator
                    : look.animator != null ? look.animator : art.unitAnimator;
                view.animator.applyRootMotion = false;
            }

            var r = Quad(art, root.transform, "Ring", ring * 0.55f, new Vector3(art.hexSize * 1.05f, 0.015f, art.hexSize * 1.05f), PrimitiveType.Cylinder);
            r.localPosition = Vector3.up * 0.01f;
            view.ring = r.GetComponent<MeshRenderer>();
            view.ringColor = ring * 0.55f;
            return view;
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
                w.localPosition = g.position;
                w.localRotation = Quaternion.Euler(g.rotation);
                foreach (var c in w.GetComponentsInChildren<Collider>(true)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            }
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
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetBool(WalkParam, walking);
        }

        public void Trigger(string name)
        {
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(name);
        }

        public void Face(Vector3 worldTarget)
        {
            var d = worldTarget - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) model.rotation = Quaternion.LookRotation(d);
        }

        public void LieDown(bool inCoffin)
        {
            Lying = true;
            if (animator != null) animator.enabled = false;
            model.localRotation = Quaternion.Euler(-90f, model.localEulerAngles.y, 0f);
            model.localPosition = Vector3.up * 0.25f;
            if (inCoffin && coffin == null)
                coffin = Quad(art, transform, "Coffin", art.coffin, new Vector3(0.8f, 0.3f, art.unitHeight * art.hexSize), PrimitiveType.Cube).gameObject;
        }

        public void StandUp()
        {
            Lying = false;
            if (coffin != null) { if (Application.isPlaying) Destroy(coffin); else DestroyImmediate(coffin); coffin = null; }
            model.localRotation = Quaternion.Euler(0f, model.localEulerAngles.y, 0f);
            model.localPosition = Vector3.zero;
            if (animator != null) animator.enabled = true;
        }
    }
}
