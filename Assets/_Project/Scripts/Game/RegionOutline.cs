using System.Collections.Generic;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // Outer border of any set of cells; inner edges shared by two cells are dropped (docs/art/battle-terrain.md 3-2).
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RegionOutline : MonoBehaviour
    {
        public enum Look { Flow, Solid }

        // Switchable live in the inspector during play.
        public Look look = Look.Flow;
        public Color color = Screens.Ui.Candle;
        public float width = 0.08f;
        // Flow only, in hexSize units and hexSize per second.
        public float dash = 0.25f, gap = 0.15f, flow = 0.3f;
        public float lift = 0.05f;

        BoardView board;
        HexLines lines;
        Look drawn;
        Mesh mesh;
        readonly List<List<Vector3>> loops = new List<List<Vector3>>();

        public static RegionOutline Create(BoardView board, string name, IEnumerable<Hex> cells, Look look = Look.Flow, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(board.transform, false);
            var o = go.AddComponent<RegionOutline>();
            o.look = look;
            if (color is Color c) o.color = c;
            o.Show(board, cells);
            return o;
        }

        public void Show(BoardView board, IEnumerable<Hex> cells)
        {
            this.board = board;
            if (mesh == null)
            {
                mesh = new Mesh { name = "RegionOutline" };
                mesh.MarkDynamic();
                GetComponent<MeshFilter>().sharedMesh = mesh;
                var mr = GetComponent<MeshRenderer>();
                mr.sharedMaterial = board.art.overlay;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lines = new HexLines(p => transform.InverseTransformPoint(board.OnHologram(transform.TransformPoint(p), lift)));
            }
            Trace(cells, board.art.hexSize, loops);
            Draw(Time.time * flow);
        }

        void Update()
        {
            if (board != null && (look == Look.Flow || drawn != look)) Draw(Time.time * flow);
        }

        // Each cell's edges run the same way round, so the border edges chain head to tail into closed loops.
        public static void Trace(IEnumerable<Hex> cells, float r, List<List<Vector3>> loops)
        {
            loops.Clear();
            var edges = new Dictionary<Vector2Int, (Vector3 a, Vector3 b)>();
            foreach (var h in new HashSet<Hex>(cells))
            {
                var c = BoardView.Local(h, r);
                for (var i = 0; i < 6; i++)
                {
                    var a = HexLines.Corner(c, r, i);
                    var b = HexLines.Corner(c, r, i + 1);
                    var k = Key((a + b) * 0.5f);
                    if (!edges.Remove(k)) edges.Add(k, (a, b));
                }
            }
            var next = new Dictionary<Vector2Int, Vector3>();
            foreach (var e in edges.Values) next[Key(e.a)] = e.b;
            while (next.Count > 0)
            {
                var loop = new List<Vector3>();
                var start = next.Keys.GetEnumerator();
                start.MoveNext();
                var k = start.Current;
                while (next.TryGetValue(k, out var b))
                {
                    next.Remove(k);
                    loop.Add(b);
                    k = Key(b);
                }
                loops.Add(loop);
            }
        }

        static Vector2Int Key(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x * 1000f), Mathf.RoundToInt(p.z * 1000f));

        void Draw(float shift)
        {
            lines.Clear();
            var s = board.art.hexSize;
            var w = width * s;
            foreach (var loop in loops)
            {
                if (look == Look.Solid || dash <= 0f)
                {
                    for (var i = 0; i < loop.Count; i++) lines.Segment(loop[i], loop[(i + 1) % loop.Count], w, color);
                    continue;
                }
                var total = 0f;
                for (var i = 0; i < loop.Count; i++) total += Vector3.Distance(loop[i], loop[(i + 1) % loop.Count]);
                // Whole dashes per loop, so there is no short dash where the loop closes.
                var period = total / Mathf.Max(1, Mathf.Round(total / ((dash + gap) * s)));
                var dashLen = period * dash / (dash + gap);
                var phase = Mathf.Repeat(shift * s, period);
                var along = 0f;
                for (var i = 0; i < loop.Count; i++)
                {
                    var a = loop[i];
                    var b = loop[(i + 1) % loop.Count];
                    var len = Vector3.Distance(a, b);
                    // First dash start at or before this edge, in the loop's distance.
                    var t = Mathf.Floor((along - phase) / period) * period + phase;
                    for (; t < along + len; t += period)
                    {
                        var from = Mathf.Max(t, along) - along;
                        var to = Mathf.Min(t + dashLen, along + len) - along;
                        if (to > from) lines.Segment(Vector3.Lerp(a, b, from / len), Vector3.Lerp(a, b, to / len), w, color);
                    }
                    along += len;
                }
            }
            lines.Apply(mesh);
            drawn = look;
        }
    }
}
