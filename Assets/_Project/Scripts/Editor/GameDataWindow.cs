using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Editor
{
    // Designer-facing editor for the data assets (docs/guides/data-editor.md).
    public sealed class GameDataWindow : EditorWindow
    {
        public enum Brush { Tile, Enemy, Unit, Erase }

        const float R = 20f;
        static readonly float Sqrt3 = Mathf.Sqrt(3f);
        static readonly (char ch, string label)[] Tiles = { ('.', "평지"), ('#', "바위"), ('T', "화톳불"), ('~', "물"), ('S', "햇빛"), ('d', "배치 구역") };

        GameDataAsset root;
        Object selected;
        UnityEditor.Editor inspector;
        Vector2 leftScroll, rightScroll;
        List<string> problems = new List<string>();

        public Brush brush = Brush.Tile;
        public char tile = '#';
        public string enemy = "skeleton";
        public string unit = "pspear";

        public static GameDataWindow Open()
        {
            var w = GetWindow<GameDataWindow>("게임 데이터");
            w.minSize = new Vector2(820, 520);
            return w;
        }

        public IReadOnlyList<string> Problems => problems;

        void OnEnable()
        {
            Undo.undoRedoPerformed += Refresh;
            Refresh();
        }

        void OnDisable() => Undo.undoRedoPerformed -= Refresh;

        public void Refresh()
        {
            root = DataAssetsBuilder.Root();
            problems = root != null ? root.Problems() : new List<string> { "데이터 에셋이 없다" };
            GameData.Use(null);
            Repaint();
        }

        public void Select(Object o)
        {
            selected = o;
            UnityEditor.Editor.CreateCachedEditor(o, null, ref inspector);
            Repaint();
        }

        void OnGUI()
        {
            if (root == null)
            {
                EditorGUILayout.HelpBox("게임 데이터 에셋이 없습니다 (" + DataAssetsBuilder.RootPath + ").", MessageType.Error);
                return;
            }
            Toolbar();
            Status();
            using (new EditorGUILayout.HorizontalScope())
            {
                List();
                using (var s = new EditorGUILayout.ScrollViewScope(rightScroll))
                {
                    rightScroll = s.scrollPosition;
                    if (selected != null) Detail();
                    else EditorGUILayout.HelpBox("왼쪽에서 고칠 항목을 고르세요.", MessageType.None);
                }
            }
        }

        void Toolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("저장", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!(selected is StageData || selected is UnitTypeData || selected is EnemyData)))
                    if (GUILayout.Button(new GUIContent("고른 항목 복제", "새 병종·하수인·장은 비슷한 것을 복제해 key(id)와 값을 고친다 (docs/guides/data-editor.md)"), EditorStyles.toolbarButton))
                    {
                        Select(DataAssetsBuilder.Duplicate(root, selected));
                        Refresh();
                    }
            }
        }

        void Status()
        {
            if (problems.Count == 0) EditorGUILayout.HelpBox("데이터에 문제가 없습니다. 재생하면 이 값으로 돕니다.", MessageType.Info);
            else EditorGUILayout.HelpBox($"문제 {problems.Count}개. 고치기 전까지 게임이 바르게 돌지 않습니다.\n" + string.Join("\n", problems.Take(12)), MessageType.Error);
        }

        void List()
        {
            using (var s = new EditorGUILayout.ScrollViewScope(leftScroll, GUILayout.Width(210)))
            {
                leftScroll = s.scrollPosition;
                Item(root.rules, "규칙 (달·흡혈)");
                EditorGUILayout.LabelField("하수인", EditorStyles.boldLabel);
                foreach (var m in root.minions) Item(m, m != null ? $"{SourceText.Get(m.key + ".glyph")}  {SourceText.Get(m.key + ".name")}" : "(비었음)");
                EditorGUILayout.LabelField("병종", EditorStyles.boldLabel);
                foreach (var u in root.units) Item(u, u != null ? $"{SourceText.Get("unit." + u.key + ".glyph")}  {SourceText.Get("unit." + u.key + ".name")}" : "(비었음)");
                EditorGUILayout.LabelField("장", EditorStyles.boldLabel);
                foreach (var st in root.stages) Item(st, st != null ? $"{SourceText.Get("stage." + st.id + ".label")} · {SourceText.Get("stage." + st.id + ".title")}" : "(비었음)");            }
        }

        void Item(Object o, string label)
        {
            var style = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, fontStyle = o == selected ? FontStyle.Bold : FontStyle.Normal };
            if (GUILayout.Toggle(o == selected, label, style) && o != selected && o != null) Select(o);
        }

        void Detail()
        {
            if (selected is StageData stage) MapPainter(stage);
            EditorGUI.BeginChangeCheck();
            inspector.OnInspectorGUI();
            SourceText.Section(selected);
            if (EditorGUI.EndChangeCheck()) Refresh();
        }

        void MapPainter(StageData s)
        {
            EditorGUILayout.LabelField("지도 칠하기", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var (ch, label) in Tiles)
                    if (GUILayout.Toggle(brush == Brush.Tile && tile == ch, $"{ch} {label}", EditorStyles.miniButton)) { brush = Brush.Tile; tile = ch; }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(brush == Brush.Enemy && enemy == EnemyType.Vampire, "적: 뱀파이어", EditorStyles.miniButton)) { brush = Brush.Enemy; enemy = EnemyType.Vampire; }
                foreach (var m in root.minions.Where(x => x != null))
                    if (GUILayout.Toggle(brush == Brush.Enemy && enemy == m.key, "적: " + SourceText.Get(m.key + ".name"), EditorStyles.miniButton)) { brush = Brush.Enemy; enemy = m.key; }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var u in root.units.Where(x => x != null))
                    if (GUILayout.Toggle(brush == Brush.Unit && unit == u.key, "예시 " + SourceText.Get("unit." + u.key + ".glyph"), EditorStyles.miniButton)) { brush = Brush.Unit; unit = u.key; }
                if (GUILayout.Toggle(brush == Brush.Erase, "유닛 지우기", EditorStyles.miniButton)) brush = Brush.Erase;
            }
            var rows = s.map.Count;
            var cols = rows > 0 ? s.map[0].Length : 0;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"크기 {cols}×{rows}", GUILayout.Width(80));
                if (GUILayout.Button("열 −", EditorStyles.miniButton)) Resize(s, cols - 1, rows);
                if (GUILayout.Button("열 +", EditorStyles.miniButton)) Resize(s, cols + 1, rows);
                if (GUILayout.Button("행 −", EditorStyles.miniButton)) Resize(s, cols, rows - 1);
                if (GUILayout.Button("행 +", EditorStyles.miniButton)) Resize(s, cols, rows + 1);
                GUILayout.FlexibleSpace();
            }
            var area = GUILayoutUtility.GetRect(R * (1.5f * cols + 0.5f) + 4, R * Sqrt3 * (rows + 0.5f) + 4, GUILayout.ExpandWidth(false));
            var e = Event.current;
            for (var r = 0; r < rows; r++)
                for (var c = 0; c < cols; c++)
                {
                    var center = Center(area, c, r);
                    if (e.type == EventType.Repaint) DrawHex(center, TileColor(s.map[r][c]));
                    var label = SpotLabel(s, c, r, out var color);
                    if (label != null)
                        GUI.Label(new Rect(center.x - R, center.y - R * 0.5f, R * 2, R), label, new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = color } });
                }
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && area.Contains(e.mousePosition) && HexAt(area, e.mousePosition, cols, rows) is Vector2Int h)
            {
                Paint(s, h.x, h.y);
                e.Use();
            }
            EditorGUILayout.Space();
        }

        static Vector2 Center(Rect area, int c, int r) => new Vector2(area.x + 2 + R + 1.5f * R * c, area.y + 2 + R * Sqrt3 * (0.5f + r + 0.5f * (c & 1)));

        static Vector2Int? HexAt(Rect area, Vector2 p, int cols, int rows)
        {
            Vector2Int? best = null;
            var bd = R * R;
            for (var r = 0; r < rows; r++)
                for (var c = 0; c < cols; c++)
                {
                    var d = (Center(area, c, r) - p).sqrMagnitude;
                    if (d < bd) { bd = d; best = new Vector2Int(c, r); }
                }
            return best;
        }

        static void DrawHex(Vector2 c, Color fill)
        {
            var pts = Enumerable.Range(0, 6).Select(i => (Vector3)(c + new Vector2(Mathf.Cos(Mathf.Deg2Rad * 60 * i), Mathf.Sin(Mathf.Deg2Rad * 60 * i)) * (R - 1.5f))).ToArray();
            Handles.color = fill;
            Handles.DrawAAConvexPolygon(pts);
            Handles.color = new Color(0, 0, 0, 0.5f);
            Handles.DrawAAPolyLine(1.5f, pts.Append(pts[0]).ToArray());
        }

        // Board palette: plain, rock, candle, water, sun, and steel for the deploy zone.
        static Color TileColor(char ch) => ch switch
        {
            '#' => new Color(0.45f, 0.42f, 0.48f),
            'T' => new Color(0.89f, 0.66f, 0.29f),
            '~' => new Color(0.24f, 0.34f, 0.44f),
            'S' => new Color(0.92f, 0.86f, 0.68f),
            'd' => new Color(0.36f, 0.46f, 0.55f),
            '.' => new Color(0.16f, 0.15f, 0.19f),
            _ => Color.magenta,
        };

        string SpotLabel(StageData s, int c, int r, out Color color)
        {
            color = Color.white;
            foreach (var en in s.enemies)
                if (en.col == c && en.row == r)
                {
                    color = en.kind == EnemyType.Vampire ? new Color(1f, 0.35f, 0.45f) : new Color(0.85f, 0.8f, 0.7f);
                    return SourceText.Get(en.kind + ".glyph");
                }
            foreach (var a in s.sample)
                if (a.col == c && a.row == r)
                {
                    color = new Color(0.6f, 0.8f, 1f);
                    var t = root.units.FirstOrDefault(u => u != null && u.key == a.kind);
                    return t != null ? SourceText.Get("unit." + t.key + ".glyph") : "?";
                }
            return null;
        }

        // The click handler, also callable from scripts: applies the current brush to one hex of the stage.
        public void Paint(StageData s, int c, int r)
        {
            if (r < 0 || r >= s.map.Count || c < 0 || c >= s.map[r].Length) return;
            Undo.RecordObject(s, "지도 칠하기");
            switch (brush)
            {
                case Brush.Tile:
                    var row = s.map[r].ToCharArray();
                    row[c] = tile;
                    s.map[r] = new string(row);
                    break;
                case Brush.Enemy:
                    ClearSpot(s, c, r);
                    if (enemy == EnemyType.Vampire) s.enemies.RemoveAll(x => x.kind == EnemyType.Vampire);
                    s.enemies.Add(new EnemySpot { kind = enemy, col = c, row = r });
                    break;
                case Brush.Unit:
                    ClearSpot(s, c, r);
                    s.sample.Add(new UnitSpot { kind = unit, col = c, row = r });
                    break;
                case Brush.Erase:
                    ClearSpot(s, c, r);
                    break;
            }
            EditorUtility.SetDirty(s);
            Refresh();
        }

        static void ClearSpot(StageData s, int c, int r)
        {
            s.enemies.RemoveAll(x => x.col == c && x.row == r);
            s.sample.RemoveAll(x => x.col == c && x.row == r);
        }

        void Resize(StageData s, int cols, int rows)
        {
            if (cols < 1 || rows < 1) return;
            Undo.RecordObject(s, "지도 크기");
            while (s.map.Count < rows) s.map.Add(new string('.', s.map.Count > 0 ? s.map[0].Length : cols));
            while (s.map.Count > rows) s.map.RemoveAt(s.map.Count - 1);
            for (var r = 0; r < s.map.Count; r++)
                s.map[r] = s.map[r].Length >= cols ? s.map[r].Substring(0, cols) : s.map[r] + new string('.', cols - s.map[r].Length);
            s.enemies.RemoveAll(x => x.col >= cols || x.row >= rows);
            s.sample.RemoveAll(x => x.col >= cols || x.row >= rows);
            EditorUtility.SetDirty(s);
            Refresh();
        }
    }
}
