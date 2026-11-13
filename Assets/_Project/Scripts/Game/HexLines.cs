using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSpawn.Game
{
    // Flat line and fill geometry for hex cells, with every vertex dropped onto the terrain through `drape` (docs/art/battle-terrain.md 3).
    public sealed class HexLines
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> tris = new List<int>();
        readonly Func<Vector3, Vector3> drape;

        // Short enough that a line follows a 0.12-high bump across one cell.
        const float Piece = 0.25f;

        public HexLines(Func<Vector3, Vector3> drape) => this.drape = drape;

        public void Clear() { verts.Clear(); colors.Clear(); tris.Clear(); }

        public void Apply(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        public static Vector3 Corner(Vector3 c, float r, int i)
        {
            var a = Mathf.Deg2Rad * 60f * (i % 6);
            return c + new Vector3(r * Mathf.Cos(a), 0f, r * Mathf.Sin(a));
        }

        public void Fill(Vector3 c, float r, Color color)
        {
            for (var i = 0; i < 6; i++)
            {
                var a = Corner(c, r, i);
                var b = Corner(c, r, i + 1);
                var ca = (c + a) * 0.5f;
                var cb = (c + b) * 0.5f;
                var ab = (a + b) * 0.5f;
                Tri(c, cb, ca, color);
                Tri(ca, ab, a, color);
                Tri(cb, b, ab, color);
                Tri(ca, cb, ab, color);
            }
        }

        public void Outline(Vector3 c, float r, float width, Color color, float dash = 0f, float gap = 0f)
        {
            for (var i = 0; i < 6; i++)
            {
                var a = Corner(c, r, i);
                var b = Corner(c, r, i + 1);
                if (dash > 0f) Dashed(a, b, width, color, dash, gap);
                else Segment(a, b, width, color);
            }
        }

        public void Dashed(Vector3 a, Vector3 b, float width, Color color, float dash, float gap)
        {
            var len = Vector3.Distance(a, b);
            var dir = (b - a) / Mathf.Max(len, 0.0001f);
            for (var t = 0f; t < len; t += dash + gap) Segment(a + dir * t, a + dir * Mathf.Min(len, t + dash), width, color);
        }

        public void Segment(Vector3 a, Vector3 b, float width, Color color)
        {
            var d = b - a;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-8f) return;
            var side = Vector3.Cross(Vector3.up, d.normalized) * (width * 0.5f);
            var along = d.normalized * (width * 0.5f);
            // The epsilon keeps a unit edge that measures 1.0000001 at 4 pieces, not 5.
            var n = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / Piece - 0.001f));
            for (var i = 0; i < n; i++)
            {
                var p = Vector3.Lerp(a, b, (float)i / n) - (i == 0 ? along : Vector3.zero);
                var q = Vector3.Lerp(a, b, (float)(i + 1) / n) + (i == n - 1 ? along : Vector3.zero);
                Quad(p - side, p + side, q + side, q - side, color);
            }
        }

        void Tri(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            var k = verts.Count;
            verts.Add(drape(a)); verts.Add(drape(b)); verts.Add(drape(c));
            for (var i = 0; i < 3; i++) colors.Add(color.linear);
            tris.Add(k); tris.Add(k + 1); tris.Add(k + 2);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            var k = verts.Count;
            verts.Add(drape(a)); verts.Add(drape(b)); verts.Add(drape(c)); verts.Add(drape(d));
            for (var i = 0; i < 4; i++) colors.Add(color.linear);
            tris.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        }
    }
}
