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

            EditorGUILayout.LabelField("유료 에셋", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Synty 다시 불러오기", "빠진 Synty 패키지를 UPackage/ 에서 임포트하고 Sidekick 레시피를 굽는다 (/synty-reload)"))) SyntyReload.Run(false);
                if (GUILayout.Button(new GUIContent("캐릭터 모두 다시 굽기", "레시피가 그대로여도 전부 다시 굽는다"))) SyntyReload.Run(true);
            }
            var last = SyntyReload.LastResult;
            if (last.Length > 0)
                EditorGUILayout.HelpBox(last + "\n확인: Window > General > Test Runner 의 SidekickRecipeTests, RequiredAssetsTests",
                    last.Contains("FAIL") || last.Contains("MISSING") ? MessageType.Warning : MessageType.Info);

            EditorGUILayout.LabelField("UI", EditorStyles.boldLabel);
            var toolkit = PlayerPrefs.GetInt(Game.Screens.MenuToolkit.PrefKey, 0) == 1;
            var next = GUILayout.Toggle(toolkit, new GUIContent("메인 메뉴를 UI Toolkit 시험판으로", "켜면 다음에 메뉴를 그릴 때 Assets/_Project/UI/Menu.uxml 을 쓴다 (#44, docs/dev/ui-toolkit-trial.md)"));
            if (next != toolkit) PlayerPrefs.SetInt(Game.Screens.MenuToolkit.PrefKey, next ? 1 : 0);

            EditorGUILayout.LabelField("지도", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("프리팹 목록 채우기", "CampaignMapLayout 이 쓰는 프리팹을 이름으로 찾아 ArtCatalog.mapProps 에 넣는다 (docs/art/campaign-map.md 2-6)")))
                Debug.Log("지도 프리팹: " + CampaignMapProps.Fill());
            if (GUILayout.Button(new GUIContent("Camp 씬에 지도 굽기", "CampaignMapLayout 을 Camp 씬의 Campaign Map 으로 다시 짓는다. 손으로 옮긴 소품은 사라진다 (docs/dev/scene-split.md 3)"))
                && EditorUtility.DisplayDialog("지도 굽기", "Camp 씬의 Campaign Map 을 배치 표로 다시 짓습니다. 손으로 옮긴 소품은 사라집니다.", "굽기", "취소"))
                Debug.Log(CampaignMapBaker.Bake());

            EditorGUILayout.LabelField("유닛 모습", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("유닛 모습 한눈에", "모든 UnitLook 을 앞 대기·옆 대기·옆 공격으로 찍어 verify-artifacts/unit-lineup.png 로 연다 (docs/art/unit-visuals.md 6)")))
            {
                var path = System.IO.Path.GetFullPath("verify-artifacts/unit-lineup.png");
                UnitLineup.Capture(path);
                EditorUtility.OpenWithDefaultApp(path);
            }
        }
    }
}
