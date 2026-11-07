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

        static readonly Color Ally = Screens.Ui.Steel, Enemy = Screens.Ui.Blood, Bone = Screens.Ui.Bone, MoonBlood = Screens.Ui.MoonBlood;

        struct Style
        {
            public Color Fill, Stroke;
            public float Radius, Width, Dash, Gap;
        }

        static Style S(Color fill, float fa, Color stroke, float sa, float radius, float width, float dash = 0f, float gap = 0f) =>
            new Style { Fill = A(fill, fa), Stroke = A(stroke, sa), Radius = radius, Width = width, Dash = dash, Gap = gap };

        // docs/art/ui-style.md 8-2. Widths are in R units; one unit is about 1 px at 1080p.
        static readonly Dictionary<Mark, Style> Styles = new Dictionary<Mark, Style>
        {
            [Mark.Deploy] = S(Ally, 0.08f, Ally, 1f, R - 2.5f, 2f),
            [Mark.Move] = S(Ally, 0.08f, Ally, 1f, R - 2.5f, 2f),
            [Mark.Atk] = S(Enemy, 0f, Enemy, 1f, R - 3f, 2f),
            [Mark.Exec] = S(Enemy, 0f, Enemy, 1f, R - 3f, 2f, 4f, 4f),
            [Mark.Sel] = S(Ally, 0f, Ally, 1f, R - 3f, 3f),
            [Mark.MarkZone] = S(MoonBlood, 0.05f, MoonBlood, 1f, R - 2f, 1f),
            [Mark.Target] = S(Enemy, 0f, Enemy, 1f, R - 1.5f, 3f),
        };
        static readonly Style HoverCell = S(Bone, 0f, Bone, 1f, R - 1f, 2f);
        static readonly Style DropIn = S(Ally, 0.25f, Ally, 1f, R - 2.5f, 2.5f);
        static readonly Style DropOut = S(Enemy, 0.25f, Enemy, 1f, R - 2.5f, 2.5f);

        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> tris = new List<int>();
        Mesh mesh;

        public int MarkCount { get; private set; }

        BattleSession last;
        Hex? lastDrop, hover;
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

        public void SetHover(Hex? h)
        {
            if (h == hover) return;
            hover = h;
            if (last != null) Show(last, lastDrop);
        }

        public void Show(BattleSession session, Hex? drop = null)
        {
            last = session;
            lastDrop = drop;
            verts.Clear(); colors.Clear(); tris.Clear();
            var marks = session.Overlay();
            MarkCount = marks.Count;
            foreach (var m in marks) Hexagon(Center(m.Hex), Styles[m.Mark]);
            var moon = session.Moon();
            if (moon != null && moon.Target is Hex t) Dashed(Center(moon.Vampire), Center(t), 2f * Scale, MoonBlood, 4f * Scale, 4f * Scale);
            if (hover is Hex hv && session.Battle.Board.InBounds(hv)) Hexagon(Center(hv), HoverCell);
            if (drop is Hex d && session.Battle.Board.InBounds(d)) Hexagon(Center(d), session.Battle.InDeploy(d) ? DropIn : DropOut);
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        public void Clear()
        {
            last = null;
            hover = null;
            mesh.Clear();
            MarkCount = 0;
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
    }
}
