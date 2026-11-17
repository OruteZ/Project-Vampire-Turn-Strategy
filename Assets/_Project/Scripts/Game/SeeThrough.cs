using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VampireSpawn.Game
{
    // docs/art/see-through.md: stage props in front of the selected unit and the cursor cell open in a dithered hole.
    public sealed class SeeThrough : MonoBehaviour
    {
        const float WorldRadius = 1.8f, ChestRaise = 0.6f;

        static readonly int A = Shader.PropertyToID("_SeeThroughA"), B = Shader.PropertyToID("_SeeThroughB");
        static readonly Dictionary<Material, Material> swapped = new Dictionary<Material, Material>();
        static Shader shader;

        BoardInput input;

        void Start()
        {
            input = FindAnyObjectByType<BoardInput>();
            Swap(gameObject);
        }

        void OnDisable()
        {
            Shader.SetGlobalVector(A, Vector4.zero);
            Shader.SetGlobalVector(B, Vector4.zero);
        }

        void LateUpdate()
        {
            var cam = input != null ? input.viewCamera : null;
            var session = input != null ? input.Session : null;
            Vector3? unit = null, cursor = null;
            if (cam != null && session != null)
            {
                if (session.Selected is int id && input.battle.ViewOf(id) is UnitView v) unit = v.transform.position + Vector3.up * ChestRaise;
                if (Mouse.current != null && input.HexAt(Mouse.current.position.ReadValue()) is Core.Hex h) cursor = input.battle.board.Stand(h) + Vector3.up * ChestRaise;
            }
            Shader.SetGlobalVector(A, Focus(cam, unit));
            Shader.SetGlobalVector(B, Focus(cam, cursor));
        }

        static Vector4 Focus(Camera cam, Vector3? at)
        {
            if (at == null) return Vector4.zero;
            var p = cam.WorldToViewportPoint(at.Value);
            if (p.z <= 0f) return Vector4.zero;
            var radius = WorldRadius / (2f * p.z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            return new Vector4(p.x, p.y, p.z, radius);
        }

        // Synty props share a few POLYGON Generic_Basic materials; each gets one SeeThroughLit twin, the asset itself is never touched.
        public static void Swap(GameObject root)
        {
            if (shader == null) shader = Resources.Load<Shader>("Shaders/SeeThroughLit");
            if (shader == null) return;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                    if (mats[i] != null && Twin(mats[i]) is Material t) { mats[i] = t; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static Material Twin(Material src)
        {
            if (swapped.TryGetValue(src, out var t)) return t;
            if (!src.HasProperty("_Albedo_Map")) return null;
            t = new Material(shader) { name = src.name + " (SeeThrough)" };
            t.SetTexture("_BaseMap", src.GetTexture("_Albedo_Map"));
            if (src.HasProperty("_BaseColor")) t.SetColor("_BaseColor", src.GetColor("_BaseColor"));
            if (src.HasProperty("_Smoothness")) t.SetFloat("_Smoothness", src.GetFloat("_Smoothness"));
            if (src.HasProperty("_Metallic")) t.SetFloat("_Metallic", src.GetFloat("_Metallic"));
            if (src.HasProperty("_Normal_Map") && src.GetTexture("_Normal_Map") is Texture n)
            {
                t.SetTexture("_BumpMap", n);
                if (src.HasProperty("_Normal_Amount")) t.SetFloat("_BumpScale", src.GetFloat("_Normal_Amount"));
                t.EnableKeyword("_NORMALMAP");
            }
            if (src.HasProperty("_Alpha_Clip_Threshold") && src.GetFloat("_Alpha_Clip_Threshold") > 0f)
            {
                t.SetFloat("_AlphaClip", 1f);
                t.SetFloat("_Cutoff", src.GetFloat("_Alpha_Clip_Threshold"));
                t.EnableKeyword("_ALPHATEST_ON");
            }
            if (src.HasProperty("_Enable_Emission") && src.GetFloat("_Enable_Emission") > 0f)
            {
                t.SetTexture("_EmissionMap", src.GetTexture("_Emission_Map"));
                t.SetColor("_EmissionColor", src.GetColor("_Emission_Color"));
                t.EnableKeyword("_EMISSION");
            }
            swapped[src] = t;
            return t;
        }
    }
}
