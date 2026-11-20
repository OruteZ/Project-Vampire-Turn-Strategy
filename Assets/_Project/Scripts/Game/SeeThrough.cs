using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VampireSpawn.Game.Screens;

namespace VampireSpawn.Game
{
    // docs/art/see-through.md: stage props in front of the focus open in a dithered hole; T cycles the focus mode.
    public sealed class SeeThrough : MonoBehaviour
    {
        public enum Mode { Off, Cursor, Units, Tiles }

        const float WorldRadius = 1.8f, ChestRaise = 0.6f, LabelSeconds = 1.5f;
        const int MaxPoints = 64; // SEE_THROUGH_MAX in SeeThrough.hlsl
        static readonly string[] Names = { "벽 투과: 끔", "벽 투과: 커서", "벽 투과: 유닛", "벽 투과: 타일" };

        static readonly int Points = Shader.PropertyToID("_SeeThroughPoints"), Count = Shader.PropertyToID("_SeeThroughCount"),
            BoardRect = Shader.PropertyToID("_SeeThroughBoard"), Floor = Shader.PropertyToID("_SeeThroughFloor");
        static readonly Dictionary<Material, Material> swapped = new Dictionary<Material, Material>();
        static Shader shader;

        public static Mode Current
        {
            get => (Mode)PlayerPrefs.GetInt("seeThroughMode", (int)Mode.Cursor);
            set => PlayerPrefs.SetInt("seeThroughMode", (int)value);
        }

        readonly Vector4[] points = new Vector4[MaxPoints];
        readonly KeyEdges keys = new KeyEdges();
        BoardInput input;
        float labelUntil;

        void Start()
        {
            input = FindAnyObjectByType<BoardInput>();
            Swap(gameObject);
        }

        void OnDisable()
        {
            Shader.SetGlobalFloat(Count, 0f);
            Shader.SetGlobalVector(BoardRect, Vector4.zero);
        }

        void Update()
        {
            if (Keyboard.current == null || !keys.Pressed("t", Keyboard.current.tKey.isPressed)) return;
            Current = (Mode)(((int)Current + 1) % Names.Length);
            labelUntil = Time.unscaledTime + LabelSeconds;
        }

        void OnGUI()
        {
            if (Time.unscaledTime > labelUntil) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height / 30f), alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(Screen.width * 0.35f, Screen.height * 0.15f, Screen.width * 0.3f, Screen.height / 15f), Names[(int)Current], style);
        }

        void LateUpdate()
        {
            var cam = input != null ? input.viewCamera : null;
            var session = input != null ? input.Session : null;
            var n = 0;
            var rect = Vector4.zero;
            if (cam != null && session != null)
            {
                var board = input.battle.board;
                switch (Current)
                {
                    case Mode.Cursor:
                        if (session.Selected is int id && input.battle.ViewOf(id) is UnitView v) Add(cam, v.transform.position, ref n);
                        if (Mouse.current != null && input.HexAt(Mouse.current.position.ReadValue()) is Core.Hex h) Add(cam, board.Stand(h), ref n);
                        break;
                    case Mode.Units:
                        foreach (var u in session.Battle.Units)
                            if (input.battle.ViewOf(u.Id) is UnitView uv) Add(cam, uv.transform.position, ref n);
                        break;
                    case Mode.Tiles:
                        rect = BoardBounds(board);
                        Shader.SetGlobalFloat(Floor, board.HologramFloor);
                        break;
                }
            }
            Shader.SetGlobalVectorArray(Points, points);
            Shader.SetGlobalFloat(Count, n);
            Shader.SetGlobalVector(BoardRect, rect);
        }

        void Add(Camera cam, Vector3 feet, ref int n)
        {
            if (n >= MaxPoints) return;
            var p = cam.WorldToViewportPoint(feet + Vector3.up * ChestRaise);
            if (p.z <= 0f) return;
            var radius = WorldRadius / (2f * p.z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            points[n++] = new Vector4(p.x, p.y, p.z, radius);
        }

        // ponytail: the board's bounding box, so gap cells at its edge open too; a per-cell mask if that reads wrong.
        static Vector4 BoardBounds(BoardView board)
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (var h in board.Board.Cells)
            {
                var w = board.HexToWorld(h);
                x0 = Mathf.Min(x0, w.x); z0 = Mathf.Min(z0, w.z); x1 = Mathf.Max(x1, w.x); z1 = Mathf.Max(z1, w.z);
            }
            var pad = board.art.hexSize;
            return new Vector4(x0 - pad, z0 - pad, x1 + pad, z1 + pad);
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
