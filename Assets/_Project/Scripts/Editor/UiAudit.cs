using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VampireSpawn.Editor
{
    // Play-mode layout check driven from the CLI (docs/dev/ui-audit.md): what leaves the screen, its own box, or its framed panel. Units are panel px.
    public static class UiAudit
    {
        const float Slack = 2f;
        static readonly string[] Frames = { "card", "panel", "unit", "box", "window" };

        public static string Resize(int width, int height)
        {
            PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, "audit");
            return width + "x" + height;
        }

        public static string Report()
        {
            var found = new List<string>();
            foreach (var doc in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)) Toolkit(doc, found);
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

        // Off the panel, text wider than its label, outside its framed box. Ellipsized labels cut on purpose; .decor stands out on purpose.
        static void Toolkit(UIDocument doc, List<string> found)
        {
            var root = doc.rootVisualElement;
            if (root == null || root.panel == null) return;
            var screen = root.worldBound;
            foreach (var e in root.Query<VisualElement>().ToList())
            {
                if (Hidden(e) || e.worldBound.width <= 0f) continue;
                var label = e as Label;
                var drawn = e.resolvedStyle.backgroundImage.sprite != null || e.resolvedStyle.backgroundImage.texture != null || (label != null && !string.IsNullOrWhiteSpace(label.text));
                if (!drawn) continue;
                var path = Path(e);
                if (Spill(screen, e.worldBound) is var s && s != "") found.Add("off-screen\t" + path + "\t" + s);
                if (label != null && label.resolvedStyle.whiteSpace == WhiteSpace.NoWrap && label.resolvedStyle.textOverflow != TextOverflow.Ellipsis)
                {
                    var need = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                    if (need.x > label.contentRect.width + 4f) found.Add("text>box\t" + path + "\tR" + (need.x - label.contentRect.width).ToString("0") + " \"" + Clip(label.text) + "\"");
                }
                for (var p = e.parent; p != null; p = p.parent)
                    if (p.ClassListContains("decor")) break;
                    else if (Frames.Any(p.ClassListContains))
                    {
                        if (Spill(p.worldBound, e.worldBound) is var f && f != "") found.Add("out-of-frame\t" + path + "\t" + f + " frame=" + (string.IsNullOrEmpty(p.name) ? p.GetClasses().FirstOrDefault() : p.name));
                        break;
                    }
            }
        }

        static bool Hidden(VisualElement e)
        {
            for (var p = e; p != null; p = p.parent)
                if (p.resolvedStyle.display == DisplayStyle.None || p.resolvedStyle.visibility == Visibility.Hidden || p.resolvedStyle.opacity < 0.02f) return true;
            return false;
        }

        static string Path(VisualElement e)
        {
            var names = new List<string>();
            for (var p = e; p != null && p.parent != null; p = p.parent) names.Add(string.IsNullOrEmpty(p.name) ? p.GetClasses().FirstOrDefault() ?? p.GetType().Name : p.name);
            names.Reverse();
            return string.Join("/", names);
        }

        static string Spill(Rect outer, Rect inner)
        {
            var parts = new List<string>();
            void Add(string side, float px) { if (px > Slack) parts.Add(side + px.ToString("0")); }
            Add("L", outer.xMin - inner.xMin);
            Add("R", inner.xMax - outer.xMax);
            Add("T", outer.yMin - inner.yMin);
            Add("B", inner.yMax - outer.yMax);
            return string.Join(",", parts);
        }

        static string Clip(string s) { s = s.Replace("\n", " "); return s.Length > 40 ? s.Substring(0, 40) + "…" : s; }
    }
}
