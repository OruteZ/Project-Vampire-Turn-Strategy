using System.Collections.Generic;
using System.Linq;
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
            new Style { Fill = Screens.Ui.Alpha(fill, fa), Stroke = Screens.Ui.Alpha(stroke, sa), Radius = radius, Width = width, Dash = dash, Gap = gap };

        // docs/art/ui-style.md 8-2. Widths are in R units; one unit is about 1 px at 1080p.
        static readonly Dictionary<Mark, Style> Styles = new Dictionary<Mark, Style>
        {
            [Mark.Deploy] = S(Ally, 0.1f, Ally, 0f, R, 0f),
            [Mark.Move] = S(Ally, 0.08f, Ally, 1f, R - 2.5f, 2f),
            [Mark.Atk] = S(Enemy, 0f, Enemy, 1f, R - 3f, 2f),
            [Mark.Exec] = S(Enemy, 0f, Enemy, 1f, R - 3f, 2f, 4f, 4f),
            [Mark.Bury] = S(Spawn, 0f, Spawn, 1f, R - 3f, 2f, 4f, 4f),
            [Mark.Mend] = S(Ally, 0f, Ally, 1f, R - 3f, 2f, 4f, 4f),
            [Mark.Trap] = S(Ally, 0.05f, Ally, 1f, R - 6f, 1.5f, 3f, 3f),
            [Mark.TrapSpot] = S(Ally, 0f, Ally, 1f, R - 3f, 2f, 4f, 4f),
            [Mark.Sel] = S(Ally, 0f, Ally, 1f, R - 3f, 3f),
            [Mark.MarkZone] = S(MoonBlood, 0.05f, MoonBlood, 1f, R - 2f, 1f),
            [Mark.Target] = S(Enemy, 0f, Enemy, 1f, R - 1.5f, 3f),
            [Mark.Hold] = S(Screens.Ui.Candle, 0.12f, Screens.Ui.Candle, 1f, R - 1f, 2f),
            [Mark.Goal] = S(Ally, 0.12f, Ally, 1f, R - 1f, 2f),
            [Mark.Arrive] = S(Enemy, 0.1f, Enemy, 1f, R - 2f, 2f, 4f, 4f),
            [Mark.Reinforce] = S(Ally, 0.1f, Ally, 0f, R, 0f),
            [Mark.Trigger] = S(Screens.Ui.Candle, 0.08f, Screens.Ui.Candle, 1f, R - 2f, 2f, 4f, 4f),
        };
        // ui-style.md 8-2 role icons: colour alone did not tell these cells apart (#101).
        static readonly Dictionary<Mark, (string Icon, Color Tint)> Icons = new Dictionary<Mark, (string, Color)>
        {
            [Mark.Deploy] = (Screens.Kit.Icon.Pop, Ally),
            [Mark.Trap] = (Screens.Kit.Icon.Target, Ally),
            [Mark.Reinforce] = (Screens.Kit.Icon.Up, Ally),
            [Mark.Arrive] = (Screens.Kit.Icon.Skulls, Enemy),
            [Mark.Hold] = (Screens.Kit.Icon.Home, Screens.Ui.Candle),
            [Mark.Goal] = (Screens.Kit.Icon.Flag, Ally),
            [Mark.Trigger] = (Screens.Kit.Icon.Star, Screens.Ui.Candle),
        };
        readonly List<SpriteRenderer> icons = new List<SpriteRenderer>();

        // ui-style.md 8-2: ally zones are one outlined region; the role icon stays but lighter than the units on it.
        static readonly Mark[] Zones = { Mark.Deploy, Mark.Reinforce };
        readonly Dictionary<Mark, RegionOutline> zones = new Dictionary<Mark, RegionOutline>();
        static readonly Style HoverCell = S(Bone, 0f, Bone, 1f, R - 1f, 2f);
        static readonly Style DropIn = S(Ally, 0.25f, Ally, 1f, R - 2.5f, 2.5f);
        static readonly Style DropOut = S(Enemy, 0.25f, Enemy, 1f, R - 2.5f, 2.5f);

        HexLines lines;
        Mesh mesh;

        BattleSession last;
        Hex? lastDrop, hover;

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
            // Drawn over walls and units so the arc is never cut by the scenery it flies over.
            aimMaterial = new Material(material);
            aimMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            arc = AimLine("AimArc");
            arc.numCapVertices = 2;
            head = AimLine("AimHead");
            head.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            x1 = AimLine("AimX");
            x2 = AimLine("AimX");
        }

        void OnDestroy() => Destroy(aimMaterial);

        public void SetHover(Hex? h)
        {
            if (h == hover) return;
            hover = h;
            if (last != null) Draw();
        }

        List<MarkedHex> marks;
        MoonMark moon;

        public void Show(BattleSession session, Hex? drop = null)
        {
            last = session;
            lastDrop = drop;
            marks = session.Overlay();
            moon = session.Moon();
            Draw();
        }

        // Hover redraws the marks of the last Show: during playback the model is already at the turn's end (#125, #126).
        void Draw()
        {
            var session = last;
            var drop = lastDrop;
            lines.Clear();
            var shown = 0;
            var iconed = new HashSet<Hex>();
            foreach (var m in marks)
            {
                Hexagon(Center(m.Hex), Styles[m.Mark]);
                if (Icons.TryGetValue(m.Mark, out var ic) && iconed.Add(m.Hex)) Icon(shown++, m.Hex, ic.Icon, ic.Tint, System.Array.IndexOf(Zones, m.Mark) >= 0);
            }
            foreach (var z in Zones)
            {
                var cells = marks.Where(m => m.Mark == z).Select(m => m.Hex);
                if (zones.TryGetValue(z, out var o) && o != null) { o.Show(board, cells); continue; }
                zones[z] = RegionOutline.Create(board, z + "Zone", cells, RegionOutline.Look.Solid, Ally);
            }
            for (var i = shown; i < icons.Count; i++) icons[i].enabled = false;
            if (moon != null && moon.Target is Hex t) lines.Dashed(Center(moon.Vampire), Center(t), 2f * Scale, MoonBlood, 4f * Scale, 4f * Scale);
            if (hover is Hex hv && session.Battle.Board.InBounds(hv)) Hexagon(Center(hv), HoverCell);
            if (drop is Hex d && session.Battle.Board.InBounds(d)) Hexagon(Center(d), session.Battle.InDeploy(d) ? DropIn : DropOut);
            lines.Apply(mesh);
            Aim(session.AimAt(hover));
        }

        Material aimMaterial;
        LineRenderer arc, head, x1, x2;
        bool crossed;

        public bool AimShown => arc.enabled;
        public bool AimCrossed => crossed;

        LineRenderer AimLine(string name)
        {
            var l = new GameObject(name).AddComponent<LineRenderer>();
            l.transform.SetParent(transform, false);
            l.sharedMaterial = aimMaterial;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.enabled = false;
            return l;
        }

        static void Set(LineRenderer l, Vector3[] points, float width, Color color)
        {
            l.positionCount = points.Length;
            l.SetPositions(points);
            l.widthMultiplier = width;
            l.startColor = l.endColor = color.linear;
            l.enabled = true;
        }

        static Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float t) => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b;

        // ui-style.md 8-6. Lengths are in hex sizes.
        void Aim((Hex From, Hex To, bool Clear)? aim)
        {
            arc.enabled = head.enabled = x1.enabled = x2.enabled = false;
            aimEnds = null;
            crossed = false;
            if (aim == null) return;
            var (from, to, clear) = aim.Value;
            var s = board.art.hexSize;
            var a = board.Stand(from) + Vector3.up * (0.6f * s);
            var b = board.Stand(to) + Vector3.up * (0.3f * s);
            var flat = b - a;
            flat.y = 0f;
            b -= flat.normalized * (0.35f * s);
            aimEnds = (a, b);
            var color = clear ? Screens.Ui.Alpha(Enemy, 0.95f) : Screens.Ui.Alpha(Bone, 0.5f);
            Set(arc, new[] { a, b }, 0.12f * s, color);
            Set(head, new[] { a, b }, 0.45f * s, color);
            if (!clear)
            {
                crossed = true;
                Set(x1, new[] { a, b }, 0.14f * s, Enemy);
                Set(x2, new[] { a, b }, 0.14f * s, Enemy);
            }
            LateUpdate();
        }

        (Vector3 A, Vector3 B)? aimEnds;
        readonly Vector3[] arcPoints = new Vector3[26];

        // Rebuilt every frame so the arc and the X follow the camera as it turns.
        void LateUpdate()
        {
            var cam = board.viewCamera != null ? board.viewCamera : Camera.main;
            if (!(aimEnds is (Vector3 a, Vector3 b)) || cam == null) return;
            var s = board.art.hexSize;
            // Bulge across the chord as seen on screen, not along world up: under the 55° board camera a world-up arc falls almost along the view ray and the head folds into a hook.
            var bulge = Vector3.Cross(cam.transform.forward, b - a).normalized;
            if (Vector3.Dot(bulge, cam.transform.up) < 0f) bulge = -bulge;
            var c = (a + b) * 0.5f + bulge * (2f * Mathf.Min(0.3f * Vector3.Distance(a, b), s));
            var headLength = 0.5f * s;
            var neck = b - (b - c).normalized * headLength;
            var n = 0;
            for (var i = 0; i <= 24; i++)
            {
                var p = Bezier(a, c, b, i / 24f);
                if (Vector3.Distance(p, b) > headLength) arcPoints[n++] = p;
            }
            arcPoints[n++] = neck;
            arc.positionCount = n;
            for (var i = 0; i < n; i++) arc.SetPosition(i, arcPoints[i]);
            head.SetPosition(0, neck);
            head.SetPosition(1, b);
            if (!crossed) return;
            var x = Bezier(a, c, b, 0.5f);
            // Half of a 0.6-long stroke along the screen diagonal.
            var k = 0.3f / Mathf.Sqrt(2f) * s;
            var r = cam.transform.right * k;
            var u = cam.transform.up * k;
            x1.SetPosition(0, x - r - u);
            x1.SetPosition(1, x + r + u);
            x2.SetPosition(0, x - r + u);
            x2.SetPosition(1, x + r - u);
        }

        Vector3 Center(Hex h) => transform.InverseTransformPoint(board.HexToWorld(h));

        void Icon(int i, Hex h, string name, Color tint, bool zone)
        {
            if (i == icons.Count)
            {
                var go = new GameObject("Icon");
                go.transform.SetParent(transform, false);
                icons.Add(go.AddComponent<SpriteRenderer>());
            }
            var sr = icons[i];
            sr.sprite = board.art.Ui(name);
            sr.color = Screens.Ui.Alpha(tint, zone ? 0.4f : 0.85f);
            sr.enabled = sr.sprite != null;
            if (!sr.enabled) return;
            var size = (zone ? 0.45f : 0.9f) * board.art.hexSize;
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
