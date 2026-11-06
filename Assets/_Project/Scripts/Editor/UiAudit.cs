using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace VampireSpawn.Editor
{
    // Play-mode layout check driven from the CLI (docs/dev/ui-audit.md): what leaves the screen, its own box, or its framed panel.
    public static class UiAudit
    {
        const float Slack = 2f;

        public static string Resize(int width, int height)
        {
            PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, "audit");
            return width + "x" + height;
        }

        public static string Report()
        {
            Canvas.ForceUpdateCanvases();
            var screen = new Rect(0, 0, Screen.width, Screen.height);
            var found = new List<string>();
            foreach (var g in Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
            {
                if (!g.isActiveAndEnabled || g.color.a < 0.02f || g.canvas == null || g is RawImage) continue;
                var drawn = Drawn(g);
                if (drawn.width <= 0f) continue;
                var path = Path(g.transform);
                // Glyph quads overhang the line box: bold bearings sideways, OS-font ascent and descent by up to 40% of the size (Latin descenders); vertical spill is font metrics, sideways spill is the visible fault.
                var (hs, vs) = g is Text tx ? (4f * g.canvas.scaleFactor, Mathf.Max(6f, tx.fontSize * 0.4f * g.canvas.scaleFactor)) : (Slack, Slack);
                string Over(Rect outer, Rect inner) => Spill(outer, inner, hs, vs);
                if (Over(screen, drawn) is var s && s != "") found.Add("off-screen\t" + path + "\t" + s);
                if (g is Text t && Over(World(g.rectTransform), drawn) is var o && o != "") found.Add("text>box\t" + path + "\t" + o + " \"" + Clip(t.text) + "\"");
                var frame = FramedAncestor(g.transform);
                if (frame != null && Over(World(frame), drawn) is var f && f != "") found.Add("out-of-frame\t" + path + "\t" + f + " frame=" + frame.name);
                // A button label running under its key cap (Kit.Hotkey).
                if (g is Text lt && g.transform.parent != null && g.transform.parent.Find("Key") is RectTransform key && key != g.transform && World(key).Overlaps(drawn))
                    found.Add("key-overlap\t" + path + "\t\"" + Clip(lt.text) + "\"");
            }
            var board = Object.FindFirstObjectByType<VampireSpawn.Game.BoardView>();
            if (board != null && board.Board != null && board.viewCamera != null && board.viewCamera.targetTexture == null)
            {
                var b = board.Board;
                foreach (var h in new[] { new Core.Hex(0, 0), new Core.Hex(b.Cols - 1, 0), new Core.Hex(0, b.Rows - 1), new Core.Hex(b.Cols - 1, b.Rows - 1) })
                {
                    // The hex's outer edge, not its centre: the old crop cut edge hexes in half.
                    var side = h.Col == 0 ? -1f : 1f;
                    var v = board.viewCamera.WorldToViewportPoint(board.HexToWorld(h) + Vector3.right * side * board.art.hexSize);
                    var fr = board.Frame; if (v.x < fr.xMin - 0.005f || v.x > fr.xMax + 0.005f || v.y < fr.yMin - 0.005f || v.y > fr.yMax + 0.005f) found.Add("board-cropped\tBoardView\thex " + h.Col + "," + h.Row + " edge at viewport " + v.x.ToString("0.00") + "," + v.y.ToString("0.00"));
                }
            }
            var sb = new StringBuilder();
            sb.Append(Screen.width).Append('x').Append(Screen.height).Append(" issues=").Append(found.Count).Append('\n');
            foreach (var line in found.Distinct()) sb.Append(line).Append('\n');
            return sb.ToString();
        }

        // Text glyphs, not the RectTransform: overflowing text draws outside its rect.
        static Rect Drawn(Graphic g)
        {
            if (!(g is Text t)) return World(g.rectTransform);
            var verts = t.cachedTextGenerator.verts;
            if (verts.Count == 0 || string.IsNullOrWhiteSpace(t.text)) return Rect.zero;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var scale = 1f / t.pixelsPerUnit;
            foreach (var v in verts)
            {
                var w = (Vector2)t.rectTransform.TransformPoint(v.position * scale);
                min = Vector2.Min(min, w);
                max = Vector2.Max(max, w);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static Rect World(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        static string Spill(Rect outer, Rect inner, float hs, float vs)
        {
            var parts = new List<string>();
            void Add(string side, float px, float slack) { if (px > slack) parts.Add(side + px.ToString("0")); }
            Add("L", outer.xMin - inner.xMin, hs);
            Add("R", inner.xMax - outer.xMax, hs);
            Add("T", inner.yMax - outer.yMax, vs);
            Add("B", outer.yMin - inner.yMin, vs);
            return string.Join(",", parts);
        }

        // A panel is framed when Ui.Frame or Kit.Btn put a "Frame" image directly under it.
        static RectTransform FramedAncestor(Transform t)
        {
            for (var p = t.parent; p != null; p = p.parent)
                if (p.Find("Frame") is Transform f && f != t && f.GetComponent<Image>() != null) return (RectTransform)p;
            return null;
        }

        static string Path(Transform t)
        {
            var names = new List<string>();
            for (var p = t; p != null && p.GetComponent<Canvas>() == null; p = p.parent) names.Add(p.name);
            names.Reverse();
            return string.Join("/", names);
        }


        static string Clip(string s) { s = s.Replace("\n", " "); return s.Length > 40 ? s.Substring(0, 40) + "…" : s; }
    }
}
