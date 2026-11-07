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
        List<string> diff = new List<string>();
        bool showDiff;

        public Brush brush = Brush.Tile;
        public char tile = '#';
        public EnemyKind enemy = EnemyKind.Skeleton;
        public UnitKind unit = UnitKind.PSpear;

        public static GameDataWindow Open()
        {
            var w = GetWindow<GameDataWindow>("게임 데이터");
            w.minSize = new Vector2(820, 520);
            return w;
        }

        public IReadOnlyList<string> Problems => problems;
        public IReadOnlyList<string> Diff => diff;

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
            diff = root != null && root.Missing().Count == 0 ? Differences(root.ToDataSet()) : new List<string>();
            Repaint();
        }

        static List<string> Differences(DataSet current)
        {
            var proto = PrototypeData.Create().Describe();
            var now = current.Describe();
            var p = new HashSet<string>(proto);
            var n = new HashSet<string>(now);
            return now.Where(l => !p.Contains(l)).Select(l => "지금  " + l).Concat(proto.Where(l => !n.Contains(l)).Select(l => "HTML  " + l)).ToList();
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
                EditorGUILayout.HelpBox("게임 데이터 에셋이 없습니다. HTML 프로토타입 값으로 만들 수 있습니다.", MessageType.Info);
                if (GUILayout.Button("HTML 기본값으로 데이터 에셋 만들기")) { DataAssetsBuilder.ResetAll(); Refresh(); }
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
                    if (showDiff) DiffPanel();
                    else if (selected != null) Detail();
                    else EditorGUILayout.HelpBox("왼쪽에서 고칠 항목을 고르세요.", MessageType.None);
                }
            }
        }

        void Toolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("저장", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();
                showDiff = GUILayout.Toggle(showDiff, $"HTML 기본값과 다른 곳 ({diff.Count})", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(selected == null))
                    if (GUILayout.Button("고른 항목을 기본값으로", EditorStyles.toolbarButton) && Confirm("고른 항목을 HTML 기본값으로 되돌릴까요?"))
                    {
                        if (!DataAssetsBuilder.Reset(selected)) ShowNotification(new GUIContent("이 항목은 HTML 에 없습니다"));
                        Refresh();
                    }
                if (GUILayout.Button("전체를 기본값으로", EditorStyles.toolbarButton) && Confirm("모든 데이터를 HTML 기본값으로 되돌릴까요? 실행 취소(Ctrl+Z)는 항목마다 해야 합니다."))
                {
                    DataAssetsBuilder.ResetAll();
                    Refresh();
                }
            }
        }

        static bool Confirm(string message) => EditorUtility.DisplayDialog("게임 데이터", message, "되돌리기", "취소");

        void Status()
        {
            if (problems.Count == 0) EditorGUILayout.HelpBox("데이터에 문제가 없습니다. 재생하면 이 값으로 돕니다.", MessageType.Info);
            else EditorGUILayout.HelpBox($"문제 {problems.Count}개. 고치기 전까지 게임은 HTML 기본값으로 돕니다.\n" + string.Join("\n", problems.Take(12)), MessageType.Error);
        }

        void List()
        {
            using (var s = new EditorGUILayout.ScrollViewScope(leftScroll, GUILayout.Width(210)))
            {
                leftScroll = s.scrollPosition;
                Item(root.rules, "규칙 (달·흡혈)");
                Item(root.skeleton, "적: " + (SourceText.Get("skeleton.name")));
                EditorGUILayout.LabelField("병종", EditorStyles.boldLabel);
                foreach (var u in root.units) Item(u, u != null ? $"{SourceText.Get("unit." + u.key + ".glyph")}  {SourceText.Get("unit." + u.key + ".name")}" : "(비었음)");
                EditorGUILayout.LabelField("장", EditorStyles.boldLabel);
                foreach (var st in root.stages) Item(st, st != null ? $"{SourceText.Get("stage." + st.id + ".label")} · {SourceText.Get("stage." + st.id + ".title")}" : "(비었음)");
                if (selected is StageData sd && GUILayout.Button("고른 장 복제"))
                {
                    Select(DataAssetsBuilder.DuplicateStage(root, sd));
                    Refresh();
                }
            }
        }

        void Item(Object o, string label)
        {
            var style = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, fontStyle = o == selected ? FontStyle.Bold : FontStyle.Normal };
            if (GUILayout.Toggle(o == selected, label, style) && o != selected && o != null) { showDiff = false; Select(o); }
        }

        void Detail()
        {
            if (selected is StageData stage) MapPainter(stage);
            EditorGUI.BeginChangeCheck();
            inspector.OnInspectorGUI();
            SourceText.Section(selected);
            if (EditorGUI.EndChangeCheck()) Refresh();
        }

        void DiffPanel()
        {
            if (diff.Count == 0) { EditorGUILayout.HelpBox("HTML 프로토타입 값과 같습니다.", MessageType.Info); return; }
            EditorGUILayout.HelpBox("\"지금\" 은 에셋 값, \"HTML\" 은 프로토타입 값입니다. 같은 항목이 두 줄로 나옵니다.", MessageType.None);
            foreach (var l in diff.Take(200)) EditorGUILayout.SelectableLabel(l, EditorStyles.miniLabel, GUILayout.Height(16));
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
                if (GUILayout.Toggle(brush == Brush.Enemy && enemy == EnemyKind.Skeleton, "적: 스켈레톤", EditorStyles.miniButton)) { brush = Brush.Enemy; enemy = EnemyKind.Skeleton; }
                if (GUILayout.Toggle(brush == Brush.Enemy && enemy == EnemyKind.Vampire, "적: 뱀파이어", EditorStyles.miniButton)) { brush = Brush.Enemy; enemy = EnemyKind.Vampire; }
                foreach (var u in root.units.Where(x => x != null))
                    if (GUILayout.Toggle(brush == Brush.Unit && unit == u.kind, "예시 " + SourceText.Get("unit." + u.key + ".glyph"), EditorStyles.miniButton)) { brush = Brush.Unit; unit = u.kind; }
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

        // Prototype CSS colours: --hex, --rock2, --candle, --water-line, --sun, and --steel for the deploy zone.
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
                    color = en.kind == EnemyKind.Vampire ? new Color(1f, 0.35f, 0.45f) : new Color(0.85f, 0.8f, 0.7f);
                    return SourceText.Get(en.kind == EnemyKind.Vampire ? "vampire.glyph" : "skeleton.glyph");
                }
            foreach (var a in s.sample)
                if (a.col == c && a.row == r)
                {
                    color = new Color(0.6f, 0.8f, 1f);
                    var t = root.units.FirstOrDefault(u => u != null && u.kind == a.kind);
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
                    if (enemy == EnemyKind.Vampire) s.enemies.RemoveAll(x => x.kind == EnemyKind.Vampire);
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
