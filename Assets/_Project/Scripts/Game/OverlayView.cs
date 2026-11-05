using System.Collections.Generic;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class OverlayView : MonoBehaviour
    {
        public BoardView board;
        public Material material;

        // Sizes are the prototype's SVG units, where the hex corner radius R is 30.
        const float R = 30f;

        static readonly Color Steel = Hexc(0x8fb0cc), Candle = Hexc(0xe3a84b), Blood = Hexc(0xc8304a), Bone = Hexc(0xebe3d2);

        struct Style
        {
            public Color Fill, Stroke;
            public float Radius, Width, Dash, Gap;
        }

        static Style S(Color fill, float fa, Color stroke, float sa, float radius, float width, float dash = 0f, float gap = 0f) =>
            new Style { Fill = A(fill, fa), Stroke = A(stroke, sa), Radius = radius, Width = width, Dash = dash, Gap = gap };

        // Prototype CSS .ov-* rules and the radius each renderOverlay call passes.
        static readonly Dictionary<Mark, Style> Styles = new Dictionary<Mark, Style>
        {
            [Mark.Deploy] = S(Steel, 0.11f, Steel, 0.35f, R - 2.5f, 1f),
            [Mark.Threat] = S(Blood, 0.13f, Blood, 0f, R - 1f, 0f),
            [Mark.Move] = S(Candle, 0.2f, Candle, 0.45f, R - 2.5f, 1f),
            [Mark.Atk] = S(Blood, 0.14f, Blood, 1f, R - 4f, 2.5f),
            [Mark.Exec] = S(Bone, 0f, Bone, 1f, R - 4f, 2.5f, 4f, 4f),
            [Mark.Sel] = S(Candle, 0f, Candle, 1f, R - 3f, 3f),
            [Mark.MarkZone] = S(Blood, 0.05f, Blood, 0.45f, R - 2f, 1.2f, 3f, 4f),
            [Mark.Target] = S(Blood, 0f, Blood, 1f, R - 1.5f, 3f, 7f, 5f),
        };
        static readonly Style DropIn = S(Candle, 0.25f, Candle, 1f, R - 2.5f, 2.5f);
        static readonly Style DropOut = S(Blood, 0.25f, Blood, 1f, R - 2.5f, 2.5f);

        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> tris = new List<int>();
        readonly List<TextMesh> labels = new List<TextMesh>();
        Mesh mesh;

        public int MarkCount { get; private set; }

        static Color Hexc(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        static Color A(Color c, float a) { c.a = a; return c; }

        float Scale => board.art.hexSize / R;
        float Lift => board.TileTop + 0.03f;

        void Awake()
        {
            mesh = new Mesh { name = "Overlay" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sharedMaterial = material;
            GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void Show(BattleSession session, Hex? drop = null)
        {
            verts.Clear(); colors.Clear(); tris.Clear();
            var used = 0;
            var marks = session.Overlay();
            MarkCount = marks.Count;
            foreach (var m in marks) Hexagon(Center(m.Hex), Styles[m.Mark]);
            var moon = session.Moon();
            if (moon != null)
            {
                Label(used++, Center(moon.Vampire), moon.VampireLabel);
                if (moon.Target is Hex t)
                {
                    Dashed(Center(moon.Vampire), Center(t), 1.6f * Scale, A(Blood, 0.75f), 3f * Scale, 5f * Scale);
                    Label(used++, Center(t), moon.TargetLabel);
                }
            }
            if (drop is Hex d && session.Battle.Board.InBounds(d)) Hexagon(Center(d), session.Battle.InDeploy(d) ? DropIn : DropOut);
            for (var i = used; i < labels.Count; i++) labels[i].gameObject.SetActive(false);
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        public void Clear()
        {
            mesh.Clear();
            MarkCount = 0;
            foreach (var l in labels) l.gameObject.SetActive(false);
        }

        Vector3 Center(Hex h)
        {
            var p = transform.InverseTransformPoint(board.HexToWorld(h));
            p.y = Lift;
            return p;
        }

        static Vector3 Corner(Vector3 c, float r, int i)
        {
            var a = Mathf.Deg2Rad * 60f * (i % 6);
            return c + new Vector3(r * Mathf.Cos(a), 0f, r * Mathf.Sin(a));
        }

        void Hexagon(Vector3 c, Style s)
        {
            var r = s.Radius * Scale;
            if (s.Fill.a > 0f)
            {
                var k = verts.Count;
                verts.Add(c); colors.Add(s.Fill.linear);
                for (var i = 0; i < 6; i++) { verts.Add(Corner(c, r, i)); colors.Add(s.Fill.linear); }
                for (var i = 0; i < 6; i++) tris.AddRange(new[] { k, k + 1 + (i + 1) % 6, k + 1 + i });
            }
            if (s.Width <= 0f || s.Stroke.a <= 0f) return;
            for (var i = 0; i < 6; i++)
            {
                var a = Corner(c, r, i);
                var b = Corner(c, r, i + 1);
                if (s.Dash > 0f) Dashed(a, b, s.Width * Scale, s.Stroke, s.Dash * Scale, s.Gap * Scale);
                else Segment(a, b, s.Width * Scale, s.Stroke);
            }
        }

        void Dashed(Vector3 a, Vector3 b, float width, Color color, float dash, float gap)
        {
            var len = Vector3.Distance(a, b);
            var dir = (b - a) / Mathf.Max(len, 0.0001f);
            for (var t = 0f; t < len; t += dash + gap) Segment(a + dir * t, a + dir * Mathf.Min(len, t + dash), width, color);
        }

        void Segment(Vector3 a, Vector3 b, float width, Color color)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-8f) return;
            var side = Vector3.Cross(Vector3.up, d.normalized) * (width * 0.5f);
            var along = d.normalized * (width * 0.5f);
            var k = verts.Count;
            verts.Add(a - side - along); verts.Add(a + side - along); verts.Add(b + side + along); verts.Add(b - side + along);
            for (var i = 0; i < 4; i++) colors.Add(color.linear);
            tris.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        }

        void Label(int index, Vector3 at, string text)
        {
            while (labels.Count <= index)
            {
                var go = new GameObject("MarkLabel");
                go.transform.SetParent(transform, false);
                var tm = go.AddComponent<TextMesh>();
                tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
                tm.fontSize = 48;
                tm.characterSize = 0.07f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = Blood;
                labels.Add(tm);
            }
            var l = labels[index];
            l.gameObject.SetActive(true);
            l.text = text;
            l.transform.localPosition = at + Vector3.up * (board.art.unitHeight * board.art.hexSize + 0.55f);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var l in labels) if (l.gameObject.activeSelf) l.transform.rotation = cam.transform.rotation;
        }
    }
}
