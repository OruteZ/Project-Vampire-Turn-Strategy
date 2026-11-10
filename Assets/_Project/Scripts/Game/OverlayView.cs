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

        static readonly Color Ally = Screens.Ui.Steel, Enemy = Screens.Ui.Blood, Bone = Screens.Ui.Bone, MoonBlood = Screens.Ui.MoonBlood, Spawn = Screens.Ui.Spawn;

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
            [Mark.Bury] = S(Spawn, 0f, Spawn, 1f, R - 3f, 2f, 4f, 4f),
            [Mark.Mend] = S(Ally, 0f, Ally, 1f, R - 3f, 2f, 4f, 4f),
            [Mark.Sel] = S(Ally, 0f, Ally, 1f, R - 3f, 3f),
            [Mark.MarkZone] = S(MoonBlood, 0.05f, MoonBlood, 1f, R - 2f, 1f),
            [Mark.Target] = S(Enemy, 0f, Enemy, 1f, R - 1.5f, 3f),
        };
        static readonly Style HoverCell = S(Bone, 0f, Bone, 1f, R - 1f, 2f);
        static readonly Style DropIn = S(Ally, 0.25f, Ally, 1f, R - 2.5f, 2.5f);
        static readonly Style DropOut = S(Enemy, 0.25f, Enemy, 1f, R - 2.5f, 2.5f);

        HexLines lines;
        Mesh mesh;

        public int MarkCount { get; private set; }

        BattleSession last;
        Hex? lastDrop, hover;
        static Color A(Color c, float a) { c.a = a; return c; }

        float Scale => board.art.hexSize / R;
        // Above the board's grid lines (battle-terrain.md 3).
        const float Lift = 0.06f;

        void Awake()
        {
            mesh = new Mesh { name = "Overlay" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sharedMaterial = material;
            GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lines = new HexLines(p => transform.InverseTransformPoint(board.OnHologram(transform.TransformPoint(p), Lift)));
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
            lines.Clear();
            var marks = session.Overlay();
            MarkCount = marks.Count;
            foreach (var m in marks) Hexagon(Center(m.Hex), Styles[m.Mark]);
            var moon = session.Moon();
            if (moon != null && moon.Target is Hex t) lines.Dashed(Center(moon.Vampire), Center(t), 2f * Scale, MoonBlood, 4f * Scale, 4f * Scale);
            if (hover is Hex hv && session.Battle.Board.InBounds(hv)) Hexagon(Center(hv), HoverCell);
            if (drop is Hex d && session.Battle.Board.InBounds(d)) Hexagon(Center(d), session.Battle.InDeploy(d) ? DropIn : DropOut);
            lines.Apply(mesh);
        }

        public void Clear()
        {
            last = null;
            hover = null;
            mesh.Clear();
            MarkCount = 0;
        }

        Vector3 Center(Hex h) => transform.InverseTransformPoint(board.HexToWorld(h));

        void Hexagon(Vector3 c, Style s)
        {
            var r = s.Radius * Scale;
            if (s.Fill.a > 0f) lines.Fill(c, r, s.Fill);
            if (s.Width > 0f && s.Stroke.a > 0f) lines.Outline(c, r, s.Width * Scale, s.Stroke, s.Dash * Scale, s.Gap * Scale);
        }
    }
}
