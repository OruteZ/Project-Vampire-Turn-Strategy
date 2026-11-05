using UnityEditor;
using UnityEngine;

namespace VampireSpawn.Editor
{
    // The project's only menu entry (CLAUDE.md "Unity 메뉴·에디터 도구"): each area adds one row of buttons here.
    public sealed class ToolsWindow : EditorWindow
    {
        [MenuItem("VampireSpawn/도구 모음 (Tools)")]
        public static void Open() => GetWindow<ToolsWindow>("도구 모음");

        void OnGUI()
        {
            EditorGUILayout.LabelField("데이터", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("게임 데이터 편집기")) GameDataWindow.Open();
                if (GUILayout.Button("데이터 에셋 선택")) Selection.activeObject = DataAssetsBuilder.Root();
            }
        }
    }
}
