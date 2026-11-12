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
            [Mark.Hold] = S(Screens.Ui.Candle, 0.12f, Screens.Ui.Candle, 1f, R - 1f, 2f),
            [Mark.Goal] = S(Ally, 0.12f, Ally, 1f, R - 1f, 2f),
            [Mark.Arrive] = S(Enemy, 0.1f, Enemy, 1f, R - 2f, 2f, 4f, 4f),
            [Mark.Reinforce] = S(Ally, 0.06f, Ally, 1f, R - 2f, 2f, 4f, 4f),
            [Mark.Trigger] = S(Screens.Ui.Candle, 0.08f, Screens.Ui.Candle, 1f, R - 2f, 2f, 4f, 4f),
        };
        // ui-style.md 8-2 role icons: colour alone did not tell these cells apart (#101).
        static readonly Dictionary<Mark, (string Icon, Color Tint)> Icons = new Dictionary<Mark, (string, Color)>
        {
            [Mark.Deploy] = (Screens.Kit.Icon.Pop, Ally),
            [Mark.Reinforce] = (Screens.Kit.Icon.Up, Ally),
            [Mark.Arrive] = (Screens.Kit.Icon.Skulls, Enemy),
            [Mark.Hold] = (Screens.Kit.Icon.Home, Screens.Ui.Candle),
            [Mark.Goal] = (Screens.Kit.Icon.Flag, Ally),
            [Mark.Trigger] = (Screens.Kit.Icon.Star, Screens.Ui.Candle),
        };
        readonly List<SpriteRenderer> icons = new List<SpriteRenderer>();
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
            var shown = 0;
            foreach (var m in marks)
            {
                Hexagon(Center(m.Hex), Styles[m.Mark]);
                if (Icons.TryGetValue(m.Mark, out var ic)) Icon(shown++, m.Hex, ic.Icon, ic.Tint);
            }
            for (var i = shown; i < icons.Count; i++) icons[i].enabled = false;
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
            foreach (var ic in icons) ic.enabled = false;
            MarkCount = 0;
        }

        Vector3 Center(Hex h) => transform.InverseTransformPoint(board.HexToWorld(h));

        void Icon(int i, Hex h, string name, Color tint)
        {
            if (i == icons.Count)
            {
                var go = new GameObject("Icon");
                go.transform.SetParent(transform, false);
                icons.Add(go.AddComponent<SpriteRenderer>());
            }
            var sr = icons[i];
            sr.sprite = board.art.Ui(name);
            sr.color = A(tint, 0.85f);
            sr.enabled = sr.sprite != null;
            if (!sr.enabled) return;
            var size = 0.9f * board.art.hexSize;
            var t = sr.transform;
            t.position = board.OnHologram(board.HexToWorld(h), Lift + 0.01f);
            t.rotation = Quaternion.Euler(90f, 0f, 0f);
            var b = sr.sprite.bounds.size;
            t.localScale = Vector3.one * (size / Mathf.Max(b.x, b.y)) / transform.lossyScale.x;
        }

        void Hexagon(Vector3 c, Style s)
        {
            var r = s.Radius * Scale;
            if (s.Fill.a > 0f) lines.Fill(c, r, s.Fill);
            if (s.Width > 0f && s.Stroke.a > 0f) lines.Outline(c, r, s.Width * Scale, s.Stroke, s.Dash * Scale, s.Gap * Scale);
        }
    }
}
